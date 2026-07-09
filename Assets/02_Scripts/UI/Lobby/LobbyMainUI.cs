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
    [SerializeField] private Slider sliderLinkRateLevel;                // 동조율 경험치 슬라이더
    [SerializeField] private TextMeshProUGUI textSpeakerName;           // 로비 대사 화자 이름 텍스트
    [SerializeField] private TextMeshProUGUI textDialogue;              // 로비 대사 텍스트
    [SerializeField] private Image CharacterStandingImage;              // 캐릭터 스텐딩 일러스트

    [Header("Notification")]
    [SerializeField] private GameObject newMemoryLogMark;               // 새로운 메모리 알림 마크

    [Header("Temporary Test Data")]
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
        // 플레이어가 선택 한 캐릭터 데이터
        SaveCharacterData charSaveData = saveData.myCharacters.Find(x => x.TID == saveData.SelectCharID);
        // 저장 데이터로부터 현재 선택 캐릭터 추출
        CharacterData charData = charRepo.GetCharacterData(saveData.SelectCharID);

        // {다이버 이름을 표시한다}
        if (textDiverName != null)
            textDiverName.text = charData.charName;
    
        int link = saveRepo.GetLinkRateLevel();
        // {현재 동조율 단계를 표시한다}
        if (textLinkRateLevel != null)
            textLinkRateLevel.text = $"동조율 Lv.{saveRepo.GetLinkRateLevel()}";

        if (sliderLinkRateLevel != null)
        {
            // 최대 레벨 미만일 경우 비율 계산, 최대 레벨일 경우 슬라이더를 꽉 채움(1.0f)
            if (charSaveData.linkRateLevel < charData.requireLinkRatePerLevel.Length)
            {
                float requireExp = charData.requireLinkRatePerLevel[charSaveData.linkRateLevel];
                // 0으로 나누기 방지
                sliderLinkRateLevel.value = requireExp > 0 ? charSaveData.TotallinkRateValue / requireExp : 1.0f;
            }
            else
            {
                sliderLinkRateLevel.value = 1.0f;
            }
        }
        
        // {로비 기본 대사 화자 이름을 표시한다}
        if (textSpeakerName != null)
            textSpeakerName.text = charData.charName;
        
        string log = dialogueRepo.GetRandomDialogue((int)CharacterTID.Yuan, DialogueType.lobbyEnter);
        // {로비 기본 대사를 표시한다}
        if (textDialogue != null)
            textDialogue.text = log;

        // {신규 심상 기록 알림 표시 여부를 갱신한다}
        if (newMemoryLogMark != null)
            newMemoryLogMark.SetActive(testHasNewMemoryLog);
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
