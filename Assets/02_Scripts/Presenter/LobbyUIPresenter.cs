using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyUIPresenter : MonoBehaviour
{
    UIManager uiManager;        // UI 메니저 인스턴스
    /* BGM ID 리스트 */
    private readonly int diversBGMAudioID = 10001;  // 다이버/기록 BGM ID
    private readonly int storageBGMAudioID = 10003; // 창고 BGM ID
    private readonly int lobbyBGMAudioID = 10004;   // 로비 BGM ID
    private readonly int sortieBGMAudioID = 10005;  // 출격 준비 BGM ID
    private readonly int recordBGMAudioID = 10008;  // 기록 카드 BGM ID

    private InputAction openSettingAction;          // 설정 창 열기 이벤트
    private SettingUI settingUI;                    // 설정 UI 캐시

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnOpenLobbyUI += OpenLobby;
        GlobalEventBus.OnOpenPrepareUI += OpenSortiePrepare;
        GlobalEventBus.OnOpenRecordUI += OpenDiverRecord;
        GlobalEventBus.OnOpenStorageUI += OpenStorageInventory;
        GlobalEventBus.OnOpenRecordCardPopUpUI += OpenRecordCardPopUp;
        GlobalEventBus.OnOpenSettingUI += OpenSettingPopup;

        openSettingAction = InputManager.Instance.GetAction("Lobby", "Setting");
        if (openSettingAction != null)
        {
            openSettingAction.Enable();
            openSettingAction.performed += OpenSettingUI;
        }
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnOpenLobbyUI -= OpenLobby;
        GlobalEventBus.OnOpenPrepareUI -= OpenSortiePrepare;
        GlobalEventBus.OnOpenRecordUI -= OpenDiverRecord;
        GlobalEventBus.OnOpenStorageUI -= OpenStorageInventory;
        GlobalEventBus.OnOpenRecordCardPopUpUI -= OpenRecordCardPopUp;
        GlobalEventBus.OnOpenSettingUI -= OpenSettingPopup;

        if (openSettingAction != null)
        {
            openSettingAction.performed -= OpenSettingUI;
            openSettingAction.Disable();
        }
    }

    private void Start()
    {
        uiManager = UIManager.Instance;
        OpenLobby();
    }

    /* 로비 UI 전환 */
    public void OpenLobby()
    {
        // 현재 열려있는 캔버스를 닫는다
        uiManager.CloseNowUI();

        // 로비 UI를 활성화하고 BGM 재생
        uiManager.Open<LobbyMainUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(lobbyBGMAudioID);
    }

    /* 준비 UI 전환 */
    public void OpenSortiePrepare()
    {
        // 현재 열려있는 캔버스를 닫는다
        uiManager.CloseNowUI();

        // 준비 UI를 활성화하고 BGM 재생
        uiManager.Open<SortiePrepareUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(sortieBGMAudioID);
    }

    /* 기록 UI 전환 */
    public void OpenDiverRecord()
    {
        // 현재 열려있는 캔버스를 닫는다
        uiManager.CloseNowUI();

        // 기록 UI를 활성화하고 BGM 재생
        uiManager.Open<DiverRecordUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(diversBGMAudioID);
    }

    /* 창고 UI 전환 */
    public void OpenStorageInventory()
    {
        // 현재 열려있는 캔버스를 닫는다
        uiManager.CloseNowUI();

        // 창고 UI를 활성화하고 BGM 재생
        uiManager.Open<StorageInventoryUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(storageBGMAudioID);
    }

    /* 기록 카드 팝업 UI 전환 */
    public void OpenRecordCardPopUp(string title, CharacterTID tid)
    {
        // 현재 열려있는 캔버스를 닫는다
        uiManager.CloseNowUI();

        // 기록 카드 팝업 UI를 활성화하고 BGM 재생
        RecordCardPopUpUI popup = uiManager.Open<RecordCardPopUpUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(recordBGMAudioID);

        // 기록 제목과 캐릭터 ID를 팝업에 전달한다
        popup.SetData(title, tid);
    }

    /* 설졍 UI 열기 */
    public void OpenSettingUI(InputAction.CallbackContext context)
    {
        OpenSettingPopup();
    }

    public void OpenSettingPopup()
    {
        if (settingUI == null)
        {
            settingUI = uiManager.Open<SettingUI>();
            return;
        }

        if (settingUI.gameObject.activeInHierarchy == false)
        {
            uiManager.Open<SettingUI>();
        }
        else
        {
            uiManager.Close<SettingUI>();
        }
    }
}