using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New MemoryPiece", menuName = "GameData/Item/MemoryPiece")]
public class MemoryPieceitemData : ItemData
{
    public UserType userType;       // 기억 조각 대응 캐릭터 종류
    public int charID;              // 캐릭터 ID
    public int linkRateGain;        // 동조율 상승 값
}
