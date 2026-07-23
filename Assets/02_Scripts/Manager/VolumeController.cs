using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬의 Global Volume을 찾아 Vignette(또는 다른 포스트 프로세스)를 제어하는 싱글톤 컴포넌트.
/// DemoScene의 System/ManagerAD에 추가해서 사용합니다.
/// </summary>
public class VolumeController : MonoBehaviour
{
    private static VolumeController instance;
    public static VolumeController Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<VolumeController>();
#else
                instance = FindObjectOfType<VolumeController>();
#endif
            }

            return instance;
        }
        private set => instance = value;
    }

    [Header("검색 설정")]
    [Tooltip("씬에 있는 Global Volume 오브젝트 이름이 고정되어 있으면 지정하세요. 비워두면 첫 번째 Volume을 사용합니다.")]
    [SerializeField] private string globalVolumeObjectName = "GlobalVolume";

    private Volume cachedVolume;
    private Vignette cachedVignette;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 씬 전용 싱글톤: 동일 타입이 존재하면 컴포넌트만 제거
        VolumeController current = Instance;
        if (current != null && current != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        // 씬 로드 시 다시 탐색하도록 구독 (DemoScene 등 씬 전환 시 안전)
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 이벤트를 구독하여 UI 단계에서 전송한 신호를 받음
        GlobalEventBus.OnVignetteChange += FadeVignette;

        TryLocateVolume();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        GlobalEventBus.OnVignetteChange -= FadeVignette;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 변경되면 새 Volume을 찾음
        TryLocateVolume();
    }

    /// <summary>
    /// 현재 씬에서 Volume과 Vignette를 찾아 캐시합니다.
    /// </summary>
    public bool TryLocateVolume()
    {
        // 이미 유효하면 갱신 시도(프로파일 변경 등)
        if (cachedVolume != null)
        {
            if (cachedVignette == null && cachedVolume.profile != null)
                cachedVolume.profile.TryGet<Vignette>(out cachedVignette);
            return cachedVolume != null;
        }

        Volume vol = null;

        if (!string.IsNullOrEmpty(globalVolumeObjectName))
        {
            GameObject go = GameObject.Find(globalVolumeObjectName);
            if (go != null)
                vol = go.GetComponent<Volume>();
        }

#if UNITY_2023_1_OR_NEWER
        if (vol == null)
            vol = FindFirstObjectByType<Volume>();
#else
        if (vol == null)
            vol = FindObjectOfType<Volume>();
#endif

        if (vol == null)
            return false;

        cachedVolume = vol;

        if (cachedVolume.profile != null)
            cachedVolume.profile.TryGet<Vignette>(out cachedVignette);

        return cachedVolume != null;
    }

    #region Vignette 컨트롤
    /// <summary>
    /// Vignette 사용 가능 여부.
    /// </summary>
    public bool IsVignetteAvailable() => cachedVignette != null;

    /// <summary>
    /// Vignette intensity를 즉시 설정합니다. (0..1)
    /// </summary>
    public void SetVignetteIntensity(float intensity, bool ensureActive = true)
    {
        if (!TryLocateVolume())
        {
            Debug.LogWarning("VolumeController: Volume을 찾지 못해 Vignette 설정을 보류합니다.");
            return;
        }

        if (cachedVignette == null)
        {
            Debug.LogWarning("VolumeController: Vignette가 VolumeProfile에 없습니다.");
            return;
        }

        intensity = Mathf.Clamp01(intensity);
        try
        {
            cachedVignette.intensity.value = intensity;
            if (ensureActive)
                cachedVignette.active = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"VolumeController: Vignette 설정 실패 - {ex.Message}");
        }
    }

    /// <summary>
    /// Vignette intensity를 duration 시간 동안 선형 보간하여 페이드합니다.
    /// </summary>
    public void FadeVignette(float targetIntensity, float duration, bool ensureActive = true)
    {
        if (!TryLocateVolume())
        {
            Debug.LogWarning("VolumeController: Volume을 찾지 못해 페이드를 수행할 수 없습니다.");
            return;
        }

        if (cachedVignette == null)
        {
            Debug.LogWarning("VolumeController: Vignette가 VolumeProfile에 없습니다.");
            return;
        }

        targetIntensity = Mathf.Clamp01(targetIntensity);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCoroutine(targetIntensity, duration, ensureActive));
    }

    private IEnumerator FadeCoroutine(float target, float duration, bool ensureActive)
    {
        if (cachedVignette == null)
            yield break;

        if (ensureActive)
            cachedVignette.active = true;

        float start = cachedVignette.intensity.value;
        float elapsed = 0f;
        if (duration <= 0f)
        {
            cachedVignette.intensity.value = target;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cachedVignette.intensity.value = Mathf.Lerp(start, target, t);
            yield return null;
        }

        cachedVignette.intensity.value = target;
        fadeCoroutine = null;
    }

    /// <summary>
    /// Vignette 활성/비활성 설정.
    /// </summary>
    public void SetVignetteEnabled(bool enabled)
    {
        if (!TryLocateVolume())
        {
            Debug.LogWarning("VolumeController: Volume을 찾지 못해 Vignette 활성화를 수행할 수 없습니다.");
            return;
        }

        if (cachedVignette == null)
        {
            Debug.LogWarning("VolumeController: Vignette가 VolumeProfile에 없습니다.");
            return;
        }

        cachedVignette.active = enabled;
    }
    #endregion
}