using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadManager : MonoBehaviour
{
    // 인스턴스
    public static LoadManager Instance { get; private set; }
    
    // 컴포넌트
    private LoadUI loadUI;                                      // 로딩 화면 UI


    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance==null)
            Instance = this;
    }

    void Start()
    {
        loadUI = FindFirstObjectByType<LoadUI>();

        if(loadUI == null)
        {
            enabled = false;
            Debug.LogError("필수 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        StartCoroutine(LoadSceneProcess());
    }

    private IEnumerator LoadSceneProcess()
    {
        // 최종 목적지 씬 이름 가져오기
        SceneField targetScene = SceneController.Instance.TargetSceneName;
        // 덮어쓸 씬 이름 가져오기
        SceneField addScene = SceneController.Instance.TargetSceneName_additive;

        // 목적지 씬을 비동기로 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        // 덮어쓸 씬이 있다면 비동기로 로드
        AsyncOperation asyncAdd = null;
        if (SceneController.Instance.TargetSceneName_additive != null)
        {
            asyncAdd = SceneManager.LoadSceneAsync(addScene, LoadSceneMode.Additive);
        }

        // 로딩 진척도에 따라 UI 업데이트
        while (!asyncLoad.isDone || (asyncAdd != null && !asyncAdd.isDone))
        {
            float Progress = (asyncAdd != null)
                ? Mathf.Min (asyncLoad.progress , asyncAdd.progress)
                : asyncLoad.progress;
            loadUI.SetProgress(Progress);
            yield return null;
        }

        loadUI.SetProgress(1f);

        // targetScene을 Active Scene으로 지정
        Scene TargetScene = SceneManager.GetSceneByName(targetScene);
        SceneManager.SetActiveScene(TargetScene);

        // LoadScene 언로드
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}