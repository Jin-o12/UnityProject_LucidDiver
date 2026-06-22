using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitPoint : MonoBehaviour, IInteractable
{
    public bool Interact(int playerID) 
    {
        Debug.Log("탈출구와 상호작용 함");

        return true;    // 상호작용 성공, 상호작용 리스트에서 삭제 요청
    }
}
