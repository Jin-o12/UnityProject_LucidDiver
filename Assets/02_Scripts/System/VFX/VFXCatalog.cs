using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// VFX가 재생되는 좌표 기준입니다.
/// 실제 VFX 종류는 enum이 아니라 Excel/JSON에서 전달되는 문자열 ID로 구분합니다.
/// </summary>
public enum VFXAttachType
{
    World,
    Parent
}

/// <summary>
/// 재생을 마친 VFX를 풀로 돌려보내는 조건입니다.
/// </summary>
public enum VFXReturnType
{
    ParticleStopped,
    Duration,
    Manual
}

/// <summary>
/// 문자열 VFX ID와 Addressable 프리팹 및 풀 설정을 연결합니다.
/// </summary>
[Serializable]
public sealed class VFXCatalogEntry
{
    [Tooltip("Excel/JSON에서 참조할 VFX ID입니다. 예: VFX_DarkSpirit_Hit")]
    [SerializeField] private string vfxId;

    [Tooltip("재생할 Addressable VFX 프리팹입니다.")]
    [SerializeField] private AssetReferenceGameObject prefabReference;

    [Header("Pool")]
    [Tooltip("인게임 진입 시 프리팹을 미리 로드하고 풀을 생성합니다.")]
    [SerializeField] private bool preload;
    [Min(0)] [SerializeField] private int initialPoolSize = 3;
    [Min(1)] [SerializeField] private int maxPoolSize = 10;

    [Header("Playback")]
    [SerializeField] private VFXAttachType attachType = VFXAttachType.World;
    [SerializeField] private VFXReturnType returnType = VFXReturnType.ParticleStopped;
    [Min(0.01f)] [SerializeField] private float duration = 1f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;
    [Min(0.01f)] [SerializeField] private float scaleMultiplier = 1f;

    public string VfxId => vfxId;
    public AssetReferenceGameObject PrefabReference => prefabReference;
    public bool Preload => preload;
    public int InitialPoolSize => Mathf.Max(0, initialPoolSize);
    public int MaxPoolSize => Mathf.Max(1, maxPoolSize, initialPoolSize);
    public VFXAttachType AttachType => attachType;
    public VFXReturnType ReturnType => returnType;
    public float Duration => Mathf.Max(0.01f, duration);
    public bool UseUnscaledTime => useUnscaledTime;
    public Vector3 PositionOffset => positionOffset;
    public Quaternion RotationOffset => Quaternion.Euler(rotationOffset);
    public float ScaleMultiplier => Mathf.Max(0.01f, scaleMultiplier);
}

/// <summary>
/// VFX ID별 Addressable과 풀 설정을 보관하는 카탈로그입니다.
/// Assets/Resources/VFX/VFXCatalog 에셋에 사용자가 VFX 프리팹을 직접 연결합니다.
/// </summary>
[CreateAssetMenu(fileName = "VFXCatalog", menuName = "Lucid Diver/VFX/VFX Catalog")]
public sealed class VFXCatalog : ScriptableObject
{
    [SerializeField] private List<VFXCatalogEntry> entries = new List<VFXCatalogEntry>();

    private Dictionary<string, VFXCatalogEntry> entryById;

    public IReadOnlyList<VFXCatalogEntry> Entries => entries;

    /// <summary>
    /// JSON에서 받은 ID로 VFX 설정을 조회합니다.
    /// </summary>
    public bool TryGetEntry(string vfxId, out VFXCatalogEntry entry)
    {
        BuildLookupIfNeeded();
        return entryById.TryGetValue(vfxId ?? string.Empty, out entry);
    }

    private void OnEnable()
    {
        entryById = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        entryById = null;
        HashSet<string> registeredIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (VFXCatalogEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.VfxId))
                continue;

            if (!registeredIds.Add(entry.VfxId))
                Debug.LogWarning($"[VFXCatalog] 중복된 VFX ID가 있습니다: {entry.VfxId}", this);
        }
    }
#endif

    private void BuildLookupIfNeeded()
    {
        if (entryById != null)
            return;

        entryById = new Dictionary<string, VFXCatalogEntry>(StringComparer.Ordinal);
        foreach (VFXCatalogEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.VfxId))
                continue;

            if (entryById.ContainsKey(entry.VfxId))
            {
                Debug.LogWarning($"[VFXCatalog] 중복 ID는 첫 번째 설정을 사용합니다: {entry.VfxId}", this);
                continue;
            }

            entryById.Add(entry.VfxId, entry);
        }
    }
}
