using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 UI 프리팹을 생성, 캐싱, 열기, 닫기 처리하는 매니저입니다.
/// 씬 전환 또는 Additive 씬 로드 중 UIManager가 중복 생성될 수 있습니다.
/// 이 경우 중복 매니저의 UI 프리팹 등록값만 기존 매니저에 병합하고 중복 오브젝트는 제거합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    // 파괴된 static 참조가 남았을 때 로드된 씬에서 살아있는 UIManager를 다시 찾아 복구합니다.
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<UIManager>();
#else
                instance = FindObjectOfType<UIManager>();
#endif
            }

            return instance;
        }
        private set => instance = value;
    }

    [Header("UI Prefab Registry")]
    [SerializeField] private List<GameObject> uiPrefabs = new();

    private readonly Dictionary<Type, MonoBehaviour> uiInstances = new();
    private readonly Stack<MonoBehaviour> uiStack = new();

    private void Awake()
    {
        UIManager currentInstance = Instance;
        if(currentInstance != null && currentInstance != this)
        {
            // Additive 씬 로드로 중복 UIManager가 생성되면 프리팹 등록값만 병합하고 중복 오브젝트는 제거합니다.
            currentInstance.MergePrefabsFrom(uiPrefabs);
            // 같은 Manager 오브젝트에 다른 매니저 컴포넌트가 함께 붙어 있으므로 오브젝트 전체가 아니라 UIManager 컴포넌트만 제거합니다.
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GlobalEventBus.OnCloseTopUI += CloseNowUI;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnCloseTopUI -= CloseNowUI;
    }

    private void OnDestroy()
    {
        // 자기 자신이 싱글톤 참조라면 파괴 시점에 참조를 비워 다음 접근 때 다시 찾을 수 있게 합니다.
        if(instance == this)
            instance = null;
    }

    /// <summary>
    /// 지정한 타입의 UI를 열고 화면 최상단에 배치합니다.
    /// </summary>
    public UiType Open<UiType>() where UiType: MonoBehaviour
    {
        if(this == null)
            return null;

        UiType ui = GetOrCreate<UiType>();
        if (ui == null) return null;

        ui.gameObject.SetActive(true);
        ui.transform.SetAsLastSibling();

        if (!uiStack.Contains(ui)) uiStack.Push(ui);

        return ui;
    }

    /// <summary>
    /// 지정한 타입의 UI를 닫습니다.
    /// </summary>
    public void Close<UiType>() where UiType: MonoBehaviour
    {
        if(this == null)
            return;

        if(!uiInstances.TryGetValue(typeof(UiType), out var ui) || ui == null) return;

        ui.gameObject.SetActive(false);
        if(uiStack.Contains(ui)) RemoveFromStack(ui);
    }

    /// <summary>
    /// 가장 최근에 열린 UI를 닫습니다.
    /// </summary>
    public void CloseNowUI()
    {
        if(this == null)
            return;

        if (uiStack.TryPop(out MonoBehaviour topUI))
        {
            if(topUI != null)
                topUI.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("현재 닫을 UI가 없습니다.");
        }
    }

    /// <summary>
    /// 스택의 최상단 UI를 반환합니다.
    /// </summary>
    public MonoBehaviour GetTopUI()
    {
        if (this == null)
            return null;

        if (uiStack.TryPeek(out MonoBehaviour topUI))
            return topUI;
            
        return null;
    }

    /// <summary>
    /// 캐시된 UI를 반환하거나, 등록된 프리팹에서 찾아 새로 생성합니다.
    /// </summary>
    private UiType GetOrCreate<UiType>() where UiType: MonoBehaviour
    {
        if(this == null)
            return null;

        if(uiInstances.TryGetValue(typeof(UiType), out var cached) && cached != null)
            return (UiType)cached;

        foreach(var prefab in uiPrefabs)
        {
            if(prefab != null && prefab.TryGetComponent<UiType>(out _))
            {
                UiType ui = Instantiate(prefab, transform).GetComponent<UiType>();
                uiInstances[typeof(UiType)] = ui;
                return ui;
            }
        }

        Debug.LogError($"UIManager: UI 프리팹이 등록되지 않았습니다. ({typeof(UiType).Name}) / 등록 수: {uiPrefabs.Count}", this);
        return null;
    }

    /// <summary>
    /// 중복 UIManager가 가진 UI 프리팹 등록값을 현재 싱글톤 매니저에 병합합니다.
    /// </summary>
    private void MergePrefabsFrom(IEnumerable<GameObject> prefabs)
    {
        if(prefabs == null) return;

        foreach(GameObject prefab in prefabs)
        {
            if(prefab == null || uiPrefabs.Contains(prefab)) continue;

            uiPrefabs.Add(prefab);
        }
    }

    /// <summary>
    /// UI 스택에서 특정 UI만 제거합니다.
    /// </summary>
    private void RemoveFromStack(MonoBehaviour ui)
    {
        var temp = new List<MonoBehaviour>(uiStack);
        temp.Remove(ui);

        uiStack.Clear();
        for(int i = 0 ; i < temp.Count ; i++)
            uiStack.Push(temp[i]);
    }
}
