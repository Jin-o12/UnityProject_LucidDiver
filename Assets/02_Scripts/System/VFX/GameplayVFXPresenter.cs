using UnityEngine;

/// <summary>
/// 위치 정보가 있는 전역 게임 이벤트를 공통 VFX 서비스에 연결합니다.
/// 충돌 지점처럼 정밀한 좌표가 필요한 이벤트는 각 판정 스크립트에서 직접 요청합니다.
/// </summary>
public sealed class GameplayVFXPresenter : MonoBehaviour
{
    private Transform currentPlayer;

    private void OnEnable()
    {
        GlobalEventBus.OnPlayerSpawned += HandlePlayerSpawned;
        GlobalEventBus.OnTimePenaltyRequested += HandleTimePenalty;
        GlobalEventBus.OnTimeOver += HandleTimeOver;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnPlayerSpawned -= HandlePlayerSpawned;
        GlobalEventBus.OnTimePenaltyRequested -= HandleTimePenalty;
        GlobalEventBus.OnTimeOver -= HandleTimeOver;
    }

    private void HandlePlayerSpawned(GameObject player)
    {
        if (player == null)
            return;

        currentPlayer = player.transform;
        VFXService.Instance?.Play(GameplayVFXIds.PlayerSpawn, currentPlayer.position, currentPlayer.rotation);
    }

    private void HandleTimePenalty(float penaltySeconds)
    {
        if (penaltySeconds <= 0.0f || !TryGetPlayerPosition(out Vector3 position))
            return;

        VFXService.Instance?.Play(GameplayVFXIds.TimePenalty, position);
    }

    private void HandleTimeOver()
    {
        if (TryGetPlayerPosition(out Vector3 position))
            VFXService.Instance?.Play(GameplayVFXIds.TimeOver, position);
    }

    private bool TryGetPlayerPosition(out Vector3 position)
    {
        if (currentPlayer == null)
        {
            PlayerStatus playerStatus = FindFirstObjectByType<PlayerStatus>();
            currentPlayer = playerStatus != null ? playerStatus.transform : null;
        }

        position = currentPlayer != null ? currentPlayer.position : Vector3.zero;
        return currentPlayer != null;
    }
}
