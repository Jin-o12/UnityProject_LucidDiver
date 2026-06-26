using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectBoxData
{
    public int TID;                 // 고유 ID
    public int worldID;             // 보관함이 배치되는 세션 ID
    public BoxSize boxSize;         // 보관함 종류
    public ItemRootData itemList;   // 보관함을 열었을 시 획득할 수 있는 아이템 리스트
}
