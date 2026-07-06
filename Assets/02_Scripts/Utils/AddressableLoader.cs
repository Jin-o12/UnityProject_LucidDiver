/// <summary>
/// Addressable 에셋을 가져오는 기능을 가진 메소드(클래스) 
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : MonoBehaviour
{
    public static async Task<T> LoadAssetAsync<T>(string addressKey) where T : Object
    {
        // 키 값이 비어있는지 방어 코드
        if (string.IsNullOrEmpty(addressKey))
        {
            Debug.LogWarning("[ResourceManager] 로드하려는 어드레시블 주소가 비어있습니다");
            return null;
        }

        try
        {
            // 2. 어드레시블 시스템에 로드 요청
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(addressKey);

            // 3. 로드가 끝날 때까지 대기 (이 동안 게임이 멈추지 않고 백그라운드에서 로드됨)
            await handle.Task;

            // 4. 로드 성공 여부 체크
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Debug.LogError($"[ResourceManager] 에셋 로드 실패 | 주소: {addressKey}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ResourceManager] 에셋 로드 중 에러 발생 ({addressKey}): {e.Message}");
            return null;
        }
    }
}
