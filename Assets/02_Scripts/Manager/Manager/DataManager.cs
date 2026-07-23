/// <summary>
/// 게임 데이터들을 불러오고 관리하는 인스턴스 클래스
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // 플레이어 저장 데이터
    public PlayerSaveData playerData { get; private set; }          // 계정 데이터 보관소
    // public PlayerSaveDataSO playerRuntimeDataSO;                 // 데이터 일시적 저장 및 관리 SO
    // private ISaveRepository saveRepo;   

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 같은 Manager 오브젝트에 다른 매니저 컴포넌트가 함께 붙어 있으므로 오브젝트 전체가 아니라 DataManager 컴포넌트만 제거합니다.
            Destroy(this);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // // 인터페이스 구현부 연결
        // saveRepo = playerRuntimeDataSO;
        
        // // 플레이어 데이터 불러오기
        // playerData = saveRepo.LoadSaveData();

        // // 전역 로케이터에 등록하여 UI 등 하위 계층에서 접근할 수 있도록 함
        // DataServiceLocator.SaveRepo = saveRepo;
    }

    // /* 플레이어 게임 데이터 저장하기 */
    // public void SaveGame()
    // {
    //     saveRepo.SaveGameData(playerRuntimeDataSO.currentData);
    //     Debug.Log("Game Saved");
    // }

    // /* 플레이어 게임 데이터 불러오기 */
    // public void LoadGame()
    // {
    //     playerData = saveRepo.LoadSaveData();
    // }
}
