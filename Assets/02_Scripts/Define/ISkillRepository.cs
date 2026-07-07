public interface ISkillRepository
{
    /* 외부에서 TID로 스킬 데이터를 꺼내갈 때 사용하는 함수 */
    SkillData GetSkillData(int skillID);
}
