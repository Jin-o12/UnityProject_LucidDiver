/// <summary>
/// 튜토리얼 Excel/JSON 데이터에서 사용하는 조건 문자열을 모아둔 클래스입니다.
/// enum으로 고정하지 않고 문자열 데이터로 운용하되, 코드 내부 오타를 줄이기 위해 상수로만 관리합니다.
/// </summary>
public static class TutorialConditionTypes
{
    public const string None = "None";
    public const string SceneLoaded = "SceneLoaded";
    public const string TriggerEnter = "TriggerEnter";
    public const string Event = "Event";
    public const string PrevGuideClosed = "PrevGuideClosed";
    public const string NextButton = "NextButton";
    public const string Duration = "Duration";
}

/// <summary>
/// 튜토리얼 Event 조건에서 사용하는 이벤트 이름 문자열입니다.
/// Excel/JSON의 OpenConditionValue 또는 ClearConditionValue에 이 값을 넣어 연결합니다.
/// </summary>
public static class TutorialEventNames
{
    public const string ItemBoxOpened = "ItemBoxOpened";
    public const string EnemyDead = "EnemyDead";
    public const string EscapeSucceeded = "EscapeSucceeded";
    public const string EscapeFailed = "EscapeFailed";
    public const string MainActiveSkillCasted = "MainActiveSkillCasted";
}
