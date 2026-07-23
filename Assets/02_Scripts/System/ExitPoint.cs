using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitPoint : MonoBehaviour, IInteractable
{
    private bool isEscaping = false;                    //탈출 코루틴 실행 중인지 판정
    [SerializeField] private GameObject timerCanvas;    //탈출 타이머 캔버스 (P0에서는 사용 안함)
    [SerializeField] private float escapeTime = 3.0f;   //탈출 채널링 시간 (즉시 탈출하려면 0초)
    private Coroutine escapeCoroutine;                  //탈출 채털링 코루틴
    private PooledVFX activeEscapeVfx;                  //탈출 성공/취소까지 유지할 채널링 VFX
    private int escapeVfxRequestVersion;                //비동기 로드 중 취소된 요청을 구분하는 버전
    public event Action<float> timerOn;                 //타이머 출력 이벤트 

    [Header("Shot Audio")]
    public int[] Escape_Interact_AudioIDPool = null;    // 탈출 상호작용 사운드 ID 리스트
    public int[] Escape_Channeling_AudioIDPool = null;  // 탈출 시작 사운드 ID 리스트
    public int[] Escape_Success_AudioIDPool = null;     // 탈출 성공 사운드 ID 리스트
    public int[] Escape_Faild_AudioIDPool = null;       // 탈출 취소 사운드 ID 리스트
    public float failAudioTime = 1.5f;                  // 탈출 취소 사운드 재생 시간

    private void Awake()
    {
        ResolveTimerCanvasReference();

        // 초기 상태에서는 타이머 캔버스를 꺼둠
        SetTimerCanvasActive(false);
    }

    private void OnEnable()
    {
        // 탈출 중단 이벤트를 구독
        GlobalEventBus.OnEscapeFailure += EscapeFailure;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnEscapeFailure -= EscapeFailure;
        EndEscapeChannelVfx();
    }

    // 탈출 포인트 진입 시 상호작용 오디오 재생
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 사운드 재생 이벤트를 AudioManager에 전달하여 탈출 포인트 지점에서 3D 오디오 재생
            if (TryGetRandomAudioId(Escape_Interact_AudioIDPool, out int escapeInteractAudioID))
                GlobalEventBus.OnPlay3DSoundRequested?.Invoke(escapeInteractAudioID, gameObject.transform.position);
        }
    }

    public bool Interact(int playerID) 
    {
        Debug.Log($"player {playerID}가 탈출구와 상호작용 함");
        // 플레이어 상태가 idle이 아니면 탈출 판정을 시작하지 않음
        if (!PlayerStatus.IsPlayerIdle(playerID)) return false;
        // 이미 탈출 판정 중이면 탈출 판정을 중복해서 시작하지 않음
        if (isEscaping) return false;
        // 달리기 입력을 취소
        GlobalEventBus.SendCanSprint?.Invoke(false);
        // 탈출 타이머 시작
        escapeCoroutine = StartCoroutine(StartEscapeTimer(playerID));
        // 사운드 재생 이벤트를 AudioManager에 전달하여 탈출 포인트 지점에서 3D 오디오 재생
        if (TryGetRandomAudioId(Escape_Interact_AudioIDPool, out int escapeInteractAudioID))
            GlobalEventBus.OnPlay3DSoundRequested?.Invoke(escapeInteractAudioID, gameObject.transform.position);
        // 상호작용 성공, 상호작용 리스트에서 삭제 요청
        return false;
    }

    private IEnumerator StartEscapeTimer(int _playerID)  //채널링 후 탈출 성공 판정을 전달
    {
        //탈출 타이머 출력(P0에서는 사용 안함)
        Debug.Log("타이머 시작");
        SetTimerCanvasActive(true);

        if (timerCanvas != null)
        {
            timerOn?.Invoke(escapeTime);
        }

        //플레이어 상태를 escape로 변경하고 탈출 판정 시작
        ResultServiceLocator.Instance.HandleEscapeStart(_playerID);
        isEscaping = true;
        BeginEscapeChannelVfx();
        // 사운드 재생 이벤트를 AudioManager에 전달하여 2D 오디오 재생
        bool hasChannelingAudio = TryGetRandomAudioId(Escape_Channeling_AudioIDPool, out int escapeChannelingAudioID);
        if (hasChannelingAudio)
            GlobalEventBus.OnPlay2DSoundRequested?.Invoke(escapeChannelingAudioID);
        // 탈출 채널링 시간 동안 대기
        yield return new WaitForSeconds(escapeTime);

        // 채널링 종료 후 탈출 성공 판정 이벤트를 발송
        Debug.Log("타이머 종료");
        SetTimerCanvasActive(false);
        isEscaping = false;
        EndEscapeChannelVfx();
        VFXService.Instance?.Play(GameplayVFXIds.EscapeSuccess, transform.position, transform.rotation);
        GlobalEventBus.OnEscapeRequest?.Invoke(true);

        // 탈출 성공 시 채널링 사운드 오디오 소스를 제거
        if (hasChannelingAudio)
            GlobalEventBus.OnStop2DSoundRequested?.Invoke(escapeChannelingAudioID);

        // 사운드 재생 이벤트를 AudioManager에 전달하여 2D 오디오 재생
        if (TryGetRandomAudioId(Escape_Success_AudioIDPool, out int escapeSuccessAudioID))
            GlobalEventBus.OnPlay2DSoundRequested?.Invoke(escapeSuccessAudioID);
    }

    private void EscapeFailure(int _playerID)  //플레이어의 탈출 채널링 코루틴을 중단하는 판정을 전달
    {
        if (!isEscaping)
            return;

        //플레이어 상태를 idle로 변경하고 탈출 판정 중단
        ResultServiceLocator.Instance.HandleEscapeIdle(_playerID);
        isEscaping = false;
        EndEscapeChannelVfx();
        VFXService.Instance?.Play(GameplayVFXIds.EscapeCancel, transform.position, transform.rotation);

        //탈출 타이머 출력 종료
        SetTimerCanvasActive(isEscaping);

        //채널링 코루틴을 중단
        if (escapeCoroutine != null)
        {
            StopCoroutine(escapeCoroutine);
            escapeCoroutine = null;
        }

        StartCoroutine(EscapeFaildSound(failAudioTime));
    }

    private IEnumerator EscapeFaildSound(float soundTime)
    {
        // 사운드 재생 이벤트를 AudioManager에 전달하여 2D 오디오 재생
        if (!TryGetRandomAudioId(Escape_Faild_AudioIDPool, out int escapeFailAudioID))
            yield break;

        GlobalEventBus.OnPlay2DSoundRequested?.Invoke(escapeFailAudioID);

        // 정해진 시간 동안 재생 후 중단
        yield return new WaitForSeconds(soundTime);
        GlobalEventBus.OnStop2DSoundRequested?.Invoke(escapeFailAudioID);
    }

    private static bool TryGetRandomAudioId(int[] audioIdPool, out int audioId)
    {
        audioId = 0;
        if (audioIdPool == null || audioIdPool.Length == 0)
            return false;

        audioId = audioIdPool[UnityEngine.Random.Range(0, audioIdPool.Length)];
        return true;
    }

    /// <summary>
    /// Addressable 로드를 기다린 뒤 채널링이 여전히 진행 중일 때만 VFX 인스턴스를 유지합니다.
    /// </summary>
    private async void BeginEscapeChannelVfx()
    {
        VFXService service = VFXService.Instance;
        if (service == null)
            return;

        int requestVersion = ++escapeVfxRequestVersion;
        PooledVFX rentedVfx = await service.PlayAsync(
            GameplayVFXIds.EscapeChannel,
            VFXContext.At(transform.position, transform.rotation));

        if (this == null || !isEscaping || requestVersion != escapeVfxRequestVersion)
        {
            rentedVfx?.Release();
            return;
        }

        activeEscapeVfx = rentedVfx;
    }

    private void EndEscapeChannelVfx()
    {
        escapeVfxRequestVersion++;
        activeEscapeVfx?.Release();
        activeEscapeVfx = null;
    }

    private void ResolveTimerCanvasReference()
    {
        // 인스펙터 연결이 빠졌다면 자식 오브젝트에서 타이머 UI를 자동으로 탐색
        if (timerCanvas != null) return;

        EscapeTimer escapeTimer = GetComponentInChildren<EscapeTimer>(true);
        if (escapeTimer != null)
        {
            timerCanvas = escapeTimer.gameObject;
        }

        if (timerCanvas == null)
        {
            Debug.LogWarning($"{name} ExitPoint의 timerCanvas가 비어 있습니다. 타이머 UI 없이 탈출 기능만 동작합니다.", this);
        }
    }

    private void SetTimerCanvasActive(bool isActive)
    {
        // P0처럼 타이머 UI를 사용하지 않는 씬에서는 null 예외 없이 안전하게 넘어감
        if (timerCanvas == null) return;

        timerCanvas.SetActive(isActive);
    }
}
