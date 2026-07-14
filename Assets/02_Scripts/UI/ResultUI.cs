using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ResultUI : MonoBehaviour
{
    [Header("변수 리스트")]
    public bool extractionResult;               //세션 종료 성패 결과 (`SUCCESS` / `FAILED`)
    public float playTime;                      //세션 내에서 탈출 성공 혹은 사망 시점까지의 기록 시간
    public int memoryFragmentCount;             //이번 세션에서 플레이어가 안전하게 회수한 기억 파편 수
    public int prevLinkRateLevel = 0;           //동조율 상승 전 다이버와의 동조율 단계
    public int linkRateLevel = 0;               //동조율 상승 후 다이버와의 동조율 단계
    public int linkRateGain = 1;                //세션 탈출 성공 시 가산되는 동조율 단계 증가치
    public bool linkRateUp = false;             //동조율 단계가 증가했는지 여부 체크
    public bool memoryLogUnlocked = false;      //개인 심상 기록 01의 해금 여부 플래그 (기본 false, 탈출 성공 시 기억 파편 수가 1개 이상이면 true)
    public string returnDialogueID;             //세션 종료 판정에 따라 로비 복귀 시 출력해야 할 귀환 대사 ID
    public int enemyKillCount;                  //이번 세션에서 플레이어가 처치한 적 개체의 누적 수.
    public ItemData memoryFragmentData;         //기억 파편 아이템 데이터
    public int invenSlotsCount;                //인벤토리 슬롯 개수 (각성 보존 슬롯 인덱스 값 보정)

    [Header("UI 컴포넌트")]
    public Image image_Banner;                      //탈출 신 패널 타이틀 이미지
    public TMP_Text text_playTime;                  //플레이 시간 텍스트
    public TMP_Text text_enemyKillCount;            //적 처치 수 텍스트
    public Image image_memoryFragmentIcon;          //기억 파편 아이콘 이미지
    public TMP_Text text_getMemoryFragment;         //기억 파편 획득 텍스트
    public TMP_Text text_linkRate;                  //동조율 상승 텍스트
    public TMP_Text text_memoryLogUnlocked;         //심상 기록 해금 텍스트
    public Image image_manaStoneLost;               //탈출 실패 시 기묘한 사탕 유실 이펙트 이미지
    public Image image_potionLost;                  //탈출 실패 시 변질된 붕대 유실 이펙트 이미지
    public TMP_Text text_returnDialogue;            //귀환 대사 텍스트
    
    [Header("동적 슬롯 컨테이너 (UI 컴포넌트)")]
    public Transform acquiredItemsContainer;             // 획득 아이템 목록 출력 콘테이너
    public GameObject acquiredItemPrefab;                // 획득 아이템 슬롯 프리팹 (Image 1개, TMP_Text 1개 구조)
    private List<GameObject> acquiredItemSlotsObj = new(); // 생성된 동적 슬롯 추적 리스트

    public Transform safeSlotContainer;             //각성 보존 슬롯 출력 콘테이너
    public List<GameObject> safeSlotsObj = new();   //각성 보존 슬롯 리스트

    [Header("UI 리소스")]
    public Sprite Banner_Success;   //탈출 성공 시 패널 타이틀 스프라이트
    public Sprite Banner_Failed;    //강제 각성(탈출 실패) 시 패널 타이틀 스프라이트
    public GameObject slotPrefab;   //각성 보존 슬롯 칸 프리팹

    // JSON 데이터 저장소 접근용 리포지토리 인스턴스
    private IItemDataRepository itemRepo;

    private void OnEnable()
    {
        itemRepo = new LocalJsonItemRepository();
        RefreshResult();
    }

    public void UpdateResultUI(bool _result)
    {
        extractionResult = _result;
        RefreshResult();
    }

    /* 결과 창 패널의 출력 데이터 갱신 메소드 */
    public void RefreshResult()  
    {
        //이미지 배너를 세션 성패 결과에 따라 탈출 성공 시 / 강제 각성 시로 나누어 출력
        if (image_Banner != null) image_Banner.sprite = extractionResult ? Banner_Success : Banner_Failed;
        PrintTime(playTime);  //플레이 시간 텍스트를 출력하는 메소드
        //적 처치 수 카운트를 출력
        if (text_enemyKillCount != null) text_enemyKillCount.text = $"Kills: {enemyKillCount}";
        MemoryFragmentUse();  //기억 파편을 사용하여 캐릭터 동조율 상승 처리를 실행하는 메소드
        InventoryUpdate();  //인벤토리에 파밍한 마나석과 회복약을 창고에 누적하는 메소드
        //말풍선에 귀환 대사를 출력하는 메소드
        if (text_returnDialogue != null) text_returnDialogue.text = Dialogue_Return();
        
        PrintItemIcons();  // 아이템 아이콘을 호출
    }

    private void PrintItemIcons()  // 아이템 아이콘 스프라이트 가져오기 메소드 (기억 파편 유지)
    {
        _ = LoadSpriteAsync(memoryFragmentData, image_memoryFragmentIcon);
    }

    private async Task LoadSpriteAsync(ItemData _data, Image _image)
    {
        if (_data == null || _image == null) return;  // null 체크
        if (string.IsNullOrEmpty(_data.iconAddress)) return;

        Sprite loadedIcon = await AddressableLoader.LoadAssetAsync<Sprite>(_data.iconAddress);

        if (loadedIcon != null && _image != null)
        {
            _image.sprite = loadedIcon;
        }
    }

    private void PrintTime(float playTime)  //플레이 시간 텍스트를 mm:ss 단위로 출력하는 메소드
    {
        int playTime_Min = (int)(playTime / 60);  //분 단위 값 추출
        int playTime_Sec = (int)(playTime % 60);  //초 단위 값 추출
        text_playTime.text = $"Play Time: {playTime_Min:00}:{playTime_Sec:00}";  // mm:ss 단위로 출력
    }

    private void MemoryFragmentUse()  //탈출 성공 여부에 따라 기억 파편 사용 처리 텍스트를 출력하는 메소드
    {
        //기억 파편 사용 텍스트 출력 (성공 시 사용 / 실패 시 유실)
        text_getMemoryFragment.text = extractionResult
            ? $"기억 파편 × {memoryFragmentCount}\n자동 사용"
            : $"기억 파편 유실\n(0개 사용)";
        //동조율 레벨 업 텍스트 출력 ( "기억 파편 사용 AND 탈출 성공" 시 동조율 단계 상승)
        text_linkRate.text = linkRateUp
            ? $"동조율 Lv.{prevLinkRateLevel} → <color=#80ff00>Lv.{linkRateLevel}</color>"
            : $"동조율 변화 없음\n(Lv.{prevLinkRateLevel} 유지)";
        //개인 심상 기록 해금 텍스트 출력
        text_memoryLogUnlocked.text = memoryLogUnlocked
            ? $"개인 심상 기록 01 해금"
            : $"개인 심상 기록 해금 <color=#ff0000>실패</color>";
    }

    private void InventoryUpdate()  //인벤토리에 파밍한 아이템들을 창고에 누적하는 동적 슬롯 생성 메소드
    {
        // 기존 생성된 슬롯 삭제
        foreach (var obj in acquiredItemSlotsObj)
        {
            if (obj != null) Destroy(obj);
        }
        acquiredItemSlotsObj.Clear();

        if (acquiredItemsContainer == null || acquiredItemPrefab == null)
        {
            Debug.LogWarning("ResultUI: acquiredItemsContainer 또는 acquiredItemPrefab이 할당되지 않았습니다.");
            return;
        }

        // 획득 아이템이 아예 없는 경우 예외 처리
        if (SessionDataSO.Instance.AcquiredItems == null || SessionDataSO.Instance.AcquiredItems.Count == 0)
        {
            GameObject emptySlot = Instantiate(acquiredItemPrefab, acquiredItemsContainer);
            acquiredItemSlotsObj.Add(emptySlot);

            TMP_Text emptyText = emptySlot.GetComponentInChildren<TMP_Text>();
            Image emptyImage = emptySlot.GetComponentInChildren<Image>();

            if (emptyText != null)
            {
                emptyText.text = extractionResult ? "획득한 아이템이 없습니다." : "유실할 아이템이 없습니다.";
            }
            if (emptyImage != null)
            {
                emptyImage.enabled = false; // 이미지는 숨김
            }
            return;
        }

        // 획득 아이템 목록 순회 (SessionDataSO에서 직접 가져옴)
        foreach (var acqItem in SessionDataSO.Instance.AcquiredItems)
        {
            int tid = acqItem.Key;
            int amount = acqItem.Value;
            
            // ItemRepository를 통해 ItemData 조회
            ItemData itemData = itemRepo.GetItemDataByID(tid);
            if (itemData == null) continue;

            GameObject newSlot = Instantiate(acquiredItemPrefab, acquiredItemsContainer);
            acquiredItemSlotsObj.Add(newSlot);

            // 프리팹 내 UI 컴포넌트 찾기
            Image itemImage = newSlot.GetComponentInChildren<Image>();
            TMP_Text itemText = newSlot.GetComponentInChildren<TMP_Text>();

            // 이미지 로드
            if (itemImage != null && itemData != null)
            {
                _ = LoadSpriteAsync(itemData, itemImage);
            }

            // 텍스트 출력
            if (itemText != null && itemData != null)
            {
                if (extractionResult)  // 탈출 성공 시
                {
                    itemText.text = $"{itemData.itemName} ×{amount}\n";
                }
                else  // 탈출 실패 시
                {
                    itemText.text = $"{itemData.itemName} ×{amount}\n";
                    
                    // 유실 이펙트가 있다면 추가로 켤 수 있지만, 
                    // 프리팹 내부에 image_lost 가 별도로 있다면 다음과 같이 처리할 수 있습니다.
                    // (기본적으로 text_manaStoneCount 구조를 따른다고 했으므로 생략)
                }
            }
        }
    }

    public void CreateSafeSlots(int count)  // 각성 보존 슬롯 생성
    {
        if (safeSlotsObj.Count == count)
            return;

        for (int j = 0; j < safeSlotsObj.Count; j++)
        {
            if (safeSlotsObj[j] != null)
                Destroy(safeSlotsObj[j]);
        }

        safeSlotsObj.Clear();

        for (int j = 0; j < count; j++)
        {
            GameObject newSlot = Instantiate(slotPrefab, safeSlotContainer);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            if (slotUI != null)
                slotUI.Initialize(j + invenSlotsCount);

            safeSlotsObj.Add(newSlot);
        }
    }

    // 각성 보존 슬롯 데이터 업데이트
    public void UpdateSafeSlot(int slotNum, InventorySlotData slotData)
    {
        InventorySlotUI slotUI = safeSlotsObj[slotNum].GetComponent<InventorySlotUI>();
        slotUI.UpdateSlot(slotData.amount, slotData.icon, SlotType.safe);
    }

    private string Dialogue_Return()  //결과 창 말풍선 대사를 DB에서 추출해 출력하는 메소드
    {
        return returnDialogueID;  //DB 구조 확인 전 임시로 ID 값을 출력
    }

    public void OnReturnLobbyClick()  //로비로 돌아가기 버튼 터치 동작
    {
        // 로비로 돌아가기 이벤트를 호출
        GlobalEventBus.OnReturnToLobby?.Invoke();
        // LobbyScene으로 이동하기
        GlobalEventBus.OnGoToLobbyScene?.Invoke();
    }
}