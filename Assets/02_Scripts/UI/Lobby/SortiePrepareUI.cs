using System.Threading.Tasks;
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
    [SerializeField] private Slider sliderLinkRate;                 // 경험치 슬라이더

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

    // 캐릭터 정보 인터페이스
    private ICharDataRepository charRepo;
    // 아이템 정보 인터페이스
    private IItemDataRepository itemRepo;

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

        // 인터페이스 연결
        charRepo = new SOCharacterRepository();
        itemRepo = new LocalJsonItemRepository();
    }

    private void OnEnable()
    {
        // 출격 준비 UI 오픈 이벤트 발생
        GlobalEventBus.PrepareUIOpen?.Invoke();

        // {출격 준비 UI가 열릴 때마다 표시 정보 갱신}
        _ = Refresh();
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
            SetSlotUI(slotTID1, slotSprite1, slotCount1, imageSlotIcon1, textSlotName1, textSlotCount1, "슬롯 1");
        }
        else if (index == 1)
        {
            slotTID2 = tid;
            slotSprite2 = icon;
            slotCount2 = count;
            SetSlotUI(slotTID2, slotSprite2, slotCount2, imageSlotIcon2, textSlotName2, textSlotCount2, "슬롯 2");
        }
        else if (index == 2)
        {
            slotTID3 = tid;
            slotSprite3 = icon;
            slotCount3 = count;
            SetSlotUI(slotTID3, slotSprite3, slotCount3, imageSlotIcon3, textSlotName3, textSlotCount3, "슬롯 3");
        }
    }

    private void OnDisable()
    {
    }

    public async Task Refresh()
    {
        // 플레이어가 선택 한 캐릭터의 세이브 데이터 추출
        SaveCharacterData charSaveData = PlayerSaveDataSO.Instance.GetNowCharacterData();
        // 플레이어 저장 데이터 SO
        PlayerSaveData saveData = PlayerSaveDataSO.Instance.currentData;
        // 저장 데이터로부터 현재 선택 캐릭터 기획 데이터 추출
        CharacterData charData = charRepo.GetCharacterData(saveData.SelectCharID);

        if (textDiverName != null) textDiverName.text = charData.charName;
        if (textDiverRole != null) textDiverRole.text = "메인 다이버";
        if (textLinkRate != null) textLinkRate.text = $"동조율 Lv.{charSaveData.linkRateLevel}";

        // 세이브 데이터에서 퀵슬롯 정보를 직접 로드하여 갱신
        await LoadQuickSlotsFromSave(saveData);

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
        return itemRepo.GetItemDataByID(tid);
    }

    private void OnClickStartSortie()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke();

        // {현재 Canvas 비활성화}
        gameObject.SetActive(false);

        // {출격 확정 시 GameScene으로 이동}
        GlobalEventBus.OnGoToGameScene?.Invoke();
    }

    private void OnClickBack()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke();

        // {로비 Canvas를 다시 활성화}
        GlobalEventBus.OnOpenLobbyUI?.Invoke();

        // {현재 Canvas 비활성화}
        gameObject.SetActive(false);
    }

    private void OnClickChangeFromStorage()
    {
        // 버튼 클릭 사운드 출력 이벤트를 호출
        GlobalEventBus.OnClickAudio?.Invoke();

        // {창고 인벤토리 UI 열기 이벤트를 호출한다}
        GlobalEventBus.OnOpenStorageUI?.Invoke();
    }

    private async Task LoadQuickSlotsFromSave(PlayerSaveData saveData)
    {
        if (saveData == null || saveData.quickSlots == null) return;

        // 인벤토리에서 해당 TID 아이템의 총 개수를 반환하는 로컬 함수
        int GetItemCount(int tid)
        {
            if (tid == 0) return 0;
            int count = 0;
            if (saveData.inventorySlots != null)
            {
                foreach (var slot in saveData.inventorySlots)
                {
                    if (slot != null && slot.TID == tid) count += slot.amount;
                }
            }
            return count;
        }

        // 최대 3개의 퀵슬롯에 대해 비동기 로드 후 UI 갱신
        for (int i = 0; i < 3; i++)
        {
            int tid = i < saveData.quickSlots.Count ? saveData.quickSlots[i] : 0;
            int count = GetItemCount(tid);
            ItemData data = GetItemDataByTID(tid);
            Sprite icon = data != null ? await AddressableLoader.LoadAssetAsync<Sprite>(data.iconAddress) : null;
            
            UpdateQuickSlot(i, tid, icon, count);
        }
    }
}
