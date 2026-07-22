using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordCardPopUpUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI textMemoryLogTitle;    // {기록 제목 텍스트}
    [SerializeField] private TextMeshProUGUI textMemoryLogBody;     // {기록 본문 텍스트}
    [SerializeField] private TextMeshProUGUI textMemoryLogIndex;    // {기록 번호 텍스트}

    [Header("Image")]
    [SerializeField] private Image imageMemoryLog;                  // {기록 이미지}

    [Header("Buttons")]
    [SerializeField] private Button buttonClose;                    // 닫기 버튼
    [SerializeField] private Button buttonNext;                     // 다음 텍스트 보기 버튼
    [SerializeField] private Button buttonLog;                      // 대화 기록 보기 버튼

    [Header("Prefab")]
    [SerializeField] GameObject answerButton;                       // 주인공의 대답 버튼 및 텍스트

    private IRecordRepository recordRepo;                           // 기록 출력 인터페이스
    private ICharDataRepository charRepo;                           // 캐릭터 정보 인터페이스
    private CharacterData charData;                                 // 인터페이스에서 추출한 캐릭터 데이터
    private int nowCharacterID = (int)CharacterTID.Yuan;            // 현재 출력 중인 캐릭터 ID
    private int nowDialogueIndex = 0;                               // 현재 출력 중인 대사 순서 번호
    private int dialogueCount = 0;                                  // 현재 출력 가능한 대사 개수
    private int currentReqLevel = 1;                                // 현재 출력 중인 기록의 레벨(그룹)
    
    private System.Collections.Generic.List<GameObject> activeAnswerButtons = new(); // 현재 생성된 대답 버튼 리스트

    private void OnEnable()
    {
        buttonClose.onClick.AddListener(CloseUI);
        //buttonPrev.onClick.AddListener(ReadPrevStory);
        buttonNext.onClick.AddListener(ReadNextStory);
    }

    private void OnDisable()
    {
        buttonClose.onClick.RemoveListener(CloseUI);
        //buttonPrev.onClick.RemoveListener(ReadPrevStory);
        buttonNext.onClick.RemoveListener(ReadNextStory);
    }

    public void SetData(string title, CharacterTID tid, int reqLevel)
    {
        // 기록 출력 인터페이스 연결
        recordRepo = new LocalJsonRecordRepository();

        // 캐릭터 정보 인터페이스 연결
        charRepo = new SOCharacterRepository();

        if (recordRepo == null)
        {
            Debug.LogError("Record Repository Load Failed");
        }

        // {팝업을 새로 열 때 항상 첫 페이지부터 시작하도록 현재 대사 인덱스를 초기화한다}
        nowDialogueIndex = 0;
        currentReqLevel = reqLevel;

        // {캐릭터 ID를 받아와 대사 개수와 맨 앞(0번) 대사를 받아온다}
        nowCharacterID = (int)tid;
        dialogueCount = recordRepo.GetRecordCount(nowCharacterID, currentReqLevel);

        // {캐릭터 ID를 캐릭터 정보 인터페이스에 대입해 캐릭터 데이터를 받아온다}
        // {캐릭터 데이터에서 캐릭터 이름을 가져와 출력}
        charData = charRepo.GetCharacterData(nowCharacterID);

        // {기록 제목 및 본문 텍스트, 이미지를 출력한다}
        PrintDialogue(charData.charName, 0);
    }

    // 다음 텍스트 보기 버튼 터치 시 ID를 1칸 뒤로
    public void ReadNextStory()
    {
        // 다이버 상호작용 사운드 출력 이벤트를 호출
        GlobalEventBus.OnInteractAudio?.Invoke();

        // {마지막 페이지를 넘지 않도록 현재 인덱스를 제한한다}
        nowDialogueIndex = Mathf.Min(nowDialogueIndex + 1, dialogueCount - 1);

        // {변경한 인덱스 값에 맞추어 제목 및 본문 텍스트 갱신}
        PrintDialogue(charData.charName, nowDialogueIndex);
    }

    // 이전 텍스트 보기 버튼 터치 시 ID를 1칸 앞으로
    public void ReadPrevStory()
    {
        // 다이버 상호작용 사운드 출력 이벤트를 호출
        GlobalEventBus.OnInteractAudio?.Invoke();

        // {첫 페이지보다 앞으로 가지 않도록 현재 인덱스를 제한한다}
        nowDialogueIndex = Mathf.Max(nowDialogueIndex - 1, 0);

        // {변경한 인덱스 값에 맞추어 제목 및 본문 텍스트 갱신}
        PrintDialogue(charData.charName, nowDialogueIndex);
    }

    // 캐릭터 대사 (제목, 본문 텍스트, 이미지 스프라이트)를 출력
    private void PrintDialogue(string title, int index)
    {
        // {기존에 생성된 대답 버튼들을 모두 제거}
        foreach (var btn in activeAnswerButtons)
        {
            if (btn != null) Destroy(btn);
        }
        activeAnswerButtons.Clear();

        // {title에 입력한 제목 텍스트를 Title 구역에 출력}
        if (textMemoryLogTitle != null)
            textMemoryLogTitle.text = title;

        string speaker = recordRepo.GetRecordSpeakerByIndex(nowCharacterID, currentReqLevel, index);
        string body = recordRepo.GetRecordTextByIndex(nowCharacterID, currentReqLevel, index);

        // 화자가 "관제사"인 경우 버튼 출력, 아니면 일반 텍스트 출력
        if (speaker == "관제사")
        {
            if (textMemoryLogBody != null) 
            {
                textMemoryLogBody.gameObject.SetActive(true);
                // 이전 대사들 중 관제사가 아닌 화자의 마지막 대사를 찾아서 유지
                string prevBody = "";
                for (int j = index - 1; j >= 0; j--)
                {
                    if (recordRepo.GetRecordSpeakerByIndex(nowCharacterID, currentReqLevel, j) != "관제사")
                    {
                        prevBody = recordRepo.GetRecordTextByIndex(nowCharacterID, currentReqLevel, j);
                        break;
                    }
                }
                textMemoryLogBody.text = prevBody;
            }
            
            if (buttonNext != null) buttonNext.gameObject.SetActive(false); // 다음 텍스트 보기 버튼 숨김

            if (answerButton != null && textMemoryLogBody != null)
            {
                GameObject btnObj = Instantiate(answerButton, this.gameObject.transform.GetChild(0));
                btnObj.SetActive(true);
                RecordAnswerButton btnScript = btnObj.GetComponent<RecordAnswerButton>();
                if (btnScript != null)
                {
                    btnScript.PrintText(body);
                    btnScript.onClickAction = () => { ReadNextStory(); };
                }
                activeAnswerButtons.Add(btnObj);
            }
        }
        else
        {
            if (textMemoryLogBody != null)
            {
                textMemoryLogBody.gameObject.SetActive(true);
                textMemoryLogBody.text = body;
            }
            if (buttonNext != null) buttonNext.gameObject.SetActive(true);
        }

        // {버튼 On/Off 설정}
        StoryButtonCtrl();

        // {기록 번호를 출력한다}
        PrintIndexNumber();
    }

    // 버튼 On/Off 설정
    public void StoryButtonCtrl()
    {
        // 맨 뒤 텍스트(index == lines.Count - 1)이면 다음 텍스트 보기 버튼 잠금
        buttonNext.interactable = nowDialogueIndex < (dialogueCount - 1);
    }

    // 기록 번호를 출력한다 (현재 번호 / 총 기록 개수)
    private void PrintIndexNumber()
    {
        // Index 번호는 0번부터 시작 / Count 개수는 1개부터 시작하므로 Index에 1을 더해서 1부터 출력해준다
        if (textMemoryLogIndex != null)
            textMemoryLogIndex.text = $"{nowDialogueIndex + 1} / {dialogueCount}";
    }

    /* 심상 기록 보기 UI를 닫고 다이버/기록 UI를 출력 */
    public void CloseUI()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke(true);

        // {팝업을 닫을 때 다음 열람을 위해 페이지 인덱스를 첫 페이지로 되돌린다}
        nowDialogueIndex = 0;

        GlobalEventBus.OnOpenRecordUI?.Invoke();
    }
}
