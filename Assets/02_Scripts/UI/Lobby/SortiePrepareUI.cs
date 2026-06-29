using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SortiePrepareUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button buttonBackTop;                  // 상단 왼쪽 뒤로가기 버튼
    [SerializeField] private Button buttonStartSortie;              // 출격 버튼
    [SerializeField] private Button buttonChangeFromStorage;        // {창고에서 변경 버튼}

    [Header("Diver Info")]
    [SerializeField] private TextMeshProUGUI textDiverName;         // 다이버 이름
    [SerializeField] private TextMeshProUGUI textDiverRole;         // 
    [SerializeField] private TextMeshProUGUI textLinkRate;          // 동조율

    [Header("Slot 1")]
    [SerializeField] private Image imageSlotIcon1;
    [SerializeField] private TextMeshProUGUI textSlotName1;
    [SerializeField] private TextMeshProUGUI textSlotCount1;
    public int slotTID1;        //1번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite1;  //1번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount1;      //1번 슬롯 아이템의 개수 데이터를 받아옴

    [Header("Slot 2")]
    [SerializeField] private Image imageSlotIcon2;
    [SerializeField] private TextMeshProUGUI textSlotName2;
    [SerializeField] private TextMeshProUGUI textSlotCount2;
    public int slotTID2;        //2번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite2;  //2번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount2;      //2번 슬롯 아이템의 개수 데이터를 받아옴

    [Header("Slot 3")]
    [SerializeField] private Image imageSlotIcon3;
    [SerializeField] private TextMeshProUGUI textSlotName3;
    [SerializeField] private TextMeshProUGUI textSlotCount3;
    public int slotTID3;        //2번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite3;  //2번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount3;      //2번 슬롯 아이템의 개수 데이터를 받아옴

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "DemoScene";

    [Header("Temporary Test Data")]
    [SerializeField] private int testLinkRateLevel = 0;

    private void Awake()
    {
        // {뒤로가기 버튼 이벤트 등록}
        if (buttonBackTop == null || buttonStartSortie == null || buttonChangeFromStorage == null)
        {
            this.enabled = false;
            Debug.Log("필수 오브젝트가 등록되지 않았습니다");
            return;
        }

        buttonBackTop.onClick.AddListener(OnClickBack);
        buttonStartSortie.onClick.AddListener(OnClickStartSortie);
        buttonChangeFromStorage.onClick.AddListener(OnClickChangeFromStorage);
    }

    private void OnEnable()
    {
        // {퀵슬롯 캐시 재전송 요청}
        GlobalEventBus.OnRequestQuickSlotCache?.Invoke();
        // (퀵슬롯 데이터 업데이트 이벤트 등록)
        GlobalEventBus.QuickSlotLoad += UpdateQuickSlot;

        // 출격 준비 UI 오픈 이벤트 발생
        GlobalEventBus.PrepareUIOpen?.Invoke();

        // {출격 준비 UI가 열릴 때마다 표시 정보 갱신}
        Refresh();
    }

    private void OnDestroy()
    {
        // {오브젝트 파괴 시 뒤로가기 버튼 이벤트 해제}
        if (buttonBackTop == null || buttonStartSortie == null || buttonChangeFromStorage == null)
        {
            this.enabled = false;
            Debug.Log("필수 오브젝트가 등록되지 않았습니다");
            return;
        }

        buttonBackTop.onClick.RemoveListener(OnClickBack);
        buttonStartSortie.onClick.RemoveListener(OnClickStartSortie);
        buttonChangeFromStorage.onClick.RemoveListener(OnClickChangeFromStorage);
    }

    private void UpdateQuickSlot(int index, int tid, Sprite icon, int count)
    {
        if (!gameObject.activeInHierarchy) return;

        if (index == 0)
        {
            slotTID1 = tid;
            slotSprite1 = icon;
            slotCount1 = count;
        }
        else if (index == 1)
        {
            slotTID2 = tid;
            slotSprite2 = icon;
            slotCount2 = count;
        }
        else if (index == 2)
        {
            slotTID3 = tid;
            slotSprite3 = icon;
            slotCount3 = count;
        }

        Refresh();
    }

    private void OnDisable()
    {
        GlobalEventBus.QuickSlotLoad -= UpdateQuickSlot;
    }

    public void Refresh()
    {
        if (textDiverName != null) textDiverName.text = "유안";
        if (textDiverRole != null) textDiverRole.text = "메인 다이버";
        if (textLinkRate != null) textLinkRate.text = $"동조율 Lv.{testLinkRateLevel}";

        SetSlotUI(slotTID1, slotSprite1, slotCount1, imageSlotIcon1, textSlotName1, textSlotCount1, "슬롯 1");
        SetSlotUI(slotTID2, slotSprite2, slotCount2, imageSlotIcon2, textSlotName2, textSlotCount2, "슬롯 2");
        SetSlotUI(slotTID3, slotSprite3, slotCount3, imageSlotIcon3, textSlotName3, textSlotCount3, "슬롯 3");
    }

    private void SetSlotUI(int tid, Sprite icon, int count, Image slotIcon, TextMeshProUGUI slotName, TextMeshProUGUI slotCount, string emptySlotName)
    {
        if (tid == 0 || count <= 0)
        {
            if (slotIcon != null) slotIcon.enabled = false;
            if (slotName != null) slotName.text = emptySlotName;
            if (slotCount != null) slotCount.text = "[비어있음]";
            return;
        }

        if (slotIcon != null)
        {
            slotIcon.sprite = icon;
            slotIcon.enabled = icon != null;
        }

        ItemData itemData = GetItemDataByTID(tid);
        if (slotName != null) slotName.text = itemData != null ? itemData.itemName : $"TID {tid}";
        if (slotCount != null) slotCount.text = $"x{count}";
    }

    private ItemData GetItemDataByTID(int tid)
    {
        if (tid == 0) return null;

        ItemData[] itemDatas = Resources.LoadAll<ItemData>("ScriptableObjects/Item");
        foreach (ItemData itemData in itemDatas)
        {
            if (itemData != null && itemData.TID == tid)
            {
                return itemData;
            }
        }

        return null;
    }

    private void OnClickStartSortie()
    {
        // {현재 Canvas 비활성화}
        gameObject.SetActive(false);

        // {출격 확정 시 GameScene으로 이동}
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnClickBack()
    {
        // {로비 Canvas를 다시 활성화}
        GlobalEventBus.OnOpenLobbyUI?.Invoke();

        // {현재 Canvas 비활성화}
        gameObject.SetActive(false);
    }

    private void OnClickChangeFromStorage()
    {
        // {창고 인벤토리 UI 열기 이벤트를 호출한다}
        GlobalEventBus.OnOpenStorageUI?.Invoke();
    }
}
