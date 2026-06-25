using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SortiePrepareUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button buttonStartSortie;

    [Header("Diver Info")]
    [SerializeField] private TextMeshProUGUI textDiverName;
    [SerializeField] private TextMeshProUGUI textDiverRole;
    [SerializeField] private TextMeshProUGUI textLinkRate;

    [Header("Slot 1")]
    [SerializeField] private Image imageSlotIcon1;
    [SerializeField] private TextMeshProUGUI textSlotName1;
    [SerializeField] private TextMeshProUGUI textSlotCount1;
    public int slotTID1;        //1번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite1;  //1번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public string slotName1;    //1번 슬롯 아이템의 이름 스트링 데이터를 받아옴
    public int slotCount1;      //1번 슬롯 아이템의 개수 데이터를 받아옴

    [Header("Slot 2")]
    [SerializeField] private Image imageSlotIcon2;
    [SerializeField] private TextMeshProUGUI textSlotName2;
    [SerializeField] private TextMeshProUGUI textSlotCount2;
    public int slotTID2;        //2번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite2;  //2번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public string slotName2;    //2번 슬롯 아이템의 이름 스트링 데이터를 받아옴
    public int slotCount2;      //2번 슬롯 아이템의 개수 데이터를 받아옴

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Temporary Test Data")]
    [SerializeField] private int testLinkRateLevel = 0;
    [SerializeField] private string testEquippedItemSlot1 = "";
    [SerializeField] private string testEquippedItemSlot2 = "";

    private void Awake()
    {
        // {출격 버튼 클릭 이벤트 등록}
        if (buttonStartSortie != null)
            buttonStartSortie.onClick.AddListener(OnClickStartSortie);
        // (퀵슬롯 데이터 업데이트 이벤트 등록)
        //  GlobalEventBus.OnQuickSlotChanged += UpdateSlot;
    }

    private void OnEnable()
    {
        // (퀵슬롯 데이터를 받아오는 이벤트를 등록)
        // {출격 준비 UI가 열릴 때마다 표시 정보 갱신}
        Refresh();
    }

    private void OnDestroy()
    {
        // {오브젝트 파괴 시 출격 버튼 이벤트 해제}
        if (buttonStartSortie != null)
            buttonStartSortie.onClick.RemoveListener(OnClickStartSortie);
        // (퀵슬롯 데이터 업데이트 이벤트 해제)
        //  GlobalEventBus.OnQuickSlotChanged += UpdateSlot;
    }

    public void Refresh()
    {
        // {P0 고정 다이버 정보 표시}
        if (textDiverName != null)
            textDiverName.text = "유안";

        if (textDiverRole != null)
            textDiverRole.text = "메인 다이버";

        if (textLinkRate != null)
            textLinkRate.text = $"동조율 Lv.{testLinkRateLevel}";

        // {소지품 슬롯 1번 표시}
        SetSlotUI(
            testEquippedItemSlot1,
            imageSlotIcon1,
            textSlotName1,
            textSlotCount1,
            "슬롯 1"
        );

        // {소지품 슬롯 2번 표시}
        SetSlotUI(
            testEquippedItemSlot2,
            imageSlotIcon2,
            textSlotName2,
            textSlotCount2,
            "슬롯 2"
        );
    }

    private void SetSlotUI(
        string itemId,
        Image slotIcon,
        TextMeshProUGUI slotName,
        TextMeshProUGUI slotCount,
        string emptySlotName
    )
    {
        // {아이템이 장착되지 않은 슬롯 표시}
        if (string.IsNullOrEmpty(itemId))
        {
            if (slotIcon != null)
                slotIcon.enabled = false;

            if (slotName != null)
                slotName.text = emptySlotName;

            if (slotCount != null)
                slotCount.text = "[비어있음]";

            return;
        }

        // {아이템이 장착된 슬롯 표시}
        if (slotIcon != null)
            slotIcon.enabled = true;

        if (slotName != null)
            slotName.text = GetItemDisplayName(itemId);

        if (slotCount != null)
            slotCount.text = "x1";
    }

    private string GetItemDisplayName(string itemId)
    {
        // {P0 공식 아이템 ID를 화면 표시명으로 변환}
        switch (itemId)
        {
            case "302":
                return "기묘한 사탕";

            case "301":
                return "변질된 붕대";

            default:
                return "알 수 없음";
        }
    }

    private void OnClickStartSortie()
    {
        // {출격 확정 시 GameScene으로 이동}
        SceneManager.LoadScene(gameSceneName);
    }
}
