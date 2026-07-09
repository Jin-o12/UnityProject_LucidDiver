using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class SkillEffectProcessor : MonoBehaviour
{
    public static SkillEffectProcessor Instance { get; private set; }

    private void Awake()
    {
        if(Instance==null)
            Instance = this;
    }

    public async void UseSkillEffect(SkillData _skill, GameObject _user, CasterStatPayload _stats, Vector3 targetPosition)
    {
        if(_skill.effects[0].areaType == AreaType.circle)
        {
            if (string.IsNullOrEmpty(_skill.skillPrefab))
            {
                Debug.LogError($"[{_skill.skillName}] 스킬의 프리팹 주소가 없습니다");
                return;
            }

            GameObject prefabToSpawn = await AddressableLoader.LoadAssetAsync<GameObject>(_skill.skillPrefab);

            if (prefabToSpawn != null)
            {
                // 3. 로드된 해당 스킬의 프리팹을 화면에 생성!
                GameObject spawnedObject = Instantiate(prefabToSpawn, _user.transform.position, Quaternion.identity);
                
                // 4. 생성된 투사체에게 데이터 전달
                GrenadeProjectile projectile = spawnedObject.GetComponent<GrenadeProjectile>();
                if (projectile != null)
                {
                    projectile.SetupAndThrow(_skill, _user, _stats, targetPosition);
                }
            }
        }
        else
        {
            
        }
    }


}
