using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRootData
{
    public int TID;                     // 고유 ID
    public ItemData[] itemID;           // 전리품으로 생성되는 아이템 정보 리스트
    public int itemCut;                 // 전리품으로 생성되는 아이템의 갯수
}
