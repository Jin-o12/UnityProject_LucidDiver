using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ResultUI : MonoBehaviour
{
    [Header("변수 리스트")]
    public bool extractionResult;               //세션 종료 성패 결과 (`SUCCESS` / `FAILED`)
    public float playTime;                      //세션 내에서 탈출 성공 혹은 사망 시점까지의 기록 시간
    public int memoryFragmentCount;             //이번 세션에서 플레이어가 안전하게 회수한 기억 파편 수
    public int prevLinkRateLevel = 0;           //동조율 상승 전 다이버와의 동조율 단계
    public int linkRateLevel = 0;               //동조율 상승 후 다이버와의 동조율 단계
    public int linkRateGain = 1;                //세션 탈출 성공 시 가산되는 동조율 단계 증가치
    public bool memoryLogUnlocked = false;      //개인 심상 기록 01의 해금 여부 플래그 (기본 false, 탈출 성공 시 기억 파편 수가 1개 이상이면 true)
    public int manaStoneCount;                  //이번 세션에서 플레이어가 파밍하여 탈출 성공한 기묘한 사탕 개수
    public int potionCount;                     //이번 세션에서 플레이어가 파밍하여 탈출 성공한 변질된 붕대 개수
    public string returnDialogueID;             //세션 종료 판정에 따라 로비 복귀 시 출력해야 할 귀환 대사 ID
    public int enemyKillCount;                  //이번 세션에서 플레이어가 처치한 적 개체의 누적 수.
    public string _lobbySceneName;              //로비로 돌아가기 처리 시 돌아가는 신 이름
    public ItemData potionData;                 //변질된 붕대 아이템 데이터
    public ItemData manaStoneData;              //기묘한 사탕 아이템 데이터
    public ItemData memoryFragmentData;         //기억 파편 아이템 데이터

    [Header("UI 컴포넌트")]
    public Image image_Banner;                  //탈출 신 패널 타이틀 이미지
    public TMP_Text text_playTime;              //플레이 시간 텍스트
    public TMP_Text text_enemyKillCount;        //적 처치 수 텍스트
    public Image image_memoryFragmentIcon;      //기억 파편 아이콘 이미지
    public TMP_Text text_getMemoryFragment;     //기억 파편 획득 텍스트
    public TMP_Text text_linkRate;              //동조율 상승 텍스트
    public TMP_Text text_memoryLogUnlocked;     //심상 기록 해금 텍스트
    public Image image_manaStoneIcon;           //기묘한 사탕 아이콘 이미지
    public TMP_Text text_manaStoneCount;        //기묘한 사탕 획득 텍스트
    public Image image_potionIcon;              //변질된 붕대 아이콘 이미지
    public TMP_Text text_potionCount;           //변질된 붕대 획득 텍스트
    public Image image_manaStoneLost;           //탈출 실패 시 기묘한 사탕 유실 이펙트 이미지
    public Image image_potionLost;              //탈출 실패 시 변질된 붕대 유실 이펙트 이미지
    public TMP_Text text_returnDialogue;        //귀환 대사 텍스트

    [Header("UI 리소스")]
    public Sprite Banner_Success;   //탈출 성공 시 패널 타이틀 스프라이트
    public Sprite Banner_Failed;    //강제 각성(탈출 실패) 시 패널 타이틀 스프라이트

    // Addressable Assets 불러오기
    private List<AsyncOperationHandle<Sprite>> loadHandles = new();    // 메모리 관리를 위해 로드 상태를 저장할 핸들

    private void OnEnable()
    {
        RefreshResult();
    }
    private void OnDestroy()
    {
        foreach (var h in loadHandles)
            if (h.IsValid()) Addressables.Release(h);
        loadHandles.Clear();
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
        // 아이템 아이콘을 호출
        PrintItemIcons();
    }

    private void PrintItemIcons()  // 아이템 아이콘 스프라이트 가져오기 메소드
    {
        LoadSprite(potionData, image_potionIcon);
        LoadSprite(manaStoneData, image_manaStoneIcon);
        LoadSprite(memoryFragmentData, image_memoryFragmentIcon);
    }

    private void LoadSprite(ItemData _data, Image _image)
    {
        if (_data == null || _image == null) return;  // null 체크
        var loadHandle = Addressables.LoadAssetAsync<Sprite>(_data.icon);
        loadHandles.Add(loadHandle);
        loadHandle.Completed += (handle) =>
        {
            // 성공적으로 가져왔는지 확인
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // handle.Result에 실제 Sprite 데이터가 들어있음
                //Debug.Log("스프라이트 로드 성공");
                _image.sprite = handle.Result;
            }
            else
            {
                //Debug.LogError("스프라이트를 불러오는 데 실패했습니다.");
            }
        };
    }

    private void PrintTime(float playTime)  //플레이 시간 텍스트를 mm:ss 단위로 출력하는 메소드
    {
        int playTime_Min = (int)(playTime / 60);  //분 단위 값 추출
        int playTime_Sec = (int)(playTime % 60);  //초 단위 값 추출
        text_playTime.text = $"Play Time: {playTime_Min:00}:{playTime_Sec:00}";  // mm:ss 단위로 출력
    }

    private void MemoryFragmentUse()  //탈출 성공 여부에 따라 기억 파편 사용 처리 텍스트를 출력하는 메소드
    {
        //기억 파편 사용 텍스트 출력 (성공 시 사용 / 실패 시 유실)
        text_getMemoryFragment.text = extractionResult
            ? $"기억 파편 × {memoryFragmentCount}\n자동 사용"
            : $"기억 파편 유실\n(0개 사용)";
        //동조율 레벨 업 텍스트 출력
        text_linkRate.text = memoryLogUnlocked
            ? $"동조율 Lv.{prevLinkRateLevel} → <color=#80ff00>Lv.{linkRateLevel}</color>"
            : $"동조율 변화 없음\n(Lv.{prevLinkRateLevel} 유지)";
        //개인 심상 기록 해금 텍스트 출력
        text_memoryLogUnlocked.text = memoryLogUnlocked
            ? $"개인 심상 기록 01 해금"
            : $"개인 심상 기록 해금 <color=#ff0000>실패</color>";
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