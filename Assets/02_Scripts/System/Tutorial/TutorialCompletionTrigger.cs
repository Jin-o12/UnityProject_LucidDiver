using UnityEngine;

/// <summary>
/// TutorialScene의 마지막 지점에서 튜토리얼 완료 상태만 저장합니다.
/// 실제 로비 이동은 인게임과 동일하게 결과 화면의 버튼 흐름에서 처리합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class TutorialCompletionTrigger : MonoBehaviour
{
    [SerializeField] private bool triggerOnce = true;

    private bool consumed;

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(4f, 2f, 1.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((triggerOnce && consumed) || !IsPlayer(other))
            return;

        consumed = true;
        MarkTutorialCompleted();
    }

    private void MarkTutorialCompleted()
    {
        PlayerSaveDataSO saveDataSO = PlayerSaveDataSO.Instance;
        PlayerSaveData saveData = saveDataSO != null
            ? saveDataSO.LoadSaveData()
            : new PlayerSaveData();

        saveData.isTutorialCompleted = true;
        saveDataSO?.SaveGameData(saveData);

        // 튜토리얼 완료 여부만 저장하고, 로비 이동은 ResultUI의 로비 버튼에서 처리합니다.
    }

    private static bool IsPlayer(Collider other)
    {
        return other != null &&
               (other.CompareTag("Player") ||
                other.GetComponentInParent<PlayerStatus>() != null ||
                other.GetComponentInParent<LocalInputReader>() != null);
    }
}
