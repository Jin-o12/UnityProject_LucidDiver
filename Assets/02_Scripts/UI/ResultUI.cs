using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ResultUI : MonoBehaviour
{
    [Header("변수 리스트")]
    public bool extractionResult;               //세션 종료 성패 결과 (`SUCCESS` / `FAILED`)
    public float playTime;                      //세션 내에서 탈출 성공 혹은 사망 시점까지의 기록 시간
    public int memoryFragmentCount;             //이번 세션에서 플레이어가 안전하게 회수한 기억 파편 수
    public int linkRateLevel = 0;               //다이버와의 동조율 단계
    public int linkRateGain = 1;                //세션 탈출 성공 시 가산되는 동조율 단계 증가치
    public bool memoryLogUnlocked = false;      //개인 심상 기록 01의 해금 여부 플래그
    public int manaStoneCount;                  //이번 세션에서 플레이어가 파밍하여 탈출 성공한 마나석 개수
    public int potionCount;                     //이번 세션에서 플레이어가 파밍하여 탈출 성공한 회복약 개수
    public string returnDialogueID;             //세션 종료 판정에 따라 로비 복귀 시 출력해야 할 귀환 대사 ID
    public int enemyKillCount;                  //이번 세션에서 플레이어가 처치한 적 개체의 누적 수.
    public string _lobbySceneName;              //로비로 돌아가기 처리 시 돌아가는 신 이름

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

    private void OnEnable()
    {
        RefreshResult();
    }

    public void UpdateResultUI(bool _result)
    {
        extractionResult = _result;
        /// 데이터 베이스에서 찾아 대입할 것이기 때문에 주석처리 
        //potionCount = FindItemCount(301);
        //manaStoneCount = FindItemCount(302);
        //memoryFragmentCount = FindItemCount(401);
        RefreshResult();
    }

    /* 결과 창 패널의 출력 데이터 갱신 메소드 */
    public void RefreshResult()  
    {
        //이미지 배너를 세션 성패 결과에 따라 탈출 성공 시 / 강제 각성 시로 나누어 출력
        if (image_Banner != null) image_Banner.sprite = extractionResult ? Banner_Success : Banner_Failed;
        PrintTime(playTime);  //플레이 시간 텍스트를 출력하는 메소드
        //적 처치 수 카운트를 출력
        if (text_enemyKillCount != null) text_enemyKillCount.text = $"Kills: {enemyKillCount}";
        MemoryFragmentUse();  //기억 파편을 사용하여 캐릭터 동조율 상승 처리를 실행하는 메소드
        InventoryUpdate();  //인벤토리에 파밍한 마나석과 회복약을 창고에 누적하는 메소드
        //말풍선에 귀환 대사를 출력하는 메소드
        if (text_returnDialogue != null) text_returnDialogue.text = Dialogue_Return();
    }
    private void PrintTime(float playTime)  //플레이 시간 텍스트를 mm:ss 단위로 출력하는 메소드
    {
        int playTime_Min = (int)(playTime / 60);  //분 단위 값 추출
        int playTime_Sec = (int)(playTime % 60);  //초 단위 값 추출
        text_playTime.text = $"Play Time: {playTime_Min:00}:{playTime_Sec:00}";  // mm:ss 단위로 출력
    }

    private void MemoryFragmentUse()  //탈출 성공 여부에 따라 기억 파편을 사용하는 메소드
    {
        if (extractionResult)  //탈출 성공 (extractionResult == true) 시 처리
        {
            text_getMemoryFragment.text = $"기억 파편 × {memoryFragmentCount}\n자동 사용";  //기억 파편 획득 개수 출력
        }
        else  //강제 각성 (extractionResult == false) 시 처리
        {
            memoryFragmentCount = 0;  //기억 파편 개수를 0으로 소실 처리
            text_getMemoryFragment.text = $"기억 파편 유실\n(0개 사용)";  //기억 파편 유실 텍스트 출력
        }
        LinkRateUp(extractionResult && memoryFragmentCount >= 1);  // 탈출 성공 시 기억 파편을 1개 이상 획득했으면 동조율 상승 처리 (그 외의 경우 상승하지 않음)
    }

    private void LinkRateUp(bool up)  //캐릭터 동조율 상승 처리를 실행하는 메소드
    {
        int prev_linkRate = linkRateLevel;  //이전 플레이 레벨 보관
        if (up)
        {
            //[P0] 현재 동조율 단계를 성공 시 동조율 단계만큼 상승시킴
            linkRateLevel += linkRateGain;
            //[P0] 동조율 단계 상승 후 기억 파편 소비
            memoryFragmentCount = 0;
            //동조율 레벨 업 텍스트 출력
            text_linkRate.text = $"동조율 Lv.{prev_linkRate} → <color=#80ff00>Lv.{linkRateLevel}</color>";
            //개인 심상 기록을 해금해준다
            memoryLogUnlocked = true;
            //개인 심상 기록 해금 텍스트 출력
            text_memoryLogUnlocked.text = $"개인 심상 기록 01 해금";
        }
        else
        {
            //동조율 단계 유지 텍스트 출력
            text_linkRate.text = $"동조율 변화 없음\n(Lv.{prev_linkRate} 유지)";
            //개인 심상 기록을 해금하지 않는다
            memoryLogUnlocked = false;
            //개인 심상 기록 해금 실패 텍스트 출력
            text_memoryLogUnlocked.text = $"개인 심상 기록 해금 <color=#ff0000>실패</color>";
        }
    }

    private void InventoryUpdate()  //인벤토리에 파밍한 마나석과 회복약을 창고에 누적하는 메소드
    {
        if (extractionResult)  //탈출 성공 (extractionResult == true) 시 처리
        {
            // 기묘한 사탕, 변질된 붕대 창고 저장 텍스트 출력
            text_manaStoneCount.text = $"기묘한 사탕 ×{manaStoneCount}\n창고 저장";
            text_potionCount.text = $"변질된 붕대 ×{potionCount}\n창고 저장";
        }
        else  //강제 각성 (extractionResult == false) 시 처리
        {
            // 기묘한 사탕, 변질된 붕대 개수를 0으로 소실 처리
            manaStoneCount = 0;
            potionCount = 0;
            // 기묘한 사탕, 변질된 붕대 유실 텍스트 출력
            text_manaStoneCount.text = $"기묘한 사탕 <color=#ff0000>전체 유실</color>\n(0개 저장)";
            text_potionCount.text = $"변질된 붕대 <color=#ff0000>전체 유실</color>\n(0개 저장)";
        }
        // 탈출 실패 시 인벤토리 아이템 유실 이펙트 이미지를 출력 (탈출 성공 시에는 출력하지 않음)
        image_manaStoneLost.enabled = !extractionResult;
        image_potionLost.enabled = !extractionResult;
    }

    private string Dialogue_Return()  //결과 창 말풍선 대사를 DB에서 추출해 출력하는 메소드
    {
        return returnDialogueID;  //DB 구조 확인 전 임시로 ID 값을 출력
    }

    public void OnReturnLobbyClick()  //로비로 돌아가기 버튼 터치 동작
    {
        // LobbyScene으로 이동하기
        SceneManager.LoadScene(_lobbySceneName);
    }
}