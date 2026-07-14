/// <summary>
/// 한 세션에 대한 휘발성 데이터들을 생성 및 관리합니다.
/// 게임 씬 실행 여부에 따라 초기화되거나 파괴되는 싱글톤입니다
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; } 
    public GlobalRuntimeData runtimeData { get; private set; }      // 게임 내 지정된 오브젝트들에 대한 데이터

    private void Awake()
    {
        if(Instance==null)
            Instance = this;
        
        runtimeData = new GlobalRuntimeData();
    }
}
