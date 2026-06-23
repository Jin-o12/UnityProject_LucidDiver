using UnityEngine;

/// <summary>
/// 플레이어가 상호작용했을 때 랜덤한 아이템 드랍을 생성하는 상자 스크립트
/// 드랍 프리팹을 배열로 받아 동일한 확률로 하나를 선택함
/// </summary>
public class ItemBox : MonoBehaviour, IInteractable
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject[] dropPrefabs;    // 랜덤 드랍 후보 프리팹 목록
    [SerializeField] private Transform dropPoint;         // 아이템이 생성될 위치

    private bool isOpened = false;                        // 한 번 열린 상자는 다시 열리지 않도록 체크

    /// <summary>
    /// 플레이어가 상자와 상호작용하면 호출됨
    /// 3개의 드랍 후보 중 하나를 랜덤 생성하고 상자는 제거됨
    /// </summary>
    public bool Interact(int playerID)
    {
        // 이미 열린 상자는 다시 처리하지 않음
        if (isOpened)
            return false;

        // 드랍 프리팹이 연결되지 않았으면 종료
        if (dropPrefabs == null || dropPrefabs.Length == 0)
        {
            Debug.LogWarning("ItemBox: 드랍 프리팹이 설정되지 않았음.", this);
            return false;
        }

        // 배열 길이 기준으로 랜덤 인덱스를 뽑기 때문에
        // 3개를 넣으면 1:1:1 비율로 랜덤 드랍이 됨
        int randomIndex = Random.Range(0, dropPrefabs.Length);
        GameObject selectedDropPrefab = dropPrefabs[randomIndex];

        if (selectedDropPrefab == null)
        {
            Debug.LogWarning("ItemBox: 비어 있는 드랍 프리팹 슬롯이 있음.", this);
            return false;
        }

        // dropPoint가 있으면 그 위치에 생성하고
        // 없으면 상자 위쪽으로 살짝 띄워서 생성
        Vector3 spawnPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + Vector3.up * 0.75f;

        Quaternion spawnRotation = selectedDropPrefab.transform.rotation;

        Instantiate(selectedDropPrefab, spawnPosition, spawnRotation);

        isOpened = true;
        Debug.Log("상자를 열었고 아이템을 드랍함: " + selectedDropPrefab.name);

        // 상자는 한 번 사용 후 제거
        Destroy(gameObject);
        return true;
    }
}