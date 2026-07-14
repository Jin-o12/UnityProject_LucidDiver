/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// </summary>
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 인스턴스
    public static GameManager Instance { get; private set; }
    
    // 인벤토리 기록에 관한 필드
    private PlayerSaveData _playerSaveData;             //인벤토리 기록이 저장된 플레이어 세이브 데이터
    // 플레이 타임 기록에 관한 필드
    private bool timeTrack = false;                     //플레이 시간 측정 중
    private float startTime;                            //플레이 시작 시점
    private readonly string[] playScenes = { "DemoScene", "DemoScene Patrol", "DemoScene Additive" }; //인게임 세션으로 취급할 씬 목록

    // 저장 데이터 인터페이스
    private ICharDataRepository charRepo;               // 캐릭터 데이터 접근 인터페이스
    private IItemDataRepository itemRepo;               // 아이템 데이터 접근 인터페이스

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance==null)
            Instance = this;

        // 인터페이스 구현부 연결
        charRepo = new SOCharacterRepository();
        itemRepo = new LocalJsonItemRepository();

        SceneManager.sceneLoaded += OnSceneLoaded;                  //신 로드 완료 시점에 실행하는 메소드 연결
        GlobalEventBus.OnEscapeRequest += ResultTime;               //탈출 판정 이벤트에 경과 시간 기록 메소드 연결
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GlobalEventBus.OnEscapeRequest -= ResultTime;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(IsPlayScene(scene.name)) //인게임 세션 신에서 실행
        {
            startTime = Time.time;  //시작 시점 등록
            timeTrack = true;       //시간 기록 시작

            // 캐릭터 데이터 가져오기
            PlayerSaveData playerData = PlayerSaveDataSO.Instance.currentData;
            _playerSaveData = playerData;
            CharacterData charData = charRepo.GetCharacterData(playerData.SelectCharID);

            // 플레이어 1회 생성
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SpawnPlayer(charData);
            }

            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.RestoreFromSave(playerData);
            }

            PlayerArtifactEquipment artifactEquipment = FindObjectOfType<PlayerArtifactEquipment>();
            if (artifactEquipment != null)
            {
                artifactEquipment.RestoreFromSave(playerData, itemRepo);
                // 장착 중인 아티팩트를 시작 소지량에 합산하여 인벤토리 이동 시 새로 획득한 것으로 처리되는 것을 방지
                SessionDataSO.Instance.AddStartingArtifacts(artifactEquipment.equippedArtifacts);
            }

            // 적 1회 생성
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SpawnEnemy();
                SpawnManager.Instance.SpawnBoxes();
            }

            UIManager.Instance.Open<GamePlayUI>();
        }
        else
        {
            timeTrack = false;     //인게임 세션 신을 벗어나면 시간 기록 중단
        }
    }

    /// <summary>
    /// 현재 로드된 씬이 인게임 세션 대상인지 판정합니다.
    /// 데모 씬과 패트롤 테스트 씬을 모두 같은 플레이 씬으로 취급하기 위한 보조 함수입니다.
    /// </summary>
    private bool IsPlayScene(string sceneName)
    {
        for (int i = 0; i < playScenes.Length; i++)
        {
            if (sceneName == playScenes[i])
            {
                return true;
            }
        }

        return false;
    }

    private void ResultTime(bool _extractionResult)
    {
        // 사망 후 후속 피격 등으로 종료 이벤트가 중복 발생해도 최초 플레이타임만 사용합니다.
        if (!timeTrack)
            return;

        //시간 기록을 중단하고 기록 고정
        timeTrack = false;
        //결과 계산 시작
        ResultManager.Instance.GameResult(_extractionResult, startTime);
    }
}
