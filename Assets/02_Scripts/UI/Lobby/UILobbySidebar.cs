using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비의 각 씬(출격, 다이버, 창고, 로비 메인) 간 이동을 지원하는
/// 왼쪽 내비게이션 사이드바(LeftSidebar) 공통 컴포넌트입니다.
/// </summary>
public class UILobbySidebar : MonoBehaviour
{
    [Header("Lobby Navigation Buttons")]
    [SerializeField] private Button buttonNavSortie;    // 출격 버튼
    [SerializeField] private Button buttonNavDiver;     // 다이버 버튼
    [SerializeField] private Button buttonNavStorage;   // 창고 버튼
    [SerializeField] private Button buttonNavLobby;     // 로비 버튼

    private void OnEnable()
    {
        if (buttonNavSortie != null) buttonNavSortie.onClick.AddListener(OnClickNavSortie);
        if (buttonNavDiver != null) buttonNavDiver.onClick.AddListener(OnClickNavDiver);
        if (buttonNavStorage != null) buttonNavStorage.onClick.AddListener(OnClickNavStorage);
        if (buttonNavLobby != null) buttonNavLobby.onClick.AddListener(OnClickNavLobby);
    }

    private void OnDisable()
    {
        if (buttonNavSortie != null) buttonNavSortie.onClick.RemoveListener(OnClickNavSortie);
        if (buttonNavDiver != null) buttonNavDiver.onClick.RemoveListener(OnClickNavDiver);
        if (buttonNavStorage != null) buttonNavStorage.onClick.RemoveListener(OnClickNavStorage);
        if (buttonNavLobby != null) buttonNavLobby.onClick.RemoveListener(OnClickNavLobby);
    }

    private void OnClickNavSortie()
    {
        GlobalEventBus.OnClickAudio?.Invoke(true);
        GlobalEventBus.OnOpenPrepareUI?.Invoke();
    }

    private void OnClickNavDiver()
    {
        GlobalEventBus.OnClickAudio?.Invoke(true);
        GlobalEventBus.OnOpenRecordUI?.Invoke();
    }

    private void OnClickNavStorage()
    {
        GlobalEventBus.OnClickAudio?.Invoke(true);
        GlobalEventBus.OnOpenStorageUI?.Invoke();
    }

    private void OnClickNavLobby()
    {
        GlobalEventBus.OnClickAudio?.Invoke(true);
        GlobalEventBus.OnOpenLobbyUI?.Invoke();
    }
}
