using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillPlayUI : MonoBehaviour
{

    [Header("쿨타임 UI")]
    [SerializeField] private Image sprintCooltimeImg;       // 달리기 쿨타임 출력 이미지
    [SerializeField] private Image skillCooltimeImg;        // 액티브 스킬 쿨타임 출력 이미지
    [SerializeField] private Image evadeCooltimeImg;        // 구르기 쿨타임 출력 이미지
    [SerializeField] private TMP_Text sprintCooltimeTxt;    // 달리기 쿨타임 출력 이미지
    [SerializeField] private TMP_Text skillCooltimeTxt;     // 액티브 스킬 쿨타임 출력 이미지
    [SerializeField] private TMP_Text evadeCooltimeTxt;     // 구르기 쿨타임 출력 이미지
    Color defaultColor = new(1, 1, 1, 1);                   // 기본 색상 설정
    Color colorClear = new(0, 0, 0, 0);                     // 투명화 색상 설정

    private void Awake()
    {
        // 쿨타임 UI를 비활성화한 상태로 시작합니다.
        sprintCooltimeImg.color = colorClear;
        skillCooltimeImg.color = colorClear;
        evadeCooltimeImg.color = colorClear;
        sprintCooltimeTxt.enabled = false;
        skillCooltimeTxt.enabled = false;
        evadeCooltimeTxt.enabled = false;

    }

    private void OnEnable()
    {
        // 쿨타임 시작 이벤트를 받아 UI를 갱신합니다.
        GlobalEventBus.OnPrintSprintCooltime += PrintSprintCooltime;
        GlobalEventBus.OnPrintSkillCooltime += PrintSkillCooltime;
        GlobalEventBus.OnPrintEvadeCooltime += PrintEvadeCooltime;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnPrintSprintCooltime -= PrintSprintCooltime;
        GlobalEventBus.OnPrintSkillCooltime -= PrintSkillCooltime;
        GlobalEventBus.OnPrintEvadeCooltime -= PrintEvadeCooltime;
    }


    // 달리기 불가 회복 시간 출력
    public void PrintSprintCooltime(float Cooltime)
    {
        StartCoroutine(ShowCooltime(Cooltime, Time.time, sprintCooltimeImg, sprintCooltimeTxt));
    }

    // 스킬 쿨타임 출력
    public void PrintSkillCooltime(float Cooltime)
    {
        StartCoroutine(ShowCooltime(Cooltime, Time.time, skillCooltimeImg, skillCooltimeTxt));
    }

    // 구르기 쿨타임 출력
    public void PrintEvadeCooltime(float Cooltime)
    {
        StartCoroutine(ShowCooltime(Cooltime, Time.time, evadeCooltimeImg, evadeCooltimeTxt));
    }

    private IEnumerator ShowCooltime(float _cooltime, float _lastTime, Image img, TMP_Text text)
    {
        // 이미지 및 텍스트를 초기화 상태로 활성화
        img.color = defaultColor;
        img.fillAmount = 1f;
        text.enabled = true;

        // 시작 시점과 끝 시점을 내부 임시 변수로 저장
        float startTime = _lastTime;
        float endTime = _lastTime + _cooltime;
        while (Time.time < endTime)
        {
            // 끝 시점까지 남은 시간을 텍스트 및 이미지 비율로 출력
            float remain = endTime - Time.time;
            img.fillAmount = remain / _cooltime;
            text.text = $"{(int)remain}";
            yield return null;
        }

        // 쿨타임만큼 경과하면 이미지 및 텍스트를 비활성화
        img.fillAmount = 0f;
        img.color = colorClear;
        text.enabled = false;
    }
}
