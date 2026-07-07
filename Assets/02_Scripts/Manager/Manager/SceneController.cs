/// <summary>
/// 모든 씬 전환을 관리하는 인스턴스 클래스
/// </summary>
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    // 로딩 후 넘어갈 씬 저장
    public SceneField TargetSceneName { get; private set;}

    [Header("게임 씬 목록")]
    public SceneField lobbyScene;
    public SceneField gameScene;
    public SceneField LoadScene;

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
        GlobalEventBus.OnGoToLobbyScene -= GoToLobbyScene;
        GlobalEventBus.OnGoToGameScene -= GoToGameScene;
    }

    public void GoToLobbyScene()
    {
        SceneLoader(lobbyScene, false);
    }

    public void GoToGameScene()
    {
        SceneLoader(gameScene, true);
    }

    // 씬 전환 메서드 (넘어가고 싶은 씬 이름, 로딩 씬 사용 여부(사용이 기본))
    public void SceneLoader(SceneField scene, bool useLoadingScene = true)
    {
        if(scene==null)
        {
            Debug.Log($"목표하는 씬이 존재하지 않습니다");
            return;
        }
        if(LoadScene==null)
        {
            Debug.Log($"로딩 씬이 존재하지 않습니다");
            return;
        }

        // 이동 하고자 하는 씬 사전에 저장
        TargetSceneName = scene;

        // 로딩 씬이 요구될 경우 로딩씬 실행
        if(useLoadingScene)
        {
            SceneManager.LoadScene(LoadScene);
        }
        else
        {
            SceneManager.LoadScene(scene);
        }
    }
}
