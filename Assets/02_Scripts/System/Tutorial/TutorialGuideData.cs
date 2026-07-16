using System;

/// <summary>
/// ExcelToJson으로 생성되는 튜토리얼 안내 데이터 한 행을 표현합니다.
/// 기존 조작 가이드 팝업과 신규 무전 대화형 튜토리얼이 같은 JSON을 읽을 수 있도록
/// 공통 필드와 무전 연출용 확장 필드를 함께 보관합니다.
/// </summary>
[Serializable]
public sealed class TutorialGuideData
{
    public int TID;
    public int TutorialStep;
    public int TutorialGuideOrder;

    // Dialogue, Guide, Objective처럼 튜토리얼 UI가 어떤 표현 방식을 사용할지 구분합니다.
    public string ContentType;

    // 무전 대화 UI에서 사용할 화자/초상화/음성 연출 식별자입니다.
    public string Speaker;
    public string SpeakerName;
    public string PortraitId;
    public string RadioEffectId;
    public string VoiceId;

    public string OpenConditionType;
    public string OpenConditionValue;
    public string ClearConditionType;
    public string ClearConditionValue;

    // 신규 데이터는 DialogueText 또는 GuideText를 우선 사용하고, 기존 팝업 호환을 위해 TutorialGuideText도 유지합니다.
    public string DialogueText;
    public string GuideText;
    public string TutorialGuideText;

    public float TutorialDuration = 3f;
    public string UIHighlightPosition;
    public string HighlightEffectPosition;

    // 여러 입력 아이콘을 표시해야 할 때 WASD|Shift 같은 문자열로 전달합니다.
    public string InputIconIds;

    // 무전 송신/응답 버튼 등 별도 인터랙션이 필요한 대사인지 표시합니다.
    public bool RequiresTransmit;

    public bool IsTutorialAutoSkip = true;
    public int NextGuideID = -1;
    public string Title;
    public string ConfirmText;
    public bool PauseGame;

    public string TutorialId => TID.ToString();

    public string Message
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(TutorialGuideText))
                return TutorialGuideText;

            if (!string.IsNullOrWhiteSpace(GuideText))
                return GuideText;

            return DialogueText ?? string.Empty;
        }
    }

    public string ResolvedTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Title))
                return Title;

            if (!string.IsNullOrWhiteSpace(SpeakerName))
                return SpeakerName;

            if (!string.IsNullOrWhiteSpace(Speaker))
                return Speaker;

            return "튜토리얼";
        }
    }

    public string ResolvedConfirmText => string.IsNullOrWhiteSpace(ConfirmText) ? "NEXT" : ConfirmText;
}
