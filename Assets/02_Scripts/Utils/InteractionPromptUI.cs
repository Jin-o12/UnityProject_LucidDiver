using UnityEngine;

/// <summary>
/// 상호작용 프롬프트 UI를 켜고 끄는 전용 스크립트
/// ItemBox 같은 상호작용 오브젝트의 루트에 붙이고
/// 실제로 보여줄 UI 오브젝트를 promptRoot로 연결해서 사용함
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;   // 월드 스페이스 UI 루트 오브젝트

    private void Awake()
    {
        // 시작 시에는 프롬프트를 꺼둠
        if (promptRoot == null)
        {
            Debug.LogWarning("InteractionPromptUI: promptRoot가 연결되지 않았음.", this);
            return;
        }

        promptRoot.SetActive(false);
    }

    /// <summary>
    /// 플레이어가 상호작용 범위 안에 들어왔을 때 프롬프트 표시
    /// </summary>
    public void Show()
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 플레이어가 상호작용 범위 밖으로 나갔을 때 프롬프트 숨김
    /// </summary>
    public void Hide()
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }
}