using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMainUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button buttonSortie;                       // 출격 버튼
    [SerializeField] private Button buttonDiverRecord;                  // 다이버 기록 버튼
    [SerializeField] private Button buttonStorage;                      // 창고 버튼

    [Header("Diver Info")]
    [SerializeField] private TextMeshProUGUI textDiverName;             // 다이버 이름 텍스트
    [SerializeField] private TextMeshProUGUI textLinkRateLevel;         // 동조율 수치 텍스트 
    [SerializeField] private TextMeshProUGUI textSpeakerName;           // 로비 대사 화자 이름 텍스트
    [SerializeField] private TextMeshProUGUI textDialogue;              // 로비 대사 텍스트

    [Header("Notification")]
    [SerializeField] private GameObject newMemoryLogMark;               

    [Header("Temporary Test Data")]
    [SerializeField] private string testDiverName = "유안";
    [SerializeField] private int testLinkRateLevel = 0;
    [TextArea]
    [SerializeField] private string testLobbyDialogue = "누군지도 모르는 너를 믿고 따라야 한다니...기분이 별로야.";
    [SerializeField] private bool testHasNewMemoryLog = false;

    // 대사 출력 인터페이스
    private IDialogueRepository dialogueRepo;
    // 플레이어 정보 인터페이스
    private ISaveRepository saveRepo;
    // 캐릭터 정보 인터페이스
    private ICharDataRepository charRepo;

    private void Start()
    {
        // 대사 출력 인터페이스 연결
        dialogueRepo = new LocalJsonDialogueRepository();
        // 플레이어 정보 인터페이스 연결
        saveRepo = new LocalSaveRepository();
        // 캐릭터 정보 인터페이스 연결
        charRepo = new SOCharacterRepository();
        // 로비 UI가 켜질 때마다 표시 정보를 갱신
        Refresh();
    }

    private void OnEnable()
    {
        // 버튼이 등록되지 않았을 시 비활성화
        if(buttonSortie==null || buttonDiverRecord==null || buttonStorage==null)
        {
            this.enabled = false;
            Debug.Log("필수 오브젝트가 등록되지 않았습니다");
            return;
        }

        /// 버튼 클릭 이벤트를 등록 ///
        buttonSortie.onClick.AddListener(OpenSortiePrepare);
        buttonDiverRecord.onClick.AddListener(OpenDiverRecord);
        buttonStorage.onClick.AddListener(OpenStorageInventory);
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시 이벤트 해제
        buttonSortie.onClick.RemoveListener(OpenSortiePrepare);
        buttonDiverRecord.onClick.RemoveListener(OpenDiverRecord);
        buttonStorage.onClick.RemoveListener(OpenStorageInventory);
    }

    /* 로비의 정보들을 갱신 */
    public void Refresh()
    {
        // 플레이어 저장 데이터
        PlayerSaveData saveData = saveRepo.LoadSaveData();
        // 저장 데이터로부터 현재 선택 캐릭터 추출
        CharacterData charData = charRepo.GetCharacterData(saveData.SelectCharID);

        // {다이버 이름을 표시한다}
        if (textDiverName != null)
            textDiverName.text = charData.name;
    
        int link = saveRepo.GetLinkRateLevel();
        // {현재 동조율 단계를 표시한다}
        if (textLinkRateLevel != null)
            textLinkRateLevel.text = $"동조율 Lv.{link}";
        
        // {로비 기본 대사 화자 이름을 표시한다}
        if (textSpeakerName != null)
            textSpeakerName.text = charData.name;
        
        string log = dialogueRepo.GetRandomDialogue((int)CharacterTID.Yuan, DialogueType.lobbyEnter);
        // {로비 기본 대사를 표시한다}
        if (textDialogue != null)
            textDialogue.text = log;

        // // {신규 심상 기록 알림 표시 여부를 갱신한다}
        // if (newMemoryLogMark != null)
        //     newMemoryLogMark.SetActive(testHasNewMemoryLog);
    }

    /* 출전 준비 씬 열기 */
    public void OpenSortiePrepare()
    {
        GlobalEventBus.OnOpenPrepareUI?.Invoke();
    }

    /* 다이버 기록 열기 */
    public void OpenDiverRecord()
    {
        GlobalEventBus.OnOpenRecordUI?.Invoke();
    }

    /* 창고 인벤토리 열기 */
    public void OpenStorageInventory()
    {
        GlobalEventBus.OnOpenStorageUI?.Invoke();
    }
}
