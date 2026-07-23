using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 ID와 화면에 표시할 문구 및 일시정지 설정을 보관합니다.
/// 씬과 분리된 ScriptableObject이므로 TutorialSystem 프리팹을 다른 씬으로 옮겨도 재사용할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "TutorialMessageCatalog", menuName = "Lucid Diver/Tutorial/Message Catalog")]
public sealed class TutorialMessageCatalog : ScriptableObject
{
    [SerializeField] private List<TutorialMessageEntry> entries = new List<TutorialMessageEntry>();

    private Dictionary<string, TutorialMessageEntry> entryById;

    public IReadOnlyList<TutorialMessageEntry> Entries => entries;

    public bool TryGetEntry(string tutorialId, out TutorialMessageEntry entry)
    {
        BuildLookupIfNeeded();
        return entryById.TryGetValue(tutorialId ?? string.Empty, out entry);
    }

    private void OnEnable()
    {
        entryById = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        entryById = null;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (TutorialMessageEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.TutorialId))
                continue;

            if (!ids.Add(entry.TutorialId))
                Debug.LogWarning($"[TutorialCatalog] 중복된 튜토리얼 ID가 있습니다: {entry.TutorialId}", this);
        }
    }
#endif

    private void BuildLookupIfNeeded()
    {
        if (entryById != null)
            return;

        entryById = new Dictionary<string, TutorialMessageEntry>(StringComparer.Ordinal);
        foreach (TutorialMessageEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.TutorialId) || entryById.ContainsKey(entry.TutorialId))
                continue;

            entryById.Add(entry.TutorialId, entry);
        }
    }
}

/// <summary>
/// 튜토리얼 팝업 한 단계에 필요한 표시 데이터입니다.
/// </summary>
[Serializable]
public sealed class TutorialMessageEntry
{
    [SerializeField] private string tutorialId;
    [SerializeField] private string title;
    [TextArea(2, 5)] [SerializeField] private string message;
    [SerializeField] private string confirmText = "확인";
    [SerializeField] private bool pauseGame = true;

    public string TutorialId => tutorialId;
    public string Title => title;
    public string Message => message;
    public string ConfirmText => string.IsNullOrWhiteSpace(confirmText) ? "확인" : confirmText;
    public bool PauseGame => pauseGame;
}
