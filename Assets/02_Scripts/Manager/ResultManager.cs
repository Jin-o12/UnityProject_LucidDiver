using System.Collections.Generic;
using UnityEngine;
public class ResultManager : MonoBehaviour/*, IResultService*/
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
    }

    public void RefreshPlayerCache()
    {
        // 기존 매핑 중 파괴된 오브젝트 제거
        var keysToRemove = new List<int>();
        foreach (var kv in _players)
        {
            if (kv.Value == null) keysToRemove.Add(kv.Key);
        }
        foreach (var key in keysToRemove)
        {
            _players.Remove(key);
            Debug.Log($"ResultManager.RefreshPlayerCache: Removed null playerID={key}");
        }

        // 씬의 모든 PlayerStatus를 스캔해 등록
        foreach (var p in FindObjectsOfType<PlayerStatus>())
        {
            if (p == null) continue;
            if (!p.TryGetComponent<EntityIdentity>(out var idComp)) { Debug.LogWarning($"ResultManager.RefreshPlayerCache: EntityIdentity 없음 - {p.gameObject.name}"); continue; }
            if (!_players.ContainsKey(idComp.entityID))
            {
                _players[idComp.entityID] = p;
                Debug.Log($"ResultManager.RefreshPlayerCache: Registered playerID={idComp.entityID} ({p.gameObject.name})");
            }
        }
    }

    // // 플레이어 등록
    // public void Register(int playerID, Component ps)
    // {
    //     // 플레이어 상태 값 null 체크를 먼저 실행
    //     if (ps == null) return;
    //     // 플레이어 EntityIdentity 컴포넌트를 가져오고 null 체크
    //     var idComp = ps.GetComponent<EntityIdentity>();
    //     if (idComp == null) return;
    //     // EntityIdentity에서 ID 값을 불러옴
    //     _players[playerID] = (PlayerStatus)ps;
    //     Debug.Log($"ResultManager.Register: playerID={playerID} registered (obj={ps.gameObject.name})");
    // }

    // 플레이어 등록 해제
    public void Unregister(int playerID)
    {
        // 플레이어 ID 값을 제외
        _players.Remove(playerID);
    }

    // 플레이어가 requester 자신인 경우에 등록 해제
    public void UnregisterIfOwner(int playerID, Component requester)
    {
        // 딕셔너리에 등록된 것이 requester 자신인 경우에만 제거
        if (_players.TryGetValue(playerID, out var current) && current == requester)
        {
            _players.Remove(playerID);
        }
    }

    // 조회 유틸
    public Component GetPlayerComponent(int playerID)
    {
        // playerID에 매핑된 컴포넌트를 가져옴
        if (_players.TryGetValue(playerID, out var ps)) return ps;
        // 매핑되지 않았으면 null 처리
        return null;
    }

    // 탈출 성공 처리
    public void HandleEscapeSuccess(int playerID) => SetPlayerState(playerID, PlayerStatus.livingState.escape);

    // 탈출 실패 처리
    public void HandleEscapeFail(int playerID) => SetPlayerState(playerID, PlayerStatus.livingState.gameover);

    // 플레이어 상태 변경
    private void SetPlayerState(int playerID, PlayerStatus.livingState state)
    {
        if (_players.TryGetValue(playerID, out var ps)) ps.SetPlayerState(state);
    }
}
