using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiverRecordUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject canvasLobby;

    [Header("Buttons")]
    [SerializeField] private Button buttonBack;
    [SerializeField] private Button buttonRecordCard01;
    [SerializeField] private Button buttonRecordCard02;
    [SerializeField] private Button buttonCloseMemoryLog;

    [Header("Diver Info")]
    [SerializeField] private TextMeshProUGUI textDiverName;
    [SerializeField] private TextMeshProUGUI textDiverRole;
    [SerializeField] private TextMeshProUGUI textLinkRate;
    [SerializeField] private RectTransform imageLinkRateBarFill;
    //[SerializeField] private float linkRateBarMaxWidth = 160f;

    [Header("Record List")]
    [SerializeField] private TextMeshProUGUI textRecordCount;

    [Header("Record 01")]
    [SerializeField] private CanvasGroup recordCard01CanvasGroup;
    [SerializeField] private TextMeshProUGUI textRecordState01;
    [SerializeField] private TextMeshProUGUI textRecordTitle01;
    [SerializeField] private TextMeshProUGUI textRecordDesc01;
    [SerializeField] private TextMeshProUGUI textOpenRecord01;
    [SerializeField] private TextMeshProUGUI textNewBadge01;

    [Header("Record 02")]
    [SerializeField] private CanvasGroup recordCard02CanvasGroup;
    [SerializeField] private TextMeshProUGUI textRecordState02;
    [SerializeField] private TextMeshProUGUI textRecordTitle02;
    [SerializeField] private TextMeshProUGUI textRecordDesc02;
    [SerializeField] private TextMeshProUGUI textOpenRecord02;
    [SerializeField] private TextMeshProUGUI textNewBadge02;

    [Header("Memory Log Popup")]
    [SerializeField] private GameObject popupMemoryLog01;
    [SerializeField] private TextMeshProUGUI textMemoryLogTitle;
    [SerializeField] private TextMeshProUGUI textMemoryLogBody;

    private int linkRateLevel;
    private bool memoryLogUnlocked;
    private bool hasNewMemoryLog;

    private const string DiverName = "유안";
    private const string DiverRole = "메인 다이버";
    private const string Record01Title = "대외 경계 태도 및 면담 일지";

    private const string Record01Desc =
        "유안의 대외 경계 태도와\n초기 면담 기록이 복구되었다.";

    private const string Record01LockedDesc =
        "기억 파편을 회수하고 탈출 성공 시\n개인 심상 기록 01이 해금된다.";

    private const string Record01Body =
        "[면담 요약서: 다이버 유안 (관리번호: D-004)]\n\n" +
        "피실험자는 면담 시간 내내 극도의 긴장 상태를 유지했으며, " +
        "탁자 위의 깨진 액정 화면에 비치는 나의 모습을 집요하게 시선으로 쫓는 기이한 행동을 보였다.\n\n" +
        "이는 사건 발생 당일 내 눈에만 이상한 보랏빛 선이 보여서 피해야 한다고 주장했으나 " +
        "아무도 믿어주지 않아 눈앞에서 가족을 잃었던 트라우마의 전형적인 방어 행동이다.\n\n" +
        "피실험자는 바깥 세계의 일반적인 시선과 언어 정보를 적대시하고 있으며, " +
        "자신이 직접 관찰하고 물리적으로 확인한 상만을 판단의 근거로 삼는다.\n\n" +
        "특이점은 이러한 편집증적인 시각적 집착이 뇌파 안정기와 맞물렸을 때, " +
        "주변 공간의 미세한 파동 굴절을 누구보다 빠르게 잡아내는 초감각으로 치환된다는 사실이다.";

    private void Awake()
    {
        // {뒤로가기 버튼 이벤트 등록}
        if (buttonBack != null)
            buttonBack.onClick.AddListener(OnClickBack);

        // {기록 01 카드 클릭 이벤트 등록}
        if (buttonRecordCard01 != null)
            buttonRecordCard01.onClick.AddListener(OnClickRecord01);

        // {기록 02 카드 클릭 이벤트 등록}
        if (buttonRecordCard02 != null)
            buttonRecordCard02.onClick.AddListener(OnClickRecord02);

        // {기록 팝업 닫기 버튼 이벤트 등록}
        if (buttonCloseMemoryLog != null)
            buttonCloseMemoryLog.onClick.AddListener(OnClickCloseMemoryLog);

        // {기록 팝업은 시작 시 닫아둠}
        if (popupMemoryLog01 != null)
            popupMemoryLog01.SetActive(false);
    }

    private void OnEnable()
    {
        // ResultManager에서 SetData 값을 이벤트로 받아옴
        GlobalEventBus.RecordDataLoad += SetData;
        // 다이버/기록 UI 오픈 이벤트 발생
        GlobalEventBus.OnOpenRecordUI?.Invoke();
        // {UI가 열릴 때마다 최신 상태로 갱신}
        Refresh();
    }

    private void OnDisable()
    {
        GlobalEventBus.RecordDataLoad -= SetData;
    }

    private void OnDestroy()
    {
        // {뒤로가기 버튼 이벤트 해제}
        if (buttonBack != null)
            buttonBack.onClick.RemoveListener(OnClickBack);

        // {기록 01 카드 클릭 이벤트 해제}
        if (buttonRecordCard01 != null)
            buttonRecordCard01.onClick.RemoveListener(OnClickRecord01);

        // {기록 02 카드 클릭 이벤트 해제}
        if (buttonRecordCard02 != null)
            buttonRecordCard02.onClick.RemoveListener(OnClickRecord02);

        // {기록 팝업 닫기 버튼 이벤트 해제}
        if (buttonCloseMemoryLog != null)
            buttonCloseMemoryLog.onClick.RemoveListener(OnClickCloseMemoryLog);
    }

    public void SetData(int newLinkRateLevel, bool newMemoryLogUnlocked, bool newHasNewMemoryLog)
    {
        linkRateLevel = newLinkRateLevel;
        memoryLogUnlocked = newMemoryLogUnlocked;
        hasNewMemoryLog = newHasNewMemoryLog;

        Refresh();
    }

    public void Refresh()
    {
        // {다이버 기본 정보 갱신}
        RefreshDiverInfo();

        // {기록 목록 갱신}
        RefreshRecordList();

        // {기록 01 카드 갱신}
        RefreshRecord01();

        // {기록 02 카드 갱신}
        RefreshRecord02();

        // {팝업 텍스트 갱신}
        RefreshMemoryLogPopup();
    }

    private void RefreshDiverInfo()
    {
        // {다이버 이름 표시}
        if (textDiverName != null)
            textDiverName.text = DiverName;

        // {다이버 역할 표시}
        if (textDiverRole != null)
            textDiverRole.text = DiverRole;

        // {동조율 표시}
        if (textLinkRate != null)
        {
            if (linkRateLevel >= 1)
                textLinkRate.text = $"동조율 Lv.{linkRateLevel} ▲";
            else
                textLinkRate.text = $"동조율 Lv.{linkRateLevel}";
        }
    }

    private void RefreshRecordList()
    {
        // {해금된 기록 개수 표시}
        if (textRecordCount != null)
        {
            int openedCount = memoryLogUnlocked ? 1 : 0;
            textRecordCount.text = $"{openedCount} / 2";
        }
    }

    private void RefreshRecord01()
    {
        // {기록 01 상태 표시}
        if (textRecordState01 != null)
            textRecordState01.text = memoryLogUnlocked ? "[OPEN] 기록 01" : "[LOCK] 기록 01";

        // {기록 01 제목 표시}
        if (textRecordTitle01 != null)
            textRecordTitle01.text = memoryLogUnlocked ? Record01Title : "???";

        // {기록 01 설명 표시}
        if (textRecordDesc01 != null)
            textRecordDesc01.text = memoryLogUnlocked ? Record01Desc : Record01LockedDesc;

        // {기록 보기 문구 표시}
        if (textOpenRecord01 != null)
            textOpenRecord01.text = memoryLogUnlocked ? "기록 보기" : "잠김";

        // {기록 01 버튼 활성화}
        if (buttonRecordCard01 != null)
            buttonRecordCard01.interactable = memoryLogUnlocked;

        // {기록 01 카드 투명도 처리}
        if (recordCard01CanvasGroup != null)
        {
            recordCard01CanvasGroup.alpha = memoryLogUnlocked ? 1f : 0.55f;
            recordCard01CanvasGroup.interactable = memoryLogUnlocked;
            recordCard01CanvasGroup.blocksRaycasts = memoryLogUnlocked;
        }

        // {NEW 배지 표시}
        if (textNewBadge01 != null)
            textNewBadge01.gameObject.SetActive(memoryLogUnlocked && hasNewMemoryLog);
    }

    private void RefreshRecord02()
    {
        // {기록 02는 P0에서 잠금 더미로 고정}
        if (textRecordState02 != null)
            textRecordState02.text = "[LOCK] 기록 02";

        if (textRecordTitle02 != null)
            textRecordTitle02.text = "???";

        if (textRecordDesc02 != null)
            textRecordDesc02.text = "동조율 Lv.2 달성 시 해금";

        if (textOpenRecord02 != null)
            textOpenRecord02.text = "잠김";

        if (buttonRecordCard02 != null)
            buttonRecordCard02.interactable = false;

        if (recordCard02CanvasGroup != null)
        {
            recordCard02CanvasGroup.alpha = 0.45f;
            recordCard02CanvasGroup.interactable = false;
            recordCard02CanvasGroup.blocksRaycasts = false;
        }

        if (textNewBadge02 != null)
            textNewBadge02.gameObject.SetActive(false);
    }

    private void RefreshMemoryLogPopup()
    {
        // {기록 팝업 제목 표시}
        if (textMemoryLogTitle != null)
            textMemoryLogTitle.text = Record01Title;

        // {기록 팝업 본문 표시}
        if (textMemoryLogBody != null)
            textMemoryLogBody.text = Record01Body;
    }

    private void OnClickBack()
    {
        // {로비 Canvas를 다시 활성화}
        if (canvasLobby != null)
            canvasLobby.SetActive(true);

        // {다이버 기록 Canvas 비활성화}
        gameObject.SetActive(false);
    }

    private void OnClickRecord01()
    {
        // {기록 01이 잠겨 있으면 열지 않음}
        if (!memoryLogUnlocked)
        {
            Debug.Log("DiverRecordUI: 기록 01은 아직 잠겨 있습니다.");
            return;
        }

        // {기록 01 팝업 열기}
        if (popupMemoryLog01 != null)
            popupMemoryLog01.SetActive(true);

        // {기록을 열람했으므로 hasNewMemoryLog를 false로 설정하여 NEW 배지 제거}
        // hasNewMemoryLog = false;
        SetData(linkRateLevel, memoryLogUnlocked, false);

        GlobalEventBus.OnRecordRead?.Invoke();

        if (textNewBadge01 != null)
            textNewBadge01.gameObject.SetActive(false);
    }

    private void OnClickRecord02()
    {
        // {기록 02는 P0에서 잠금 더미}
        Debug.Log("DiverRecordUI: 기록 02는 P0에서 잠금 상태입니다.");
    }

    private void OnClickCloseMemoryLog()
    {
        // {기록 01 팝업 닫기}
        if (popupMemoryLog01 != null)
            popupMemoryLog01.SetActive(false);
    }
}
