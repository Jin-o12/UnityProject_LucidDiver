using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUIPresenter : MonoBehaviour
{
    UIManager uiManager;        // UI 메니저 인스턴스

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnOpenLobbyUI += OpenLobby;
        GlobalEventBus.OnOpenPrepareUI += OpenSortiePrepare;
        GlobalEventBus.OnOpenRecordUI += OpenDiverRecord;
        GlobalEventBus.OnOpenStorageUI += OpenStorageInventory;
        GlobalEventBus.OnOpenRecordCardPopUpUI += OpenRecordCardPopUp;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnOpenLobbyUI -= OpenLobby;
        GlobalEventBus.OnOpenPrepareUI -= OpenSortiePrepare;
        GlobalEventBus.OnOpenRecordUI -= OpenDiverRecord;
        GlobalEventBus.OnOpenStorageUI -= OpenStorageInventory;
        GlobalEventBus.OnOpenRecordCardPopUpUI -= OpenRecordCardPopUp;
    }

    private void Start()
    {
        uiManager = UIManager.Instance;
        OpenLobby();
    }

    /* 로비 UI 전환 */
    public void OpenLobby()
    {
        // 현재 열려있는 캔버스를 닫고 준비 UI로 전환
        uiManager.CloseNowUI();
        uiManager.Open<LobbyMainUI>();
    }

    /* 준비 UI 전환 */
    public void OpenSortiePrepare()
    {
        // 현재 열려있는 캔버스를 닫고 준비 UI로 전환
        uiManager.CloseNowUI();
        uiManager.Open<SortiePrepareUI>();
    }

    /* 기록 UI 전환 */
    public void OpenDiverRecord()
    {
        // 현재 열려있는 캔버스를 닫고 다음 UI로 전환
        uiManager.CloseNowUI();
        uiManager.Open<DiverRecordUI>();
    }

    /* 창고 UI 전환 */
    public void OpenStorageInventory()
    {
        // 현재 열려있는 캔버스를 닫고 다음 UI로 전환
        uiManager.CloseNowUI();
        uiManager.Open<StorageInventoryUI>();
    }

    /* 기록 카드 팝업 UI 전환 */
    public void OpenRecordCardPopUp(string title, string body)
    {
        // 팝업은 뒤의 DiverRecordUI를 닫지 않고 위에만 올린다
        RecordCardPopUpUI popup = uiManager.Open<RecordCardPopUpUI>();

        // 기록 제목과 본문을 팝업에 전달한다
        popup.SetData(title, body);
    }
}