/// <summary>
/// 게임 내 모든 UI들을 관리하는 인스턴스 클래스
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 프리팹 등록")]
    [SerializeField] private List<GameObject> uiPrefabs = new();            // UI 프리팹 목록
    
    private readonly Dictionary<Type, MonoBehaviour> uiInstances = new();     // 생성된 UI 캐시
    private readonly Stack<MonoBehaviour> uiStack = new();                     // UI 레이어를 관리 할 스텍

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance!=null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* 원하는 타입의 UI를 실행: Open<열려는 UI타입>() */
    public UiType Open<UiType>() where UiType: MonoBehaviour
    {
        // 캐싱되있거나 새로 만들어진 ui를 받아옵니다
        UiType ui = GetOrCreate<UiType>();
        if (ui == null) return null;

        // 해당 UI 활성화
        ui.gameObject.SetActive(true);
        // 하이라키의 가장 하단에 두어 UI가 가장 위에 그려지도록 합니다
        ui.transform.SetAsLastSibling();
        // 스택에 없는 UI일 경우에만 UI를 스택에 추가합니다 (중복 push 방지)
        if (!uiStack.Contains(ui)) uiStack.Push(ui);
        // 생성되어있는 UI 캐시를 반환합니다
        return ui;
    }

    /* 원하는 UI를 닫음: Close<닫으려는 UI타입>() */
    public void Close<UiType>() where UiType: MonoBehaviour
    {
        // 해당 타입의 UI가 존재하지 않는다면 아래의 과정을 생략함
        if(!uiInstances.TryGetValue(typeof(UiType), out var ui) || ui == null) return;
        
        // UI 비활성화, 스택에서 해당 UI 삭제
        ui.gameObject.SetActive(false);
        if(uiStack.Contains(ui)) RemoveFromStack(ui);
     }

    /* 가장 최근에 열린 UI를 닫음 */
    public void CLoseNowUI()
    {
        // 비어있다면 실행하지 않음
        if(uiStack.Count==0) return;
        uiStack.Pop();
    }

    /* UI가 열릴 때, 이미 캐싱된 UI를 찾고 그렇지 않다면 새로 생성함 */
    private UiType GetOrCreate<UiType>() where UiType: MonoBehaviour
    {
        // UI 캐싱 목록에서 T 타입에 맞는 UI를 찾고 해당 UI가 존재할 시 해당 UI 리턴
        if(uiInstances.TryGetValue(typeof(UiType), out var cached) && cached!=null)
            return (UiType)cached;

        // 캐싱 되어 있지 않은, 즉 처음 생성되는 UI라면 첫 1회 생성
        foreach(var prefab in uiPrefabs)
        {
            // 찾는 타입에 맞는 (해당 컴포넌트가 붙어있는) 프리팹이 존재한다면
            if(prefab != null && prefab.TryGetComponent<UiType>(out _))
            {
                UiType ui = Instantiate(prefab, transform).GetComponent<UiType>();
                uiInstances[typeof(UiType)] = ui;
                return ui;
            }
        }
        Debug.LogError($"UiManager: UI 프리팹이 등록되지 않았습니다. ({typeof(UiType).Name})");
        return null;
    }

    /* 스택 내의 특정 UI를 제거 */
    private void RemoveFromStack(MonoBehaviour ui)
    {
        // Stack의 데이터들을 임시로 가지고 있을 List를 만들어 데이터를 옮기고
        var temp = new List<MonoBehaviour>(uiStack);
        // 지우고자 하는 UI만을 제거한 뒤
        temp.Remove(ui);
        // Stack에 다시 UI 정보를 쌓습니다
        uiStack.Clear();
        for(int i=0 ; i < temp.Count ; i++)
            uiStack.Push(temp[i]);        
    }

    /* UI 스택 클리어 */
    public void UiStackCLear()
    {
        uiStack.Clear();
    }
}
