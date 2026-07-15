using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TutorialScene의 마지막 지점에서 튜토리얼 완료 상태를 저장하고 로비로 이동합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class TutorialCompletionTrigger : MonoBehaviour
{
    private const string LobbySceneName = "LobbyScene";

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
        CompleteTutorialAndGoToLobby();
    }

    private void CompleteTutorialAndGoToLobby()
    {
        PlayerSaveDataSO saveDataSO = PlayerSaveDataSO.Instance;
        PlayerSaveData saveData = saveDataSO != null
            ? saveDataSO.LoadSaveData()
            : new PlayerSaveData();

        saveData.isTutorialCompleted = true;
        saveDataSO?.SaveGameData(saveData);

        if (GlobalEventBus.OnGoToLobbyScene != null)
        {
            GlobalEventBus.OnGoToLobbyScene.Invoke();
            return;
        }

        // TutorialScene만 단독 테스트해도 완료 후 흐름을 확인할 수 있도록 직접 로드합니다.
        SceneManager.LoadScene(LobbySceneName);
    }

    private static bool IsPlayer(Collider other)
    {
        return other != null &&
               (other.CompareTag("Player") ||
                other.GetComponentInParent<PlayerStatus>() != null ||
                other.GetComponentInParent<LocalInputReader>() != null);
    }
}
