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

        /// 이전 씬 비동기 언로드 (로딩 바 0% ~ 20%) ///
        string previousScene = SceneController.Instance.PreviousSceneName;
        if (!string.IsNullOrEmpty(previousScene))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(previousScene);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                {
                    // 이전 씬 정리 진행도를 0~0.2 구간에 매핑
                    loadUI.SetProgress(unloadOp.progress * 0.2f);
                    yield return null;
                }
            }
        }
        loadUI.SetProgress(0.2f);

        /// 목적지 씬 비동기 로드 (로딩 바 20% ~ 100%) ///
        // 목적지 씬을 비동기로 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        // 덮어쓸 씬이 있다면 비동기로 로드
        AsyncOperation asyncAdd = null;
        if (SceneController.Instance.TargetSceneName_additive != null)
        {
            asyncAdd = SceneManager.LoadSceneAsync(addScene, LoadSceneMode.Additive);
        }

        // 로딩 진척도에 따라 UI 업데이트 (0.2 ~ 1.0 구간)
        while (!asyncLoad.isDone || (asyncAdd != null && !asyncAdd.isDone))
        {
            float sceneProgress = (asyncAdd != null)
                ? Mathf.Min(asyncLoad.progress, asyncAdd.progress)
                : asyncLoad.progress;
            loadUI.SetProgress(0.2f + sceneProgress * 0.8f);
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