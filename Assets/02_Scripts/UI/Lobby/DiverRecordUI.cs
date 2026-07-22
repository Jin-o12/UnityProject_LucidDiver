using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiverRecordUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button buttonBackTop;                          //상단 왼쪽 뒤로가기 버튼
    [SerializeField] private Button buttonBackBottom;                       //하단 버튼 그룹의 뒤로가기 버튼

    [Header("Diver Info")]
    [SerializeField] private TextMeshProUGUI textDiverName;
    [SerializeField] private TextMeshProUGUI textDiverRole;
    [SerializeField] private TextMeshProUGUI textLinkRate;
    [SerializeField] private Slider sliderLinkRate;

    [Header("Record List")]
    [SerializeField] private TextMeshProUGUI textRecordCount;
    [SerializeField] private Transform recordCardContainer;
    [SerializeField] private GameObject recordCardPrefab;

    [Header("Temporary Test Data")]
    [SerializeField] private bool useTestData = true;
    [SerializeField] private int testLinkRateLevel = 0;
    [SerializeField] private bool testMemoryLogUnlocked = false;
    [SerializeField] private bool testHasNewMemoryLog = false;

    private int linkRateLevel;
    private bool memoryLogUnlocked;
    private bool hasNewMemoryLog;

    // 캐릭터 정보 인터페이스
    private ICharDataRepository charRepo;

    // 대사 출력 인터페이스
    private IDialogueRepository dialogue;

    // 기록 출력 인터페이스
    private IRecordRepository recordRepo;

    private const string DiverRole = "메인 다이버";

    private void Awake()
    {
        // {뒤로가기 버튼 이벤트 등록}
        if (buttonBackTop == null || buttonBackBottom == null)
        {
            this.enabled = false;
            Debug.Log("필수 오브젝트가 등록되지 않았습니다");
            return;
        }

        // 캐릭터 정보 인터페이스 연결
        charRepo = new SOCharacterRepository();
        
        // 대사 출력 인터페이스 연결
        dialogue = new LocalJsonDialogueRepository();

        // 기록 출력 인터페이스 연결
        recordRepo = new LocalJsonRecordRepository();
    }

    private void OnEnable()
    {
        /// 버튼 클릭 이벤트를 등록 ///
        buttonBackTop.onClick.AddListener(OnClickBack);
        buttonBackBottom.onClick.AddListener(OnClickBack);

        // {테스트 데이터 사용 시 임시 값 적용}
        if (useTestData)
        {
            linkRateLevel = testLinkRateLevel;
            memoryLogUnlocked = testMemoryLogUnlocked;
            hasNewMemoryLog = testHasNewMemoryLog;
        }

        // ResultManager에서 SetData 값을 이벤트로 받아옴
        GlobalEventBus.RecordDataLoad += SetData;


        // 다이버/기록 UI 오픈 이벤트 발생
        GlobalEventBus.RecordUIOpen?.Invoke();

        // {UI가 열릴 때마다 최신 상태로 갱신}
        Refresh();
    }

    private void OnDisable()
    {
        buttonBackTop.onClick.RemoveListener(OnClickBack);
        buttonBackBottom.onClick.RemoveListener(OnClickBack);

        GlobalEventBus.RecordDataLoad -= SetData;
    }


    public void SetData(int newLinkRateLevel, bool newMemoryLogUnlocked, bool newHasNewMemoryLog)
    {
        // {외부에서 실제 저장 데이터를 전달할 때 사용}
        useTestData = false;

        linkRateLevel = newLinkRateLevel;
        memoryLogUnlocked = newMemoryLogUnlocked;
        hasNewMemoryLog = newHasNewMemoryLog;

        Refresh();
    }

    public void Refresh()
    {
        if (!useTestData)
        {
            // 세이브 데이터의 동조율 레벨을 기준으로 판별
            SaveCharacterData charSaveData = PlayerSaveDataSO.Instance.GetNowCharacterData();
            linkRateLevel = charSaveData.linkRateLevel;
            // 캐릭터가 레벨 1에 도달했을 때 심상기록 01 해금
            memoryLogUnlocked = linkRateLevel >= 1;
        }

        // {다이버 기본 정보 갱신}
        RefreshDiverInfo();

        // {기록 목록 갱신 및 프리팹 생성}
        RefreshRecordList();
    }

    private void RefreshDiverInfo()
    {
        if (PlayerSaveDataSO.Instance == null) return;
        PlayerSaveData saveData = PlayerSaveDataSO.Instance.currentData;
        if (saveData == null)
        {
            saveData = PlayerSaveDataSO.Instance.LoadSaveData();
            if (saveData == null) return;
        }
        // 저장 데이터로부터 현재 선택 캐릭터 기획 데이터 추출
        CharacterData charData = charRepo.GetCharacterData(saveData.SelectCharID);
        if (charData == null) return;
        // 플레이어가 선택 한 캐릭터의 세이브 데이터 추출
        SaveCharacterData charSaveData = PlayerSaveDataSO.Instance.GetNowCharacterData();
        if (charSaveData == null) return;


        // {다이버 이름 표시}
        if (textDiverName != null)
            textDiverName.text = charData.charName;

        // {다이버 역할 표시}
        if (textDiverRole != null)
            textDiverRole.text = DiverRole;

        // {동조율 표시}
        if (textLinkRate != null)
        {
            if (linkRateLevel >= 1)
                textLinkRate.text = $"동조율 Lv.{charSaveData.linkRateLevel} ▲";
            else
                textLinkRate.text = $"동조율 Lv.{charSaveData.linkRateLevel}";
        }

        int currentLevel = PlayerSaveDataSO.Instance.GetLinkRateLevel();
        int maxLevel = charData.requireLinkRatePerLevel.Length - 1;
        if(sliderLinkRate != null)
        {
            // 최대 레벨 미만일 경우 비율 계산, 최대 레벨일 경우 슬라이더를 꽉 채움
            if (currentLevel < maxLevel)
            {
                float requireExp = charData.requireLinkRatePerLevel[charSaveData.linkRateLevel+1];
                
                sliderLinkRate.maxValue = requireExp;
                sliderLinkRate.value = PlayerSaveDataSO.Instance.GetlinkRatePoint();
            }
            else
            {
                sliderLinkRate.value = 1.0f;
            }
        }
    }

    private void RefreshRecordList()
    {
        if (recordRepo == null) return;
        if (PlayerSaveDataSO.Instance == null) return;

        SaveCharacterData charSaveData = PlayerSaveDataSO.Instance.GetNowCharacterData();
        if (charSaveData == null) return;

        int charTID = charSaveData.TID;

        // 기록 그룹 가져오기 (해금 레벨 리스트)
        System.Collections.Generic.List<int> recordLevels = recordRepo.GetRecordGroupLevels(charTID);
        
        int openedCount = 0;
        int totalCount = recordLevels.Count;

        // 컨테이너 초기화
        if (recordCardContainer != null)
        {
            foreach (Transform child in recordCardContainer)
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < totalCount; i++)
        {
            int reqLevel = recordLevels[i];
            bool isUnlocked = linkRateLevel >= reqLevel;
            
            if (isUnlocked)
            {
                openedCount++;
            }

            if (recordCardPrefab != null && recordCardContainer != null)
            {
                GameObject recordObj = Instantiate(recordCardPrefab, recordCardContainer);
                RecordCardItem cardItem = recordObj.GetComponent<RecordCardItem>();
                if (cardItem != null)
                {
                    int index = i + 1;
                    string cardTitle = isUnlocked ? $"심상 기록 {index:D2}" : "???";
                    
                    // 제목 밑에 보여줄 짧은 내용으로는 첫 번째 대사를 일부 보여주거나 기본 문구 사용
                    string cardDesc = isUnlocked ? recordRepo.GetRecordTextByIndex(charTID, reqLevel, 0) : "기억 파편을 회수하고 특정 동조율에 도달하면 해금됩니다.";
                    if (cardDesc.Length > 30) cardDesc = cardDesc.Substring(0, 30) + "..."; // 텍스트가 길 경우 생략
                    
                    cardItem.Setup(isUnlocked, cardTitle, cardDesc, index, hasNewMemoryLog && isUnlocked, () => OnClickRecord(cardTitle, charTID, reqLevel));
                }
            }
        }

        // {해금된 기록 개수 표시}
        if (textRecordCount != null)
        {
            textRecordCount.text = $"{openedCount} / {totalCount}";
        }
    }

    private void OnClickBack()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke(true);

        // {로비 Canvas를 다시 활성화}
        GlobalEventBus.OnOpenLobbyUI?.Invoke();
    }

    private void OnClickRecord(string title, int charTID, int reqLevel)
    {
        // 기록 해금 여부에 따라 버튼 클릭 사운드 출력 이벤트를 호출 (이미 isUnlocked 체크되어 호출되므로 true 전달)
        GlobalEventBus.OnClickAudio?.Invoke(true);

        CharacterTID tid = (CharacterTID)charTID;

        // {기록 카드 팝업 열기 이벤트를 호출한다}
        GlobalEventBus.OnOpenRecordCardPopUpUI?.Invoke(title, tid, reqLevel);

        // {기록을 열람했으므로 NEW 배지를 제거한다}
        hasNewMemoryLog = false;

        // NEW 배지 갱신을 위해 다시 새로고침
        RefreshRecordList();

        // {기록 읽음 이벤트를 호출한다}
        GlobalEventBus.OnRecordRead?.Invoke();
    }
}
