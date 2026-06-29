/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// [26.06.24_강다영] playerPrefab, EnemyPrefab: 캐릭터 및 적 프리팹은 생성 시 결정되도록 바꿀 것
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    

    // 인벤토리 기록에 관한 필드
    private PlayerSaveData _playerSaveData;             //인벤토리 기록이 저장된 플레이어 세이브 데이터
    private ItemData potionData;                        //변질된 붕대 아이템 데이터
    private ItemData manaStoneData;                     //기묘한 사탕 아이템 데이터
    private ItemData memoryFragmentData;                //기억 파편 아이템 데이터
    // 플레이 타임 기록에 관한 필드
    private float playTime;                             //이번 세션 플레이 시간
    private bool timeTrack = false;                     //플레이 시간 측정 중
    private float startTime;                            //플레이 시작 시점
    private readonly string playScene = "DemoScene";    //플레이 시간을 측정할 신
    // 탈출에 관한 필드
    private bool extractionResult;                      //탈출 성공 여부 판정
    //private ResultUI resultPanel;                       //결과 창 UI 캐시
    // 동조율 관련 코드
    public int linkRateLevel = 0;                       //동조율 상승 후 다이버와의 동조율 단계
    public int linkRateGain = 1;                        //세션 탈출 성공 시 가산되는 동조

    // 저장 데이터 인터페이스
    private ISaveRepository saveRepo;                                // 플레이어 데이터 저장 인터페이스
    public PlayerSaveData playerData { get; private set; }           // 실질적인 플레이어 데이터
    private ICharDataRepository charRepo;
    public CharacterData charData { get; private set; }

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance==null)
            Instance = this;

        saveRepo = new LocalSaveRepository();
        charRepo = new SOCharacterRepository();

        SceneManager.sceneLoaded += OnSceneLoaded;                  //신 로드 완료 시점에 실행하는 메소드 연결
        //GlobalEventBus.OnEscapeRequest += QuitGame;              //탈출 판정 이벤트에 탈출 처리 메소드 연결
        //GlobalEventBus.OnReturnToLobby += CloseResultPanel;      //로비로 돌아가기 버튼에 결과 창 닫기 연결
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        //GlobalEventBus.OnEscapeRequest -= QuitGame;
        //GlobalEventBus.OnReturnToLobby -= CloseResultPanel;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == playScene) //인게임 세션 신에서 실행
        {
            startTime = Time.time;  //시작 시점 등록
            timeTrack = true;       //시간 기록 시작

            // 캐릭터 데이터 가져오기
            playerData = saveRepo.LoadSaveData();
            _playerSaveData = playerData;
            charData = charRepo.GetCharacterData(playerData.SelectCharID);

            // 플레이어 1회 생성
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SpawnPlayer(charData);
            }

            // 적 1회 생성
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SpawnEnemy();
            }
        }
        else
        {
            timeTrack = false;     //인게임 세션 신을 벗어나면 시간 기록 중단
        }
    }

    // private void ResultTime(bool _extractionResult)
    // {
    //     foreach (SaveSlotData slot in _playerSaveData.inventorySlots)
    //     {
    //         // 해당 아이템이 이미 창고에 존재한다면 보유 개수를 창고에 더함
    //         if (slot.TID == _tid)
    //         {
    //             slot.amount = 0;
    //         }
    //     }
    // }

    public void FindItem(int _tid, out int count, out ItemData data)
    {
        foreach (SaveSlotData slot in _playerSaveData.inventorySlots)
        {
            if (slot.TID == _tid)
            {
                count = slot.amount;
                data = DataManager.Instance.GetItemData(_tid);
                return;
            }
        }
        count = 0;
        data = null;
        return;
    }

    private void InventorySync()
    {
        // 플레이어 오브젝트에서 PlayerInventory 컴포넌트를 찾아 데이터 동기화
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("PlayerInventory를 찾을 수 없습니다.");
            return;
        }

        // PlayerSaveData의 인벤토리 슬롯을 비우고 현재 플레이어 인벤토리 데이터로 채우기
        _playerSaveData.inventorySlots.Clear();
        foreach (var slot in playerInventory.slots)
        {
            // TID가 0이 아닌 슬롯만 저장 (빈 슬롯 제외)
            if (slot.TID != 0)
            {
                _playerSaveData.inventorySlots.Add(new SaveSlotData
                {
                    index = slot.order,
                    TID = slot.TID,
                    amount = slot.amount
                });
            }
        }
        // //시간 기록을 중단하고 기록 고정
        // timeTrack = false;
        // //결과 계산 시작
        // ResultManager.Instance.GameResult(_extractionResult, startTime);
    }
}