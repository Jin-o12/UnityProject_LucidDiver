using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMainUI : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private GameObject canvasLobby;
    [SerializeField] private GameObject canvasSortiePrepare;
    [SerializeField] private GameObject canvasDiverRecord;
    [SerializeField] private GameObject canvasStorageInventory;

    [Header("Buttons")]
    [SerializeField] private Button buttonSortie;
    [SerializeField] private Button buttonDiverRecord;
    [SerializeField] private Button buttonStorage;

    [Header("Diver Info")]
    [SerializeField] private TextMeshProUGUI textDiverName;
    [SerializeField] private TextMeshProUGUI textLinkRateLevel;
    [SerializeField] private TextMeshProUGUI textSpeakerName;
    [SerializeField] private TextMeshProUGUI textDialogue;
    

    [Header("Notification")]
    [SerializeField] private GameObject newMemoryLogMark;

    [Header("Temporary Test Data")]
    [SerializeField] private string testDiverName = "유안";
    [SerializeField] private int testLinkRateLevel = 0;
    [TextArea]
    [SerializeField] private string testLobbyDialogue = "누군지도 모르는 너를 믿고 따라야 한다니...기분이 별로야.";
    [SerializeField] private bool testHasNewMemoryLog = false;

    private void Awake()
    {
        // {출격 버튼 클릭 이벤트를 등록한다}
        if (buttonSortie != null)
            buttonSortie.onClick.AddListener(OpenSortiePrepare);

        // {다이버 기록 버튼 클릭 이벤트를 등록한다}
        if (buttonDiverRecord != null)
            buttonDiverRecord.onClick.AddListener(OpenDiverRecord);

        // {창고 버튼 클릭 이벤트를 등록한다}
        if (buttonStorage != null)
            buttonStorage.onClick.AddListener(OpenStorageInventory);
    }

    private void OnEnable()
    {
        // {로비 UI가 켜질 때마다 표시 정보를 갱신한다}
        Refresh();
    }

    private void OnDestroy()
    {
        // {오브젝트 파괴 시 출격 버튼 이벤트를 해제한다}
        if (buttonSortie != null)
            buttonSortie.onClick.RemoveListener(OpenSortiePrepare);

        // {오브젝트 파괴 시 다이버 기록 버튼 이벤트를 해제한다}
        if (buttonDiverRecord != null)
            buttonDiverRecord.onClick.RemoveListener(OpenDiverRecord);

        // {오브젝트 파괴 시 창고 버튼 이벤트를 해제한다}
        if (buttonStorage != null)
            buttonStorage.onClick.RemoveListener(OpenStorageInventory);
    }

    public void Refresh()
    {
        // {다이버 이름을 표시한다}
        if (textDiverName != null)
            textDiverName.text = testDiverName;

        // {현재 동조율 단계를 표시한다}
        if (textLinkRateLevel != null)
            textLinkRateLevel.text = $"동조율 Lv.{testLinkRateLevel}";

        // {로비 기본 대사를 표시한다}
        if (textSpeakerName != null)
            textSpeakerName.text = testDiverName;
        
        // {로비 기본 대사를 표시한다}
        if (textDialogue != null)
            textDialogue.text = testLobbyDialogue;

        // {신규 심상 기록 알림 표시 여부를 갱신한다}
        if (newMemoryLogMark != null)
            newMemoryLogMark.SetActive(testHasNewMemoryLog);
    }

    public void OpenSortiePrepare()
    {
        // {다른 로비 서브 Canvas를 모두 닫는다}
        CloseSubCanvases();

        // {로비 Canvas를 닫는다}
        SetCanvas(canvasLobby, false);

        // {출격 준비 Canvas를 연다}
        SetCanvas(canvasSortiePrepare, true);
    }

    public void OpenDiverRecord()
    {
        // {다른 로비 서브 Canvas를 모두 닫는다}
        CloseSubCanvases();

        // {로비 Canvas를 닫는다}
        SetCanvas(canvasLobby, false);

        // {다이버 기록 Canvas를 연다}
        SetCanvas(canvasDiverRecord, true);
    }

    public void OpenStorageInventory()
    {
        // {다른 로비 서브 Canvas를 모두 닫는다}
        CloseSubCanvases();

        // {로비 Canvas를 닫는다}
        SetCanvas(canvasLobby, false);

        // {창고 Canvas를 연다}
        SetCanvas(canvasStorageInventory, true);
    }

    public void BackToLobby()
    {
        // {다른 로비 서브 Canvas를 모두 닫는다}
        CloseSubCanvases();

        // {로비 Canvas를 다시 연다}
        SetCanvas(canvasLobby, true);

        // {로비로 돌아올 때 표시 정보를 갱신한다}
        Refresh();
    }

    private void CloseSubCanvases()
    {
        // {출격 준비 Canvas를 닫는다}
        SetCanvas(canvasSortiePrepare, false);

        // {다이버 기록 Canvas를 닫는다}
        SetCanvas(canvasDiverRecord, false);

        // {창고 Canvas를 닫는다}
        SetCanvas(canvasStorageInventory, false);
    }

    private void SetCanvas(GameObject targetCanvas, bool isActive)
    {
        // {대상 Canvas가 연결되어 있을 때만 활성 상태를 변경한다}
        if (targetCanvas != null)
            targetCanvas.SetActive(isActive);
    }
}
