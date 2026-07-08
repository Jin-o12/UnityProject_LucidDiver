/// <summary>
/// 모든 씬 전환을 관리하는 인스턴스 클래스
/// </summary>
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    // 로딩 후 넘어갈 씬 저장
    public SceneField TargetSceneName { get; private set; }             //기본 로딩 씬
    public SceneField TargetSceneName_additive { get; private set; }    //additive 로딩으로 덮어쓸 씬

    [Header("게임 씬 목록")]
    public SceneField lobbyScene;   //로비 허브 씬
    public SceneField levelScene;   //레벨 디자인 씬
    public SceneField gameScene;    //게임플레이 씬
    public SceneField LoadScene;    //로딩 씬

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
        SceneLoader(levelScene, true, gameScene);
    }

    // 씬 전환 메서드 (넘어가고 싶은 씬 이름, 로딩 씬 사용 여부(사용이 기본), 로드할 추가 씬(기본값 null))
    public void SceneLoader(SceneField scene, bool useLoadingScene = true, SceneField addScene = null)
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

        // additive로 덮어쓸 씬 저장
        if (addScene != null) TargetSceneName_additive = addScene;

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
