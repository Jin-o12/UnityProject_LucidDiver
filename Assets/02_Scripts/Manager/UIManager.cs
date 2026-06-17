using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // UI 레이어를 관리 할 스텍
    private Stack<GameObject> uiStack = new Stack<GameObject>();

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance!=null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // 바닥에서 아이템이 주어졌을 때 터지는 전역 이벤트를 구독합니다.
        GlobalEventBus.OnUIPushRequested += PushUI;
        GlobalEventBus.OnUIPopRequested += PopUI;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        GlobalEventBus.OnUIPushRequested -= PushUI;
        GlobalEventBus.OnUIPopRequested -= PopUI;
    }

    public void PushUI(GameObject uiObject)
    {
        Debug.Log("UI 활성화");
        uiObject.SetActive(true);
        uiStack.Push(uiObject);
    }

    public void PopUI(GameObject uiObject)
    {
        if (uiStack.Count > 0)
        {
            Debug.Log("UI 비활성화");
            GameObject topUI = uiStack.Pop();
            topUI.SetActive(false);
        }
    }
}
