using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("엔티티 생성")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject EnemyPrefab;
    [SerializeField] private GameObject playerSpawnPool;
    [SerializeField] private GameObject enemySpawnPool;
    private List<Transform> playerSpawnPoint = new();
    private List<Transform> enemySpawnPoint = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // 플레이어와 적 스폰지점 불러오기
        if (playerSpawnPool != null)
        {
            foreach (Transform point in playerSpawnPool.transform)
            {
                playerSpawnPoint.Add(point);
            }
        }
        if (enemySpawnPool != null)
        {
            foreach (Transform point in enemySpawnPool.transform)
            {
                enemySpawnPoint.Add(point);
            }
        }
    }

    public void SpawnPlayer(CharacterData charData)
    {
        if (playerSpawnPoint.Count == 0)
        {
            Debug.LogError("Player spawn point list is empty");
            return;
        }

        // 플레이어 스폰 포인트 중 무작위로 하나 선정
        int spawnNum = Random.Range(0, playerSpawnPoint.Count);

        // 스폰 장소 오브젝트가 없을 경우 대비
        if (playerSpawnPoint[spawnNum] == null)
        {
            Debug.LogError("Player spawn point not found");
            return;
        }
        // 플레이어 오브젝트 생성
        Transform spawnPoint = playerSpawnPoint[spawnNum].transform;
        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // 플레이어 오브젝트 세션 데이터에 등록
        GlobalRuntimeData.CountingPlayerData(spawnedPlayer);

        // 플레이어에게 세이브 데이터 넘겨주기
        if (charData != null)
        {
            if(spawnedPlayer.TryGetComponent<PlayerStatus>(out var status))
            {
                status.initialize(charData.hpMax, charData.manaMax, charData.manaRegen);
            }
            if(spawnedPlayer.TryGetComponent<PlayerMovement>(out var movement))
            {
                movement.initialize(charData.moveSpeed);
            }
        }

        // 플레이어 스폰 여부 이벤트
        GlobalEventBus.OnPlayerSpawned?.Invoke(spawnedPlayer);
    }

    public void SpawnEnemy()
    {
        // 적 프리팹이 연결되지 않았을 경우 대비
        if (EnemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not found");
            return;
        }

        // 적 스폰 포인트가 하나도 없을 경우 대비
        if (enemySpawnPoint.Count == 0)
        {
            Debug.LogError("Enemy spawn point list is empty");
            return;
        }

        // 적 스폰 포인트 중 무작위로 하나 선정
        // 플레이어 스폰 방식과 동일한 구조로 맞춘다.
        int spawnNum = Random.Range(0, enemySpawnPoint.Count);

        // 스폰 장소 오브젝트가 없을 경우 대비
        if (enemySpawnPoint[spawnNum] == null)
        {
            Debug.LogError("Enemy spawn point not found");
            return;
        }

        // 적 오브젝트 생성
        Transform spawnPoint = enemySpawnPoint[spawnNum].transform;
        GameObject spawnedEnemy = Instantiate(EnemyPrefab, spawnPoint.position, spawnPoint.rotation);

        // 생성된 적 오브젝트를 런타임 데이터에 등록
        GlobalRuntimeData.CountingEnemyData(spawnedEnemy);
    }
}
