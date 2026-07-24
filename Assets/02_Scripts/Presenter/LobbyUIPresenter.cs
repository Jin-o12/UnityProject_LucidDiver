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
    private GameMenuUI gameMenuUI;                  // 게임 메뉴 UI 캐시

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnOpenLobbyUI += OpenLobby;
        GlobalEventBus.OnOpenPrepareUI += OpenSortiePrepare;
        GlobalEventBus.OnOpenRecordUI += OpenDiverRecord;
        GlobalEventBus.OnOpenStorageUI += OpenStorageInventory;
        GlobalEventBus.OnOpenRecordCardPopUpUI += OpenRecordCardPopUp;
        GlobalEventBus.OnOpenSettingUI += OpenSettingPopup;
        GlobalEventBus.OnHideMenuUI += HideGameMenu;

        openSettingAction = InputManager.Instance.GetAction("Lobby", "GameMenu");
        if (openSettingAction != null)
        {
            openSettingAction.Enable();
            openSettingAction.performed += OpenPauseUI;
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
        GlobalEventBus.OnHideMenuUI -= HideGameMenu;

        if (openSettingAction != null)
        {
            openSettingAction.performed -= OpenPauseUI;
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
        // 이전 UI 닫기
        CloseOtherUI();

        // 로비 UI를 활성화하고 BGM 재생
        uiManager.Open<LobbyMainUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(lobbyBGMAudioID);
    }

    /* 준비 UI 전환 */
    public void OpenSortiePrepare()
    {
        // 이전 UI 닫기
        CloseOtherUI();

        // 준비 UI를 활성화하고 BGM 재생
        uiManager.Open<SortiePrepareUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(sortieBGMAudioID);
    }

    /* 기록 UI 전환 */
    public void OpenDiverRecord()
    {
        // 이전 UI 닫기
        CloseOtherUI();

        // 기록 UI를 활성화하고 BGM 재생
        uiManager.Open<DiverRecordUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(diversBGMAudioID);
    }

    /* 창고 UI 전환 */
    public void OpenStorageInventory()
    {
        // 이전 UI 닫기
        CloseOtherUI();

        // 창고 UI를 활성화하고 BGM 재생
        uiManager.Open<StorageInventoryUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(storageBGMAudioID);
    }

    private void CloseOtherUI()
    {
        // 먼저 게임 메뉴 및 세팅 UI를 닫는다
        uiManager.Close<GameMenuUI>();
        uiManager.Close<SettingUI>();

        // 현재 열려있는 캔버스를 닫는다
        uiManager.CloseNowUI();
    }

    /* 기록 카드 팝업 UI 전환 */
    public void OpenRecordCardPopUp(string title, CharacterTID tid, int reqLevel)
    {
        // 현재 열려있는 캔버스를 닫는다
        uiManager.CloseNowUI();

        // 기록 카드 팝업 UI를 활성화하고 BGM 재생
        RecordCardPopUpUI popup = uiManager.Open<RecordCardPopUpUI>();
        GlobalEventBus.OnPlayBGMRequested?.Invoke(recordBGMAudioID);

        // 기록 제목, 캐릭터 ID, 해금 레벨을 팝업에 전달한다
        popup.SetData(title, tid, reqLevel);
    }

    /* ESC 목록UI 열기 */
    public void OpenPauseUI(InputAction.CallbackContext context)
    {
        if(gameMenuUI==null)
        {
            gameMenuUI = uiManager.Open<GameMenuUI>();
            Debug.Log("OpenMenu → OpenPauseUI → New Open");
            return;
        }

        if (gameMenuUI.gameObject.activeInHierarchy == false)
        {
            uiManager.Open<GameMenuUI>();
            Debug.Log("OpenMenu → OpenPauseUI → Open");

            // 메뉴 UI 열기 전 이미 열려 있던 설정 UI를 닫기
            if (settingUI != null && settingUI.gameObject.activeInHierarchy == true)
            {
                OpenSettingPopup();
            }
        }
        else
        {
            if (uiManager.GetTopUI() == gameMenuUI)
            {
                uiManager.Close<GameMenuUI>();
                Debug.Log("OpenMenu → OpenPauseUI → Close");
            }
            else
            //스택된 UI가 1개뿐인 경우에는 해당 UI를 닫지 않도록 방어 처리
            if (uiManager.uiStack.Count > 1)
            {
                uiManager.CloseNowUI();
                Debug.Log("OpenMenu → OpenPauseUI → CloseNow");
            }
            else return; 
        }
    }

    /* 설졍 UI 열기 */
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

    // 설정 메뉴 On/Off에 따라 게임 메뉴 UI를 숨김
    private void HideGameMenu(bool hiding)
    {
        if (gameMenuUI.gameObject.activeSelf == true)
        {
            gameMenuUI.panelGroup.alpha = hiding ? 0f: 1f;
        }
    }
}