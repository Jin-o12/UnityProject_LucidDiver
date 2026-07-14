using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 본편 시스템의 글로벌 이벤트를 튜토리얼 팝업 요청으로 변환합니다.
/// TutorialScene에만 배치하여 다른 씬과 풀링 프리팹에는 영향을 주지 않습니다.
/// </summary>
public sealed class TutorialEventBridge : MonoBehaviour
{
    private const string ItemTutorialId = "TUTORIAL_ITEM_001";
    private const string SkillTutorialId = "TUTORIAL_SKILL_001";

    private readonly Queue<string> pendingTutorials = new Queue<string>();
    private readonly HashSet<string> requestedTutorials = new HashSet<string>();

    private void OnEnable()
    {
        GlobalEventBus.OnItemBoxOpened += HandleItemBoxOpened;
        GlobalEventBus.OnEnemyHealthChanged += HandleEnemyHealthChanged;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnItemBoxOpened -= HandleItemBoxOpened;
        GlobalEventBus.OnEnemyHealthChanged -= HandleEnemyHealthChanged;
    }

    private void Update()
    {
        if (pendingTutorials.Count == 0)
            return;

        TutorialManager manager = TutorialManager.Instance;
        if (manager == null || manager.IsShowing)
            return;

        string tutorialId = pendingTutorials.Peek();
        if (manager.Show(tutorialId))
            pendingTutorials.Dequeue();
    }

    /// <summary>
    /// 상자를 실제로 열었을 때 아이템 이동 방법을 한 번만 안내합니다.
    /// </summary>
    private void HandleItemBoxOpened(IInteractable interactable, int playerId)
    {
        if (interactable is ItemBox)
            RequestOnce(ItemTutorialId);
    }

    /// <summary>
    /// 첫 공격이 적에게 적중하면 다음 실습인 액티브 스킬 사용법을 안내합니다.
    /// </summary>
    private void HandleEnemyHealthChanged(int enemyId, float currentHealth, float maxHealth)
    {
        if (maxHealth > 0f && currentHealth > 0f && currentHealth < maxHealth)
            RequestOnce(SkillTutorialId);
    }

    private void RequestOnce(string tutorialId)
    {
        if (!requestedTutorials.Add(tutorialId))
            return;

        pendingTutorials.Enqueue(tutorialId);
    }
}
