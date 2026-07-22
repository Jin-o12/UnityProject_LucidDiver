using System.Collections.Generic;
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
    [SerializeField] private Button buttonClose;                    // {닫기 버튼}
    [SerializeField] private Button buttonPrev;                     // {이전 텍스트 보기 버튼}
    [SerializeField] private Button buttonNext;                     // {다음 텍스트 보기 버튼}

    private IRecordRepository recordRepo;                           // 기록 출력 인터페이스
    private ICharDataRepository charRepo;                           // 캐릭터 정보 인터페이스
    private CharacterData charData;                                 // 인터페이스에서 추출한 캐릭터 데이터
    private int nowCharacterID = (int)CharacterTID.Yuan;            // 현재 출력 중인 캐릭터 ID
    private int nowDialogueIndex = 0;                               // 현재 출력 중인 대사 순서 번호
    private int nowDialogueTID = 0;                                 // 현재 출력 중인 대사 ID
    private int dialogueCount = 0;                                  // 현재 출력 가능한 대사 개수
    private int currentReqLevel = 1;                                // 현재 출력 중인 기록의 레벨(그룹)

    private void OnEnable()
    {
        buttonClose.onClick.AddListener(CloseUI);
        buttonPrev.onClick.AddListener(ReadPrevStory);
        buttonNext.onClick.AddListener(ReadNextStory);
    }

    private void OnDisable()
    {
        buttonClose.onClick.RemoveListener(CloseUI);
        buttonPrev.onClick.RemoveListener(ReadPrevStory);
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
        PrintDialogue(title, 0);
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
    private void PrintDialogue(string title,int index)
    {
        // {title에 입력한 제목 텍스트를 Title 구역에 출력}
        if (textMemoryLogTitle != null)
            textMemoryLogTitle.text = title;

        // {index 값에 해당하는 캐릭터 대사를 Body 구역에 출력}
        string body = recordRepo.GetRecordTextByIndex(nowCharacterID, currentReqLevel, index);
        if (textMemoryLogBody != null)
            textMemoryLogBody.text = body;

        // // {이미지 출력 구역}
        // // P0 버전에서는 임시로 대사별 ID 값에 따른 색 변경으로 구현함
        // nowDialogueTID = recordRepo.GetRecordDialogIDByIndex(nowCharacterID, currentReqLevel, index);
        // imageMemoryLog.color = (nowDialogueTID % 2) switch
        // {
        //     1 => Color.gray,
        //     _ => Color.white,
        // };

        // {버튼 On/Off 설정}
        StoryButtonCtrl();

        // {기록 번호를 출력한다}
        PrintIndexNumber();
    }

    // 버튼 On/Off 설정
    public void StoryButtonCtrl()
    {
        // {맨 앞 텍스트(index == 0)이면 이전 텍스트 보기 버튼 잠금}
        buttonPrev.interactable = nowDialogueIndex > 0;
        // {맨 뒤 텍스트(index == lines.Count - 1)이면 다음 텍스트 보기 버튼 잠금}
        buttonNext.interactable = nowDialogueIndex < (dialogueCount - 1);
    }

    // 기록 번호를 출력한다 (현재 번호 / 총 기록 개수)
    private void PrintIndexNumber()
    {
        // {Index 번호는 0번부터 시작 / Count 개수는 1개부터 시작하므로 Index에 1을 더해서 1부터 출력해준다}
        if (textMemoryLogIndex != null)
            textMemoryLogIndex.text = $"{nowDialogueIndex + 1} / {dialogueCount}";
    }

    // {심상 기록 보기 UI를 닫고 다이버/기록 UI를 출력 }
    public void CloseUI()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke(true);

        // {팝업을 닫을 때 다음 열람을 위해 페이지 인덱스를 첫 페이지로 되돌린다}
        nowDialogueIndex = 0;

        GlobalEventBus.OnOpenRecordUI?.Invoke();
    }
}
