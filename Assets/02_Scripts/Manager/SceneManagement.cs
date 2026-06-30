/// <summary>
/// 모든 씬 전환을 관리하는 인스턴스 클래스
/// </summary>
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    public static SceneManagement Instance { get; private set; }

    [Header("게임 씬 목록")]
    public SceneField lobbyScene;
    public SceneField gameScene;

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance!=null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GlobalEventBus.OnGoToLobbyScene += GoToLobbyScene;
        GlobalEventBus.OnGoToGameScene += GoToGameScene;
    }

    private void OnDisable()
    {
        
    }

    public void GoToLobbyScene()
    {
        if(lobbyScene==null)
        {
            Debug.Log("Lobby Scene이 존재하지 않습니다");
            return;
        }
        SceneManager.LoadScene(lobbyScene);
    }

    public void GoToGameScene()
    {
        if(gameScene==null)
        {
            Debug.Log("Game Scene이 존재하지 않습니다");
            return;
        }
        SceneManager.LoadScene(gameScene);
    }
}
