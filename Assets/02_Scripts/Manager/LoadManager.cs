using System.Collections;
using System.Collections.Generic;
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
            
        // 목적지 씬을 비동기로 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        // 로딩 진척도에 따라 UI 업데이트
        while(!asyncLoad.isDone)
        {
            yield return null;

            loadUI.SetProgress(asyncLoad.progress);
            if(asyncLoad.progress >= 0.9f)
            {
                loadUI.SetProgress(1f);
                asyncLoad.allowSceneActivation = true;
                yield break;
            }
        }
    }
}
