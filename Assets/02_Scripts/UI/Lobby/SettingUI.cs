using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("볼륨 조절 UI")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Header("그래픽 설정 UI")]
    [SerializeField] Button fullScreen;

    private void Start()
    {
        // 첫 실행 시 볼륨 값을 불러와 UI 슬라이더에 반영 (이벤트 발생 방지)
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 0.5f));
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        // 슬라이더를 조종할 이벤트 연결
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    public void SetMasterVolume(float _vol)
    {
        GlobalEventBus.OnMasterVolumeChanged?.Invoke(_vol);
    }

    public void SetBGMVolume(float _vol)
    {
        GlobalEventBus.OnBGMVolumeChanged?.Invoke(_vol);
    }

    public void SetSFXVolume(float _vol)
    {
        GlobalEventBus.OnSFXVolumeChanged?.Invoke(_vol);
    }
}
