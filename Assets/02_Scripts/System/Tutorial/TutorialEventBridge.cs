using UnityEngine;

/// <summary>
/// 기존 게임플레이 이벤트를 튜토리얼 데이터 조건 이벤트로 전달하는 보조 브릿지입니다.
/// TutorialManager도 동일 이벤트를 직접 구독하므로, 씬 세팅에 따라 브릿지가 없어도 동작합니다.
/// 다만 기존 씬에 배치된 브릿지가 예전 ID 팝업을 중복 호출하지 않도록 문자열 이벤트만 전달합니다.
/// </summary>
public sealed class TutorialEventBridge : MonoBehaviour
{
    private void OnEnable()
    {
        GlobalEventBus.OnItemBoxOpened += HandleItemBoxOpened;
        GlobalEventBus.OnEnemyDead += HandleEnemyDead;
        GlobalEventBus.OnEscapeRequest += HandleEscapeRequest;
        GlobalEventBus.OnMainActiveSkillCasted += HandleMainActiveSkillCasted;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnItemBoxOpened -= HandleItemBoxOpened;
        GlobalEventBus.OnEnemyDead -= HandleEnemyDead;
        GlobalEventBus.OnEscapeRequest -= HandleEscapeRequest;
        GlobalEventBus.OnMainActiveSkillCasted -= HandleMainActiveSkillCasted;
    }

    private void HandleItemBoxOpened(IInteractable interactable, int playerId)
    {
        if (interactable is ItemBox)
            Notify(TutorialEventNames.ItemBoxOpened);
    }

    private void HandleEnemyDead(int enemyId)
    {
        Notify(TutorialEventNames.EnemyDead);
    }

    private void HandleEscapeRequest(bool success)
    {
        Notify(success ? TutorialEventNames.EscapeSucceeded : TutorialEventNames.EscapeFailed);
    }

    private void HandleMainActiveSkillCasted()
    {
        Notify(TutorialEventNames.MainActiveSkillCasted);
    }

    private static void Notify(string eventName)
    {
        TutorialManager manager = TutorialManager.Instance;
        manager?.NotifyEvent(eventName);
    }
}
