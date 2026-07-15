using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 바닥에 떨어져 있는 아이템 오브젝트.
/// 플레이어가 상호작용하면 실제로 주울 수 있는 수량만 인벤토리에 반영한다.
/// </summary>
public class DropItem : MonoBehaviour, IInteractable
{
    public ItemData itemData;   // 드랍 아이템이 가지고 있는 실제 아이템 데이터
    public int stackCount;      // 바닥에 남아 있는 수량

    [SerializeField] private SpriteRenderer itemIconRenderer; // 공통 드랍 프리팹에서 아이템별 아이콘을 표시할 렌더러

    private void Awake()
    {
        ResolveIconRenderer();
    }

    private void OnValidate()
    {
        ResolveIconRenderer();
    }

    /// <summary>
    /// 인벤토리에서 버려진 아이템 데이터와 수량을 주입하고, 월드 표시 이미지를 갱신한다.
    /// </summary>
    public void Initialize(ItemData data, int count)
    {
        itemData = data;
        stackCount = count;

        _ = RefreshIconAsync();
    }

    public bool Interact(int playerID)
    {
        if (itemData == null || stackCount <= 0)
        {
            Debug.LogWarning("DropItem: 유효하지 않은 드랍 아이템 데이터입니다.");
            return false;
        }

        if (!TryFindPicker(playerID, out MonoBehaviour pickupHandler))
        {
            Debug.LogWarning($"DropItem: playerID={playerID} 에 해당하는 픽업 처리 대상을 찾지 못했습니다.");
            return false;
        }

        int previousCount = stackCount;
        int remain = InvokePickupHandler(pickupHandler);
        if (remain >= stackCount)
        {
            Debug.Log("인벤토리가 가득 차서 아이템을 주울 수 없습니다.");
            return false;
        }

        stackCount = remain;
        if (stackCount < previousCount)
            VFXService.Instance?.Play(GameplayVFXIds.ItemPickup, transform.position, transform.rotation);

        // 전부 주웠으면 월드 아이템을 제거한다.
        if (stackCount <= 0)
        {
            Destroy(gameObject);
            return true;
        }

        // 일부만 주운 경우에는 바닥에 남겨 둔다.
        return false;
    }

    /// <summary>
    /// 프리팹 연결 누락을 줄이기 위해 자식 SpriteRenderer를 자동으로 찾는다.
    /// </summary>
    private void ResolveIconRenderer()
    {
        if (itemIconRenderer == null)
        {
            itemIconRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    /// <summary>
    /// JSON 아이템 데이터의 아이콘 주소를 읽어 공통 드랍 프리팹의 이미지를 아이템별로 교체한다.
    /// </summary>
    private async Task RefreshIconAsync()
    {
        ResolveIconRenderer();

        if (itemIconRenderer == null || itemData == null || string.IsNullOrEmpty(itemData.iconAddress))
        {
            return;
        }

        Sprite loadedIcon = await AddressableLoader.LoadAssetAsync<Sprite>(itemData.iconAddress);
        if (loadedIcon == null)
        {
            return;
        }
        else
        {
            //Debug.Log($"Sprite size: {loadedIcon.bounds.size.x} × {loadedIcon.bounds.size.y}");
        }

        // 이미지의 실제 크기에 비례한 비율로 유니티 유닛 단위에 맞게 축소해 출력되는 이미지의 크기를 일정하게 유지
        float iconMultiple = loadedIcon != null ? Mathf.Min(1 / loadedIcon.bounds.size.x, 1 / loadedIcon.bounds.size.y) : 1f;
        itemIconRenderer.transform.localScale = Vector3.one * iconMultiple;
        itemIconRenderer.sprite = loadedIcon;
    }

    /// <summary>
    /// 상호작용한 플레이어 ID와 일치하는 픽업 처리 대상을 찾는다.
    /// </summary>
    private bool TryFindPicker(int playerID, out MonoBehaviour pickupHandler)
    {
        EntityIdentity[] identities = FindObjectsOfType<EntityIdentity>();
        pickupHandler = null;

        for (int i = 0; i < identities.Length; i++)
        {
            if (identities[i] == null || identities[i].entityID != playerID)
                continue;

            pickupHandler = identities[i].GetComponent("InventoryPresenter") as MonoBehaviour;
            return pickupHandler != null;
        }

        return false;
    }

    /// <summary>
    /// PresenterAD를 직접 참조하지 않고 TryPickUpItem 메서드를 호출한다.
    /// </summary>
    private int InvokePickupHandler(MonoBehaviour pickupHandler)
    {
        if (pickupHandler == null)
            return stackCount;

        var pickupMethod = pickupHandler.GetType().GetMethod("TryPickUpItem");
        if (pickupMethod == null)
        {
            Debug.LogWarning("DropItem: TryPickUpItem 메서드를 찾지 못했습니다.");
            return stackCount;
        }

        object result = pickupMethod.Invoke(pickupHandler, new object[] { itemData, stackCount });
        if (result is int remain)
            return remain;

        Debug.LogWarning("DropItem: TryPickUpItem 반환값을 처리하지 못했습니다.");
        return stackCount;
    }
}
