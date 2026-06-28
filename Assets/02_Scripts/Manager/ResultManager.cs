using System.Collections.Generic;
using UnityEngine;
public class ResultManager : MonoBehaviour, IResultService
{
    public static ResultManager Instance { get; private set; }  //싱글톤 인스턴스 지정
    // playerID -> PlayerStatus 매핑 딕셔너리
    public readonly Dictionary<int, PlayerStatus> _players = new Dictionary<int, PlayerStatus>();
    private void Awake()
    {
        // 싱글톤 인스턴스 중복 방지 설정
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
        // ResultServiceLocator에 자신을 등록
        ResultServiceLocator.Instance = this;
        Debug.Log("ResultManager Awake - registered to ResultServiceLocator");

        // 씬에 이미 존재하는 PlayerStatus를 찾아 등록 (타이밍 안전성 보장)
        foreach (var p in FindObjectsOfType<PlayerStatus>())
        {
            if (p == null) continue;
            var idComp = p.GetComponent<EntityIdentity>();
            if (idComp == null)
            {
                Debug.LogWarning($"ResultManager Awake: EntityIdentity 없음 - gameObject={p.gameObject.name}");
                continue;
            }
            // Register는 내부적으로 동일 key를 덮어쓰므로 중복 걱정 없음
            Register(idComp.entityID, p);
            Debug.Log($"ResultManager Awake: Registered existing playerID={idComp.entityID} (gameObject={p.gameObject.name})");
        }

        //로비로 돌아가기 버튼에 동조율 데이터 갱신 연결
        GlobalEventBus.OnSetRecordData += RenewLinkRateData;
        //로비로 돌아가기 버튼에 결과 창 닫기 연결
        GlobalEventBus.OnReturnToLobby += CloseResultPanel;
        //출격 준비 UI 오픈 이벤트 연결
        GlobalEventBus.PrepareUIOpen += SendQuickSlotCacheEvent;
        //다이버/기록 UI 오픈 이벤트 연결
        GlobalEventBus.RecordUIOpen += SendLinkRecordData;
        //다이버/기록 UI 읽음 이벤트 연결
        GlobalEventBus.OnRecordRead += NewMemoryRead;
    }

    private void OnDestroy()  //IResultService 구현체 (로케이터에 등록)
    {
        if (ResultServiceLocator.Instance == (IResultService)this) ResultServiceLocator.Instance = null;
        if (Instance == this) Instance = null;
        GlobalEventBus.OnSetRecordData -= RenewLinkRateData;
        GlobalEventBus.OnReturnToLobby -= CloseResultPanel;
        GlobalEventBus.PrepareUIOpen -= SendQuickSlotCacheEvent;
        GlobalEventBus.RecordUIOpen -= SendLinkRecordData;
        GlobalEventBus.OnRecordRead -= NewMemoryRead;
    }

    // 플레이어 등록
    public void Register(int playerID, Component ps)
    {
        // 플레이어 상태 값 null 체크를 먼저 실행
        if (ps == null) return;
        // 플레이어 EntityIdentity 컴포넌트를 가져오고 null 체크
        var idComp = ps.GetComponent<EntityIdentity>();
        if (idComp == null) return;
        // EntityIdentity에서 ID 값을 불러옴
        _players[playerID] = (PlayerStatus)ps;
    }

    // 플레이어 등록
    public void Register(int playerID, Component ps)
    {
        // 플레이어 상태 값 null 체크를 먼저 실행
        if (ps == null) return;
        // 플레이어 EntityIdentity 컴포넌트를 가져오고 null 체크
        var idComp = ps.GetComponent<EntityIdentity>();
        if (idComp == null) return;
        // EntityIdentity에서 ID 값을 불러옴
        _players[playerID] = (PlayerStatus)ps;
        Debug.Log($"ResultManager.Register: playerID={playerID} registered (obj={ps.gameObject.name})");
    }

    // 플레이어 등록 해제
    public void Unregister(int playerID)
    {
        // 플레이어 ID 값을 제외
        _players.Remove(playerID);
    }

    // 조회 유틸
    public Component GetPlayerComponent<PlayerStatue>(int playerID)
    {
        // playerID에 매핑된 PlayerStatus를 가져옴
        if (_players.TryGetValue(playerID, out var ps)) return ps;
        // 매핑되지 않았으면 null 처리
        else return null;
    }

    // 탈출 성공 처리
    public void HandleEscapeSuccess(int playerID)
    {
        SetPlayerState(playerID, PlayerStatus.livingState.escape);
    }

    // 탈출 실패 처리 (예: hp <= 0 시 호출)
    public void HandleEscapeFail(int playerID)
    {
        SetPlayerState(playerID, PlayerStatus.livingState.gameover);
    }

    // 플레이어 상태 변경
    private void SetPlayerState(int playerID, PlayerStatus.livingState state)
    {
        PlayerStatus ps = (PlayerStatus)GetPlayerComponent<PlayerStatus>(playerID);
        if (ps != null)
        {
            ps.nowState = state;
        }
    }
}
