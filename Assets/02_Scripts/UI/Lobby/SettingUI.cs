using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] Button closeButton;    //팝업 닫기 버튼

    [Header("볼륨 조절 UI")]
    [SerializeField] Slider masterSlider;   //전체 음량 조절
    [SerializeField] Slider bgmSlider;      //BGM 음량 조절
    [SerializeField] Slider sfxSlider;      //SFX 음량 조절
    [SerializeField] Slider uiSlider;       //UI 음량 조절
    [SerializeField] Slider ambSlider;      //환경음 음량 조절

    [Header("그래픽 설정 UI")]
    [SerializeField] Toggle fullscreenToggle;   //전체화면 토글

    private void Start()
    {
        // 첫 실행 시 볼륨 값을 불러와 UI 슬라이더에 반영 (이벤트 발생 방지)
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 0.5f));
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        if (uiSlider != null) uiSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("UIVolume", 0.5f));
        if (ambSlider != null) ambSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("AmbVolume", 0.5f));
        
        // 풀스크린 토글의 초기 값을 읽어 표기
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        // 슬라이더를 조종할 이벤트 연결
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiSlider != null) uiSlider.onValueChanged.AddListener(SetUIVolume);
        if (ambSlider != null) ambSlider.onValueChanged.AddListener(SetAmbVolume);

        // 토글을 조종할 이벤트 연결
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // 팝업 닫기 버튼 이벤트 연결
        if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
        if (uiSlider != null) uiSlider.onValueChanged.RemoveListener(SetUIVolume);
        if (ambSlider != null) ambSlider.onValueChanged.RemoveListener(SetAmbVolume);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePopup);
    }

    // 전체 음량 세팅
    public void SetMasterVolume(float _vol)
    {
        GlobalEventBus.OnMasterVolumeChanged?.Invoke(_vol);
    }

    // BGM 음량 세팅
    public void SetBGMVolume(float _vol)
    {
        GlobalEventBus.OnBGMVolumeChanged?.Invoke(_vol);
    }

    // SFX 음량 세팅
    public void SetSFXVolume(float _vol)
    {
        GlobalEventBus.OnSFXVolumeChanged?.Invoke(_vol);
    }

    // UI 음량 세팅
    public void SetUIVolume(float _vol)
    {
        GlobalEventBus.OnUIVolumeChanged?.Invoke(_vol);
    }

    // 환경음 음량 세팅
    public void SetAmbVolume(float _vol)
    {
        GlobalEventBus.OnAmbVolumeChanged?.Invoke(_vol);
    }

    // 풀스크린 여부 체크 동작
    public void SetFullscreen(bool _bool)
    {
        // UI 터치 사운드 출력
        GlobalEventBus.OnClickAudio?.Invoke(true);

        // 토글 상태에 따라 풀스크린 출력 여부 값 변경
        Screen.fullScreen = _bool;
        Debug.Log($"빌드 시 풀스크린으로 출력: {_bool}");
    }

    // 팝업 닫기 버튼 동작
    public void ClosePopup()
    {
        // UI 터치 사운드 출력
        GlobalEventBus.OnClickAudio?.Invoke(true);

        GlobalEventBus.OnOpenSettingUI?.Invoke();
    }
}
