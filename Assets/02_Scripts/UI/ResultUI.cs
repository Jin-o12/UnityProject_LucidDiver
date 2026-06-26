using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ResultUI : MonoBehaviour
{
    [Header("변수 리스트")]
    public bool extractionResult;       //세션 종료 성패 결과 (`SUCCESS` / `FAILED`)
    public float playTime;              //세션 내에서 탈출 성공 혹은 사망 시점까지의 기록 시간
    public int memoryFragmentCount;     //이번 세션에서 플레이어가 안전하게 회수한 기억 파편 수
    public int linkRateLevel = 0;       //다이버와의 동조율 단계
    public int linkRateGain = 1;        //세션 탈출 성공 시 가산되는 동조율 단계 증가치
    public bool memoryLogUnlocked = false;      //개인 심상 기록 01의 해금 여부 플래그
    public int manaStoneCount;          //이번 세션에서 플레이어가 파밍하여 탈출 성공한 마나석 개수
    public int potionCount;             //이번 세션에서 플레이어가 파밍하여 탈출 성공한 회복약 개수
    public string returnDialogueID;     //세션 종료 판정에 따라 로비 복귀 시 출력해야 할 귀환 대사 ID
    public int enemyKillCount;          //이번 세션에서 플레이어가 처치한 적 개체의 누적 수.
    [Header("UI 컴포넌트")]
    public Image image_Banner;                  //탈출 신 패널 타이틀 이미지
    public TMP_Text text_playTime;              //플레이 시간 텍스트
    public TMP_Text text_enemyKillCount;        //적 처치 수 텍스트
    public TMP_Text text_getMemoryFragment;     //기억 조각 획득 텍스트
    public TMP_Text text_linkRate;              //동조율 상승 텍스트
    public TMP_Text text_memoryLogUnlocked;     //심상 기록 해금 텍스트
    public TMP_Text text_manaStoneCount;        //마나석 획득 텍스트
    public TMP_Text text_potionCount;           //회복약 획득 텍스트
    public Image image_manaStoneLost;           //탈출 실패 시 마나석 유실 이펙트 이미지
    public Image image_potionLost;              //탈출 실패 시 회복약 유실 이펙트 이미지
    public TMP_Text text_returnDialogue;        //귀환 대사 텍스트
    [Header("UI 리소스")]
    public Sprite Banner_Success;   //탈출 성공 시 패널 타이틀 스프라이트
    public Sprite Banner_Failed;    //강제 각성(탈출 실패) 시 패널 타이틀 스프라이트
    void OnEnable()  //결과 창 패널이 활성화될 때 출력 데이터가 갱신됨
    {
        //이미지 배너를 세션 성패 결과에 따라 탈출 성공 시 / 강제 각성 시로 나누어 출력
        image_Banner.sprite = extractionResult ? Banner_Success : Banner_Failed;
        PrintTime(playTime);  //플레이 시간 텍스트를 출력하는 메소드
        text_enemyKillCount.text = $"Kills: {enemyKillCount}";  //적 처치 수 카운트를 출력
        MemoryRestore();  //기억 파편을 사용하여 캐릭터 동조율 상승 처리를 실행하는 메소드
        InventoryUpdate();  //인벤토리에 파밍한 마나석과 회복약을 창고에 누적하는 메소드
        text_returnDialogue.text = Dialogue_Return();  //말풍선에 귀환 대사를 출력하는 메소드
    }
    void PrintTime(float playTime)  //플레이 시간 텍스트를 출력하는 메소드
    {
        int playTime_Min = (int)(playTime / 60);  //분 단위 값 추출
        int playTime_Sec = (int)(playTime % 60);  //초 단위 값 추출
        text_playTime.text = $"Play Time: {playTime_Min}:{playTime_Sec}";  //분:초 단위로 출력
    }
    void MemoryRestore()  //기억 파편을 사용하여 캐릭터 동조율 상승 처리를 실행하는 메소드
    {
        int prev_linkRate = linkRateLevel;
        if (extractionResult)  //탈출 성공 (extractionResult == true) 시 처리
        {
            text_getMemoryFragment.text = $"기억 파편 × {memoryFragmentCount}\n자동 사용";  //기억 파편 획득 개수 출력
            linkRateLevel += linkRateGain;  //현재 동조율 단계를 성공 시 동조율 단계만큼 증가시킴
            //동조율 레벨 업 텍스트 출력
            text_linkRate.text = $"동조율 Lv.{prev_linkRate} → <color=#80ff00>Lv.{linkRateLevel}</color>";
            //개인 심상 기록을 해금해준다
            memoryLogUnlocked = true;
            //개인 심상 기록 해금 텍스트 출력
            text_memoryLogUnlocked.text = $"개인 심상 기록 01 해금";
        }
        else  //강제 각성 (extractionResult == false) 시 처리
        {
            text_getMemoryFragment.text = $"기억 파편 유실\n(0개 사용)";  //기억 파편 유실 텍스트 출력
            //동조율 단계 유지 텍스트 출력
            text_linkRate.text = $"동조율 변화 없음\n(Lv.{prev_linkRate} 유지)";
            //개인 심상 기록을 해금하지 않는다
            memoryLogUnlocked = false;
            //개인 심상 기록 해금 실패 텍스트 출력
            text_memoryLogUnlocked.text = $"개인 심상 기록 해금 <color=#ff0000>실패</color>";
        }
    }
    void InventoryUpdate()  //인벤토리에 파밍한 마나석과 회복약을 창고에 누적하는 메소드
    {
        if (extractionResult)  //탈출 성공 (extractionResult == true) 시 처리
        {
            text_manaStoneCount.text = $"마나석 ×{manaStoneCount}\n창고 저장";
            text_potionCount.text = $"회복약 ×{potionCount}\n창고 저장";
        }
        else  //강제 각성 (extractionResult == false) 시 처리
        {
            text_manaStoneCount.text = $"마나석 <color=#ff0000>전체 유실</color>\n(0개 저장)";
            text_potionCount.text = $"회복약 <color=#ff0000>전체 유실</color>\n(0개 저장)";
        }
        // 탈출 실패 시 마나석 및 포션의 유실 이펙트 이미지를 출력
        image_manaStoneLost.enabled = !extractionResult;
        image_potionLost.enabled = !extractionResult;
    }
    string Dialogue_Return()
    {
        return returnDialogueID;
    }
}