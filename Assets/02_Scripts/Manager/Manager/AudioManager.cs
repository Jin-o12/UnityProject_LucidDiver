using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;        //인스턴스
    public LocalJsonAudioRepository audioRepo;  //오디오 리포지토리

    // 음량 설정 세이브 키
    public const string MasterVolumeKey = "MasterVolume";   //전체 사운드 음량 키
    public const string BGMVolumeKey = "BGMVolume";         //BGM 음량 키
    public const string SFXVolumeKey = "SFXVolume";         //SFX 음량 키
    public const string UIVolumeKey = "UIVolume";           //UI 사운드 음량 키
    public const string MasterMuteKey = "MasterMute";       //전체 사운드 음소거 키
    public const string BGMMuteKey = "BGMMute";             //BGM 음소거 키
    public const string SFXMuteKey = "SFXMute";             //SFX 음소거 키
    public const string UIMuteKey = "UIMute";               //UI 사운드 음소거 키

    [Header("오디오 컴포넌트")]
    [SerializeField] private AudioSource BGMSource;       //BGM 오디오 소스
    [SerializeField] private AudioSource SFXSource;       //SFX 오디오 소스
    [SerializeField] private AudioSource UISource;        //UI 사운드 오디오 소스

    [Header("음량 조절")]
    [Range(0f, 1f)][SerializeField] private float masterVolume = 1.0f;   //전체 사운드 음량
    [Range(0f, 1f)][SerializeField] private float BGMVolume = 1.0f;      //BGM 음량
    [Range(0f, 1f)][SerializeField] private float SFXVolume = 1.0f;      //SFX 음량
    [Range(0f, 1f)][SerializeField] private float UIVolume = 1.0f;       //UI 사운드 음량
    [Header("음량 기본값")]
    [Range(0f, 1f)][SerializeField] private float defaultMasterVolume = 1.0f;   //전체 사운드 음량 기본값
    [Range(0f, 1f)][SerializeField] private float defaultBGMVolume = 1.0f;      //BGM 음량 기본값
    [Range(0f, 1f)][SerializeField] private float defaultSFXVolume = 1.0f;      //SFX 음량 기본값
    [Range(0f, 1f)][SerializeField] private float defaultUIVolume = 1.0f;       //UI 사운드 음량 기본값
    [Header("음소거 체크")]
    public bool masterMute = false;     //전체 사운드 음소거
    public bool BGMMute = false;        //BGM 음소거
    public bool SFXMute = false;        //SFX 음소거
    public bool UIMute = false;         //UI 사운드 음소거

    [Header("오디오 믹서")]
    [SerializeField] private AudioMixer mixer;                  //오디오 믹서
    [SerializeField] private AudioMixerGroup BGMMixerGroup;     //BGM 믹서 그룹
    [SerializeField] private AudioMixerGroup SFXMixerGroup;     //SFX 믹서 그룹
    [SerializeField] private AudioMixerGroup UIMixerGroup;      //UI 사운드 믹서 그룹
    private AudioMixerSnapshot Snapshot;                        //믹서 스냅샷

    [Header("UI 사운드")]
    [SerializeField] private int[] Click_AudioIDPool;           //클릭 시 사운드 ID 풀

    // <AudioID, AudioClip> 클립 딕셔너리
    public Dictionary<int, AudioClip> clipDict = new Dictionary<int, AudioClip>();

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

        // 믹서 그룹 및 스냅샷 설정 초기화
        AssignMixerOutputs();
        CacheMixerSnapshots();

        // 오디오 리포지토리를 불러옴
        audioRepo = new LocalJsonAudioRepository();

        // 불러온 리포지토리에 따라 오디오 클립 딕셔너리를 생성
        ClipCache();

        // 클라이언트 PlayerPrefs에서 오디오 설정을 불러옴
        LoadAudioSettings();

        // 사운드 재생 요청 이벤트 구독
        GlobalEventBus.OnPlayBGMRequested += PlayBGM;
        GlobalEventBus.OnPlay2DSoundRequested += Play2DSound;
        GlobalEventBus.OnPlay3DSoundRequested += Play3DSound;
        GlobalEventBus.OnPlay3DSoundRequestedWithHandle += Play3DSoundAndReturn;

        GlobalEventBus.OnClickAudio += PlayClickSound;          // UI 클릭 사운드 재생 이벤트

        // 사운드 종료 요청 이벤트 구독
        GlobalEventBus.OnStopBGMRequested += StopBGM;
        GlobalEventBus.OnStop2DSoundRequested += Stop2DSound;
        GlobalEventBus.OnStop3DSoundRequested += Stop3DSound;

        // Awake 처리 완료 시 디버그 콜
        Debug.Log("AudioManager Awake CALLED");
    }

    private void OnDestroy()
    {
        GlobalEventBus.OnPlayBGMRequested -= PlayBGM;
        GlobalEventBus.OnPlay2DSoundRequested -= Play2DSound;
        GlobalEventBus.OnPlay3DSoundRequested -= Play3DSound;
        GlobalEventBus.OnPlay3DSoundRequestedWithHandle -= Play3DSoundAndReturn;

        GlobalEventBus.OnClickAudio -= PlayClickSound;

        GlobalEventBus.OnStopBGMRequested -= StopBGM;
        GlobalEventBus.OnStop2DSoundRequested -= Stop2DSound;
        GlobalEventBus.OnStop3DSoundRequested -= Stop3DSound;
    }

    #region 데이터 및 변수 관리
    // 오디오 데이터의 파일 이름에 대응되는 클립을 캐시 딕셔너리에 저장
    private void ClipCache()
    {
        // 기존 딕셔너리를 클리어해 중복 방지
        clipDict.Clear();

        // Resources/Sound 폴더에서 오디오 클립 리스트를 찾기
        AudioClip[] _clips = Resources.LoadAll<AudioClip>("Sound");

        // 찾은 클립을 clipCache에 저장
        foreach (AudioClip clip in _clips)
        {
            if (audioRepo.TryGetAudioIDByClipName(clip.name, out int audioID))
            {
                // 같은 ID가 있으면 덮어쓰기
                clipDict[audioID] = clip;
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Resources/Sound/{clip.name}에 대응되는 AudioData를 찾을 수 없습니다.");
            }
        }
    }

    // 오디오 데이터(_data) 및 클립 파일(_clip)을 ID 값으로 찾아 꺼내기
    private void FindAudio(int audioID, out AudioData _data, out AudioClip _clip)
    {
        _data = audioRepo.GetAudioData(audioID);
        if (!clipDict.TryGetValue(audioID, out _clip)) 
        { 
            Debug.LogError($"Audio Clip Not Found : {audioID}"); 
            return; 
        }
    }

    // 오디오 데이터의 타입에 따라 오디오 소스 선택
    private AudioSource GetAudioSource(AudioType type)
    {
        return type switch
        {
            AudioType.BGM   => BGMSource,
            AudioType.SFX   => SFXSource,
            AudioType.UI    => UISource,
            _               => null         //Type 값이 없으면 null 처리
        };
    }
    #endregion

    #region 사운드 재생 / 중단
    // BGM 재생 요청 처리
    private void PlayBGM(int audioID)
    {
        FindAudio(audioID, out AudioData _data, out AudioClip _clip);
        if (_clip == null) return;

        // BGM 소스 설정 후 재생하기
        BGMSource.Stop();
        BGMSource.loop = true;
        BGMSource.clip = _clip;
        BGMSource.volume = CalculateVolume(_data);
        BGMSource.Play();
    }

    // 2D 사운드 재생 요청 처리
    private void Play2DSound(int audioID)
    {
        FindAudio(audioID, out AudioData _data, out AudioClip _clip);
        if (_clip == null) return;

        AudioSource _source = GetAudioSource(_data.AudioType);
        _source.volume = CalculateVolume(_data);

        //찾은 파일을 타입에 맞는 소스에서 재생
        if (_data.Loop)
        {
            // 루프 사운드인 경우 Source.clip에 지정해서 재생
            _source.clip = _clip;
            _source.loop = _data.Loop;
            _source.Play();
        }
        else
        {
            _source.PlayOneShot(_clip, CalculateVolume(_data));
        }
    }

    // 3D 사운드 재생 요청 처리 (루프하지 않는 일회용 사운드 오브젝트를 생성)
    private void Play3DSound(int audioID, Vector3 sourcePosition)
    {
        FindAudio(audioID, out AudioData _data, out AudioClip _clip);
        if (_clip == null) return;

        // 임시 오디오 소스를 재생할 오브젝트를 생성
        GameObject _tempObj = new($"Temp3DSound_{_data.AudioType}");
        _tempObj.transform.position = sourcePosition;

        // 임시 오디오 소스 설정
        AudioSource _source = _tempObj.AddComponent<AudioSource>();
        _source.clip = _clip;
        _source.spatialBlend = 1.0f; // 3D
        _source.rolloffMode = AudioRolloffMode.Logarithmic;
        _source.minDistance = 1f;
        _source.maxDistance = Mathf.Max(10f, _data.Volume * 50f);
        _source.volume = _data.Volume * _data.AudioType switch
        {
            AudioType.BGM   => BGMSource.volume,
            AudioType.SFX   => SFXSource.volume,
            AudioType.UI    => UISource.volume,
            _               => SFXSource.volume
        };
        _source.loop = _data.Loop;

        // 임시 오디오 소스 재생
        _source.Play();

        // 루프 사운드가 아닌 경우 클립 길이만큼 경과 시 제거
        if (_data.Loop == false) Destroy(_tempObj, _clip.length);
    }

    // 3D 루프 사운드 재생 요청 처리 (루프 요청을 제거할 수 있도록 GameObject를 out으로 리턴합니다)
    private GameObject Play3DSoundAndReturn(int audioID, Vector3 sourcePosition)
    {
        FindAudio(audioID, out AudioData _data, out AudioClip _clip);
        if (_clip == null) return null;

        // 임시 오디오 소스를 재생할 오브젝트를 생성
        GameObject tempObj = new($"Temp3DSound_{audioID}");
        tempObj.transform.position = sourcePosition;

        // 임시 오디오 소스 설정
        AudioSource src = tempObj.AddComponent<AudioSource>();
        src.clip = _clip;
        src.spatialBlend = 1.0f; // 3D
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 1f;
        src.maxDistance = Mathf.Max(10f, _data.Volume * 50f);
        src.volume = _data.Volume * _data.AudioType switch
        {
            AudioType.BGM => BGMSource.volume,
            AudioType.SFX => SFXSource.volume,
            AudioType.UI => UISource.volume,
            _ => SFXSource.volume
        };
        src.loop = _data.Loop;

        // 임시 오디오 소스 재생
        src.Play();

        // 루프 사운드가 아닌 경우 클립 길이만큼 경과 시 제거
        if (!_data.Loop) Destroy(tempObj, _clip.length);

        return tempObj;
    }

    // BGM 재생 중단 처리
    private void StopBGM()
    {
        BGMSource.Stop();
    }

    // 2D 사운드 재생 중단 처리
    private void Stop2DSound(int audioID)
    {
        AudioData _data = audioRepo.GetAudioData(audioID);
        AudioSource _source = GetAudioSource(_data.AudioType);
        if (_source.clip == clipDict[audioID]) _source.Stop();
    }

    // 3D 사운드 재생 중단 처리
    private void Stop3DSound(AudioSource source)
    {
        Destroy(source.gameObject);
    }

    // 모든 사운드 일괄 중단
    public void StopAll()
    {
        BGMSource.Stop();
        SFXSource.Stop();
        UISource.Stop();
    }
    #endregion

    #region 음량 관리
    // 음량 값 적용
    public void ApplyVolume()
    {
        AudioListener.volume = masterMute ? 0f : masterVolume;
        if (BGMSource != null ) BGMSource.volume =  BGMVolume * (BGMMute ? 0 : 1);
        if (UISource != null) UISource.volume = UIVolume * (UIMute ? 0 : 1);
        if (SFXSource != null) SFXSource.volume = SFXVolume * (SFXMute ? 0 : 1);
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
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        BGMVolume = PlayerPrefs.GetFloat(BGMVolumeKey, defaultBGMVolume);
        SFXVolume = PlayerPrefs.GetFloat(SFXVolumeKey, defaultSFXVolume);
        UIVolume = PlayerPrefs.GetFloat(UIVolumeKey, defaultUIVolume);

        //음소거 값 불러오기 (true = 1 / false = 0 번역)
        masterMute = PlayerPrefs.GetInt(MasterMuteKey, 0) == 1;
        BGMMute = PlayerPrefs.GetInt(BGMMuteKey, 0) == 1;
        SFXMute = PlayerPrefs.GetInt(SFXMuteKey, 0) == 1;
        UIMute = PlayerPrefs.GetInt(UIMuteKey, 0) == 1;

        // 불러온 값으로 음량 설정 업데이트
        ApplyVolume();
    }

    // 실제 출력할 최종 음량 계산
    public float CalculateVolume(AudioData data)
    {
        return data.AudioType switch
        {
            AudioType.BGM   =>  masterVolume * (masterMute ? 0 : 1) * data.Volume * BGMVolume * (BGMMute ? 0 : 1),
            AudioType.SFX   =>  masterVolume * (masterMute ? 0 : 1) * data.Volume * SFXVolume * (SFXMute ? 0 : 1),
            AudioType.UI    =>  masterVolume * (masterMute ? 0 : 1) * data.Volume * UIVolume * (UIMute ? 0 : 1),
            _               =>  0f
        };
    }
    #endregion

    #region 믹서 관리
    // 각 소스별 믹서 그룹 설정
    private void AssignMixerOutputs()
    {
        if (BGMSource != null && BGMMixerGroup != null)
        {
            BGMSource.outputAudioMixerGroup = BGMMixerGroup;
        }
        if (SFXSource != null && SFXMixerGroup != null)
        {
            SFXSource.outputAudioMixerGroup = SFXMixerGroup;
        }
        if (UISource != null && UIMixerGroup != null)
        {
            UISource.outputAudioMixerGroup = UIMixerGroup;
        }
    }

    // 믹서 스냅샷 설정
    private void CacheMixerSnapshots()
    {
        if (mixer == null)
        {
            Snapshot = null;
            return;
        }

        Snapshot = mixer.FindSnapshot("Snapshot");
    }
    #endregion

    #region 공통 사운드 재생
    private void PlayClickSound()
    {
        // 사운드 재생 이벤트를 AudioManager에 전달하여 오디오 재생
        int ShotAudioID = Click_AudioIDPool[UnityEngine.Random.Range(0, Click_AudioIDPool.Length)];
        Play2DSound(ShotAudioID);
    }

    #endregion
}