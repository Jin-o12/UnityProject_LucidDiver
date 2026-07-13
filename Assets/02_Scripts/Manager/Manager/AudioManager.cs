using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AudioManager : MonoBehaviour
{
    public AudioManager Instance;

    [Header("음량 조절")]
    public float masterVolume = 1.0f;   //전체 사운드 음량
    public float BGMVolume = 1.0f;      //BGM 음량
    public float SFXVolume = 1.0f;      //SFX 음량
    public float UIVolume = 1.0f;       //UI 사운드 음량

    public float defaultMasterVolume = 1.0f;   //전체 사운드 음량 기본값
    public float defaultBGMVolume = 1.0f;      //BGM 음량 기본값
    public float defaultSFXVolume = 1.0f;      //SFX 음량 기본값
    public float defaultUIVolume = 1.0f;       //UI 사운드 음량 기본값

    [Header("음소거 체크")]
    public bool masterMute = false;     //전체 사운드 음소거
    public bool BGMMute = false;        //BGM 음소거
    public bool SFXMute = false;        //SFX 음소거
    public bool UIMute = false;         //UI 사운드 음소거

    [Header("오디오 컴포넌트")]
    public AudioSource BGMSource;       //BGM 오디오 소스
    public AudioSource SFXSource;       //SFX 오디오 소스
    public AudioSource UISource;        //UI 사운드 오디오 소스

    [Header("세이브 키")]
    public const string MasterVolumeKey = "MasterVolume";   //전체 사운드 음량 키
    public const string BGMVolumeKey = "BGMVolume";         //BGM 음량 키
    public const string SFXVolumeKey = "SFXVolume";         //SFX 음량 키
    public const string UIVolumeKey = "UIVolume";           //UI 사운드 음량 키
    public const string MasterMuteKey = "MasterMute";       //전체 사운드 음소거 키
    public const string BGMMuteKey = "BGMMute";             //BGM 음소거 키
    public const string SFXMuteKey = "SFXMute";             //SFX 음소거 키
    public const string UIMuteKey = "UIMute";               //UI 사운드 음소거 키

    // <AudioID, AudioClip> 캐시 딕셔너리
    public Dictionary<int, AudioClip> clipCache = new Dictionary<int, AudioClip>();

    private void Awake()
    {
        // 싱글톤 인스턴스 중복 방지 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // 클라이언트 PlayerPrefs에서 오디오 설정을 불러옴
        LoadAudioSettings();

        // 사운드 재생 요청 이벤트 구독
        GlobalEventBus.OnBGMPlayRequested += PlayBGM;
        GlobalEventBus.On2DSoundPlayRequested += Play2DSound;
        GlobalEventBus.On3DSoundPlayRequested += Play3DSound;

        // 사운드 종료 요청 이벤트 구독
        GlobalEventBus.OnBGMStopRequested += StopBGM;
        GlobalEventBus.On2DSoundStopRequested += Stop2DSound;
        GlobalEventBus.On3DSoundStopRequested += Stop3DSound;
    }

    private void OnDestroy()
    {
        GlobalEventBus.OnBGMPlayRequested -= PlayBGM;
        GlobalEventBus.On2DSoundPlayRequested -= Play2DSound;
        GlobalEventBus.On3DSoundPlayRequested -= Play3DSound;

        GlobalEventBus.OnBGMStopRequested += StopBGM;
        GlobalEventBus.On2DSoundStopRequested += Stop2DSound;
        GlobalEventBus.On3DSoundStopRequested += Stop3DSound;
    }

    #region 사운드 재생 / 중단
    // BGM 재생 요청 처리
    public void PlayBGM(int audioID)
    {
        BGMSource.Stop();
        BGMSource.clip = clipCache[audioID];
        BGMSource.Play();
    }

    // 2D 사운드 재생 요청 처리
    private void Play2DSound(int audioID)
    {
        throw new NotImplementedException();
    }

    // 3D 사운드 재생 요청 처리
    private void Play3DSound(int audioID, Vector3 sourcePosition)
    {
        throw new NotImplementedException();
    }

    // BGM 재생 중단 처리
    public void StopBGM()
    {
        BGMSource.Stop();
    }

    // 2D 사운드 재생 중단 처리
    public void Stop2DSound(int audioID)
    {
        
    }

    // 3D 사운드 재생 중단 처리
    public void Stop3DSound(AudioSource source)
    {
        Destroy(source);
    }
    #endregion

    #region 음량 관리
    // 음량 값 적용
    public void ApplyVolume()
    {
        AudioListener.volume = masterMute ? 0 : 1;
        if (BGMSource != null ) BGMSource.volume = masterVolume * BGMVolume * (BGMMute ? 0 : 1);
        if (UISource != null) UISource.volume = masterVolume * UIVolume * (UIMute ? 0 : 1);
        if (SFXSource != null) SFXSource.volume = masterVolume * SFXVolume * (SFXMute ? 0 : 1);
    }

    // 사운드 설정 데이터 저장
    public void SaveAudioSettings()
    {
        // 음량 값 저장
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(BGMVolumeKey, BGMVolume);
        PlayerPrefs.SetFloat(SFXVolumeKey, SFXVolume);
        PlayerPrefs.SetFloat(UIVolumeKey, UIVolume);

        // 음소거 값 저장 (true = 1 / false = 0 번역)
        PlayerPrefs.SetInt(MasterMuteKey, masterMute ? 1 : 0);
        PlayerPrefs.SetInt(BGMMuteKey, BGMMute ? 1 : 0);
        PlayerPrefs.SetInt(SFXMuteKey, SFXMute ? 1 : 0);
        PlayerPrefs.SetInt(UIMuteKey, UIMute ? 1 : 0);

        // 저장한 값을 기기에 적용
        PlayerPrefs.Save();
    }

    // 사운드 설정 데이터 불러오기
    public void LoadAudioSettings()
    {
        //음량 값 불러오기
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1.0f);
        BGMVolume = PlayerPrefs.GetFloat(BGMVolumeKey, 1.0f);
        SFXVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1.0f);
        UIVolume = PlayerPrefs.GetFloat(UIVolumeKey, 1.0f);

        //음소거 값 불러오기 (true = 1 / false = 0 번역)
        masterMute = PlayerPrefs.GetInt(MasterMuteKey, 0) == 1;
        BGMMute = PlayerPrefs.GetInt(BGMMuteKey, 0) == 1;
        SFXMute = PlayerPrefs.GetInt(SFXMuteKey, 0) == 1;
        UIMute = PlayerPrefs.GetInt(UIMuteKey, 0) == 1;

        // 불러온 값으로 업데이트
        ApplyVolume();
    }
    #endregion
}