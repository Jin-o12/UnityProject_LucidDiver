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
    [SerializeField] private RawImage CharacterStandingImage;           // 캐릭터 스텐딩 일러스트 (Live2D 비디오 재생용)


    [Header("Notification")]
    [SerializeField] private GameObject newMemoryLogMark;               // 새로운 메모리 알림 마크

    [Header("Temporary Test Data")]
    [SerializeField] private bool testHasNewMemoryLog = false;

    [Header("Currencies (Top)")]
    [SerializeField] private TextMeshProUGUI textGemAmount;             // 젬 수량
    [SerializeField] private TextMeshProUGUI textShardAmount;           // 파편 수량
    [SerializeField] private TextMeshProUGUI textGoldAmount;            // 골드 수량
    [SerializeField] private Button buttonMail;                         // 우편함 버튼

    [Header("Operator Panel (Left)")]
    [SerializeField] private Image imageRingGauge;                      // 원형 링 게이지 (Filled)
    [SerializeField] private TextMeshProUGUI textRingValue;             // 링 백분율 값 (42%)
    [SerializeField] private TextMeshProUGUI textOperatorLevel;         // 관제사 레벨
    [SerializeField] private Slider sliderOperatorExp;                  // 관제사 경험치 슬라이더
    [SerializeField] private TextMeshProUGUI textOperatorStats;         // 상세 스탯 요약용 멀티라인
    [SerializeField] private TextMeshProUGUI textSystemConsoleLogs;     // 콘솔 로그용 텍스트

    [Header("Quick Alarm Panel (Top-Right)")]
    [SerializeField] private GameObject panelQuickAlarm;                // 우상단 알림 패널

    // 대사 출력 인터페이스
    private IDialogueRepository dialogueRepo;
    // 캐릭터 정보 인터페이스
    private ICharDataRepository charRepo;

    private void Awake()
    {
        // 대사 출력 인터페이스 연결
        dialogueRepo = new LocalJsonDialogueRepository();
        // 캐릭터 정보 인터페이스 연결
        charRepo = new SOCharacterRepository();
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

        // UI 활성화 시에도 정보 업데이트
        Refresh();
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
        // 플레이어가 선택 한 캐릭터의 세이브 데이터 추출
        SaveCharacterData charSaveData = PlayerSaveDataSO.Instance.GetNowCharacterData();
        // 플레이어 저장 데이터 SO
        PlayerSaveData saveData = PlayerSaveDataSO.Instance.currentData;
        // 저장 데이터로부터 현재 선택 캐릭터 기획 데이터 추출
        CharacterData charData = charRepo.GetCharacterData(saveData.SelectCharID);

        // {다이버 이름을 표시한다}
        if (textDiverName != null)
            textDiverName.text = charData.charName;
    
        // {현재 동조율 단계를 표시한다}
        if (textLinkRateLevel != null)
            textLinkRateLevel.text = $"동조율 Lv.{charSaveData.linkRateLevel}";

        int currentLevel = PlayerSaveDataSO.Instance.GetLinkRateLevel();
        int maxLevel = charData.requireLinkRatePerLevel.Length - 1;

        if (sliderLinkRateLevel != null)
        {
            // 최대 레벨 미만일 경우 비율 계산, 최대 레벨일 경우 슬라이더를 꽉 채움
            if (currentLevel < maxLevel)
            {
                float requireExp = charData.requireLinkRatePerLevel[charSaveData.linkRateLevel+1];
                
                sliderLinkRateLevel.maxValue = requireExp;
                sliderLinkRateLevel.value = PlayerSaveDataSO.Instance.GetlinkRatePoint();
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

        // {재화 가상 텍스트 표시}
        if (textGemAmount != null) textGemAmount.text = "12,450";
        if (textShardAmount != null) textShardAmount.text = "3,680";
        if (textGoldAmount != null) textGoldAmount.text = "1,256,890";

        // {좌측 관제사 패널 가상 정보 표시}
        if (imageRingGauge != null)
        {
            imageRingGauge.type = Image.Type.Filled;
            imageRingGauge.fillMethod = Image.FillMethod.Radial360;
            imageRingGauge.fillOrigin = (int)Image.Origin360.Top;
            imageRingGauge.fillAmount = 0.42f;
        }
        if (textRingValue != null) textRingValue.text = "42%";
        if (textOperatorLevel != null) textOperatorLevel.text = "관제사 Lv.27";
        if (sliderOperatorExp != null)
        {
            sliderOperatorExp.maxValue = 100f;
            sliderOperatorExp.value = 42f;
        }
        if (textOperatorStats != null)
        {
            textOperatorStats.text = "탐사 구역 : CHAP.03\n탐사 진행도 : 42%\n통신 링크 : SECURE\n연결 상태 : 유안 (Main)";
        }
        if (textSystemConsoleLogs != null)
        {
            textSystemConsoleLogs.text = "[14:15:32] LINK ESTABLISHED\n[14:15:35] DEVIATION: 0.02%\n[14:16:01] SYNC SUCCESSFUL\n[14:16:12] SIGNAL STRENGTH: 99%\n<color=#10b981>[14:16:30] SYSTEM NORMAL</color>";
        }

        // {알림창 가상 활성화 처리 (새 기록이 있을 때만)}
        if (panelQuickAlarm != null)
        {
            panelQuickAlarm.SetActive(testHasNewMemoryLog);
        }
    }

    /* 출전 준비 씬 열기 */
    public void OpenSortiePrepare()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke();

        GlobalEventBus.OnOpenPrepareUI?.Invoke();
    }

    /* 다이버 기록 열기 */
    public void OpenDiverRecord()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke();

        GlobalEventBus.OnOpenRecordUI?.Invoke();
    }

    /* 창고 인벤토리 열기 */
    public void OpenStorageInventory()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke();

        GlobalEventBus.OnOpenStorageUI?.Invoke();
    }
}
