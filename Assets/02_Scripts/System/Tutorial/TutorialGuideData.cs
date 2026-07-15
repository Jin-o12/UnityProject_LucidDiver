using System;

/// <summary>
/// ExcelToJson으로 생성되는 튜토리얼 안내 데이터 한 행을 표현합니다.
/// 열림 조건과 클리어 조건을 모두 문자열 데이터로 받아 기획 데이터에서 흐름을 조정할 수 있게 합니다.
/// </summary>
[Serializable]
public sealed class TutorialGuideData
{
    public int TID;
    public int TutorialStep;
    public int TutorialGuideOrder;
    public string OpenConditionType;
    public string OpenConditionValue;
    public string ClearConditionType;
    public string ClearConditionValue;
    public string TutorialGuideText;
    public float TutorialDuration = 3f;
    public string UIHighlightPosition;
    public string HighlightEffectPosition;
    public bool IsTutorialAutoSkip = true;
    public int NextGuideID = -1;
    public string Title;
    public string ConfirmText;
    public bool PauseGame;

    public string TutorialId => TID.ToString();
    public string Message => TutorialGuideText ?? string.Empty;
    public string ResolvedTitle => string.IsNullOrWhiteSpace(Title) ? "튜토리얼" : Title;
    public string ResolvedConfirmText => string.IsNullOrWhiteSpace(ConfirmText) ? "NEXT" : ConfirmText;
}
