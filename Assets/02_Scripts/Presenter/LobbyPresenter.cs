/// <summary
/// 로비 씬의 UI, 시스템 등의 기능들을 중재하는 클래스
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPresenter : MonoBehaviour
{

    // UI 캐시
    private LobbyMainUI lobbyUI;
    private SortiePrepareUI PrepareUI;
    private DiverRecordUI recordUI;
    
    private void Awake()
    {
        
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnOpenLobbyUI += OpenLobbyUI;
        GlobalEventBus.OnOpenPrepareUI += OpenPrepareUI;
        GlobalEventBus.OnOpenRecordUI += OpenRecordUI;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnOpenLobbyUI -= OpenLobbyUI;
        GlobalEventBus.OnOpenPrepareUI -= OpenPrepareUI;
        GlobalEventBus.OnOpenRecordUI -= OpenRecordUI;
    }

    private void Start()
    {
        OpenLobbyUI();
    }

#region UI Open/Close
    public void OpenLobbyUI()
    {
        UIManager.Instance.UiStackCLear();
        lobbyUI = UIManager.Instance.Open<LobbyMainUI>();
        UpdateLobbyCharInfo();
    }

    public void OpenPrepareUI()
    {
        UIManager.Instance.UiStackCLear();
        PrepareUI = UIManager.Instance.Open<SortiePrepareUI>();
    }

    public void OpenRecordUI()
    {
        UIManager.Instance.UiStackCLear();
        recordUI = UIManager.Instance.Open<DiverRecordUI>();
    }
#endregion

    /* 로비 씬의 캐릭터 정보 표시 */
    private void UpdateLobbyCharInfo()
    {
        DataManager data = DataManager.Instance;
        
        // DataManager에서 캐릭터 데이터, 동조율 진행도, 로비씬에서의 랜덤 대사 가져옴
        CharacterData charData = data.GetCharacterData(data.playerData.SelectCharID);
        PlayerSaveData playerSaveData = data.playerData;
        string dialouge = data.GetRandomDialogue(charData.TID, DialogueType.lobbyEnter);
        
        // 로비씬에 출력
        lobbyUI.RefreshDiverInfo(charData, playerSaveData, dialouge);
    }
}
