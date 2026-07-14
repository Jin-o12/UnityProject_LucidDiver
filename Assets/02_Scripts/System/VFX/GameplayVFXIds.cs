/// <summary>
/// 코드에서 발생하는 고정 게임 이벤트와 VFXCatalog의 문자열 ID를 연결합니다.
/// 스킬처럼 Excel/JSON에 ID가 있는 콘텐츠는 이 목록에 추가하지 않고 데이터 값을 그대로 사용합니다.
/// </summary>
public static class GameplayVFXIds
{
    public const string PlayerSpawn = "VFX_Player_Spawn";
    public const string PlayerMuzzle = "VFX_Player_Muzzle";
    public const string BulletImpactEnemy = "VFX_Bullet_Impact_Enemy";
    public const string BulletImpactWorld = "VFX_Bullet_Impact_World";
    public const string PlayerHit = "VFX_Player_Hit";
    public const string PlayerDeath = "VFX_Player_Death";
    public const string PlayerEvade = "VFX_Player_Evade";
    public const string PlayerFootstepWalk = "VFX_Player_Footstep_Walk";
    public const string PlayerFootstepRun = "VFX_Player_Footstep_Run";
    public const string PlayerHeal = "VFX_Player_Heal";
    public const string PlayerManaGain = "VFX_Player_ManaGain";
    public const string ArtifactEquip = "VFX_Artifact_Equip";
    public const string ArtifactUnequip = "VFX_Artifact_Unequip";
    public const string LucidMark01 = "VFX_LucidMark_01";
    public const string LucidMark02 = "VFX_LucidMark_02";
    public const string LucidLeak = "VFX_LucidLeak";

    public const string EnemySpawn = "VFX_Enemy_Spawn";
    public const string EnemyDetected = "VFX_Enemy_Detected";
    public const string EnemyInvestigate = "VFX_Enemy_Investigate";
    public const string EnemyAttackTelegraph = "VFX_Enemy_AttackTelegraph";
    public const string EnemySlash01 = "VFX_Enemy_Slash_01";
    public const string EnemySlash02 = "VFX_Enemy_Slash_02";
    public const string EnemyHit = "VFX_Enemy_Hit";
    public const string EnemyDeath = "VFX_Enemy_Death";

    public const string ItemDrop = "VFX_Item_Drop";
    public const string ItemPickup = "VFX_Item_Pickup";
    public const string ChestOpen = "VFX_Chest_Open";
    public const string DoorOpen = "VFX_Door_Open";
    public const string DoorClose = "VFX_Door_Close";

    public const string EscapeChannel = "VFX_Escape_Channel";
    public const string EscapeCancel = "VFX_Escape_Cancel";
    public const string EscapeSuccess = "VFX_Escape_Success";
    public const string TimePenalty = "VFX_Time_Penalty";
    public const string TimeOver = "VFX_Time_Over";
}
