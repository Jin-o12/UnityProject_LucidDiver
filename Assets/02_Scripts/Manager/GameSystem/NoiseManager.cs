using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체의 소음 요청을 받아 실제 소음 이벤트로 확정하고 브로드캐스트하는 중앙 관리자입니다.
/// 소리 종류별 기본 범위, 지속 시간, 우선순위를 한곳에서 관리합니다.
/// </summary>
public class NoiseManager : MonoBehaviour
{
    [Serializable]
    private struct NoiseTypeSetting
    {
        public NoiseType type;
        public float defaultRadius;
        public float defaultDuration;
        public int priority;
        public bool canInterruptChase;
        public Color debugColor;
    }

    public static NoiseManager Instance { get; private set; }

    [Header("Noise Debug")]
    [SerializeField] private bool createNoiseMarkers = true;
    [SerializeField] private NoiseSourceObject noiseSourcePrefab;

    [Header("Noise Settings")]
    [SerializeField] private List<NoiseTypeSetting> noiseSettings = new()
    {
        new NoiseTypeSetting { type = NoiseType.Walk, defaultRadius = 15.0f, defaultDuration = 0.5f, priority = 5, canInterruptChase = false, debugColor = new Color(0.25f, 0.7f, 1.0f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.Run, defaultRadius = 20.0f, defaultDuration = 0.7f, priority = 10, canInterruptChase = false, debugColor = new Color(0.1f, 0.9f, 0.9f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.Gunshot, defaultRadius = 40.0f, defaultDuration = 1.2f, priority = 30, canInterruptChase = false, debugColor = new Color(1.0f, 0.6f, 0.1f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.ChestOpen, defaultRadius = 30.0f, defaultDuration = 1.8f, priority = 40, canInterruptChase = true, debugColor = new Color(1.0f, 0.9f, 0.2f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.Skill, defaultRadius = 12.0f, defaultDuration = 1.0f, priority = 35, canInterruptChase = false, debugColor = new Color(0.8f, 0.4f, 1.0f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.EnemyAlertCry, defaultRadius = 13.0f, defaultDuration = 1.2f, priority = 45, canInterruptChase = false, debugColor = new Color(1.0f, 0.2f, 0.2f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.HitReaction, defaultRadius = 5.0f, defaultDuration = 0.6f, priority = 15, canInterruptChase = false, debugColor = new Color(0.9f, 0.5f, 0.5f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.CombatImpact, defaultRadius = 8.0f, defaultDuration = 0.8f, priority = 20, canInterruptChase = false, debugColor = new Color(0.9f, 0.35f, 0.15f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.Decoy, defaultRadius = 18.0f, defaultDuration = 2.5f, priority = 60, canInterruptChase = true, debugColor = new Color(0.4f, 1.0f, 0.35f, 1.0f) },
        new NoiseTypeSetting { type = NoiseType.LucidLeak, defaultRadius = 45.0f, defaultDuration = 3.0f, priority = 50, canInterruptChase = false, debugColor = new Color(0.85f, 0.15f, 0.2f, 1.0f) },
    };

    private readonly Dictionary<NoiseType, NoiseTypeSetting> cachedSettings = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null)
        {
            return;
        }

        // 씬에 배치하지 않아도 노이즈 시스템이 항상 동작하도록 자동 생성합니다.
        GameObject noiseManagerObject = new("NoiseManager");
        DontDestroyOnLoad(noiseManagerObject);
        noiseManagerObject.AddComponent<NoiseManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CacheSettings();
    }

    private void OnEnable()
    {
        CacheSettings();
        GlobalEventBus.OnNoiseRequested += HandleNoiseRequested;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnNoiseRequested -= HandleNoiseRequested;
    }

    private void CacheSettings()
    {
        cachedSettings.Clear();

        for (int i = 0; i < noiseSettings.Count; i++)
        {
            cachedSettings[noiseSettings[i].type] = noiseSettings[i];
        }
    }

    private void HandleNoiseRequested(NoiseStimulus requestedStimulus)
    {
        NoiseStimulus resolvedStimulus = ResolveStimulus(requestedStimulus);
        NoiseSourceObject spawnedNoiseSource = null;

        if (createNoiseMarkers && resolvedStimulus.Duration > 0.0f)
        {
            spawnedNoiseSource = CreateNoiseSourceObject(resolvedStimulus);
        }

        if (spawnedNoiseSource != null)
        {
            // AI 가 실제 노이즈 오브젝트를 기준점으로 삼을 수 있게 앵커를 연결합니다.
            resolvedStimulus.Position = spawnedNoiseSource.transform.position;
            resolvedStimulus.AnchorTransform = spawnedNoiseSource.transform;
        }

        GlobalEventBus.OnNoiseEmitted?.Invoke(resolvedStimulus);
    }

    private NoiseStimulus ResolveStimulus(NoiseStimulus requestedStimulus)
    {
        NoiseTypeSetting setting = GetNoiseSetting(requestedStimulus.Type);

        float radius = requestedStimulus.Radius > 0.0f ? requestedStimulus.Radius : setting.defaultRadius;
        float duration = requestedStimulus.Duration > 0.0f ? requestedStimulus.Duration : setting.defaultDuration;
        int priority = requestedStimulus.Priority >= 0 ? requestedStimulus.Priority : setting.priority;
        bool canInterruptChase = requestedStimulus.CanInterruptChase || setting.canInterruptChase;

        return new NoiseStimulus(
            requestedStimulus.Type,
            requestedStimulus.Position,
            requestedStimulus.Source,
            radius,
            duration,
            canInterruptChase,
            priority,
            requestedStimulus.CreatedTime,
            requestedStimulus.AnchorTransform);
    }

    private NoiseTypeSetting GetNoiseSetting(NoiseType noiseType)
    {
        if (cachedSettings.TryGetValue(noiseType, out NoiseTypeSetting setting))
        {
            return setting;
        }

        return new NoiseTypeSetting
        {
            type = noiseType,
            defaultRadius = 8.0f,
            defaultDuration = 1.0f,
            priority = 0,
            canInterruptChase = false,
            debugColor = Color.white,
        };
    }

    private NoiseSourceObject CreateNoiseSourceObject(NoiseStimulus stimulus)
    {
        // 공용 프리팹이 연결되어 있으면 그 오브젝트를 생성하고,
        // 없으면 코드로 최소 구성 오브젝트를 만들어도 기능은 끊기지 않게 합니다.
        NoiseSourceObject spawnedNoiseSource = null;

        if (noiseSourcePrefab != null)
        {
            spawnedNoiseSource = Instantiate(noiseSourcePrefab, stimulus.Position, Quaternion.identity);
            spawnedNoiseSource.name = $"Noise_{stimulus.Type}";
        }
        else
        {
            GameObject noiseObject = new($"Noise_{stimulus.Type}");
            noiseObject.transform.SetPositionAndRotation(stimulus.Position, Quaternion.identity);
            spawnedNoiseSource = noiseObject.AddComponent<NoiseSourceObject>();
        }

        spawnedNoiseSource.Initialize(stimulus, GetNoiseSetting(stimulus.Type).debugColor);
        return spawnedNoiseSource;
    }
}
