using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSetupManager : MonoBehaviour
{
    [Header("시네머신 카메라")]
    public CinemachineVirtualCamera virtualCamera; // Unity 6의 새 컴포넌트 이름

    private void OnEnable()
    {
        GlobalEventBus.OnPlayerSpawned += SetupCameraTarget;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnPlayerSpawned -= SetupCameraTarget;
    }

    private void SetupCameraTarget(GameObject localPlayer)
    {
        // 카메라가 쫓아갈 핵심 타겟 지점을 찾음
        Transform targetRoot = localPlayer.transform.Find("CameraRoot");

        if (targetRoot == null)
        {
            // CameraRoot가 없다면 몸통 전체를 타겟
            targetRoot = localPlayer.transform;
        }

        // 시네머신 카메라의 타겟으로 설정합니다.
        virtualCamera.Follow = targetRoot; // 카메라가 따라갈 위치
        virtualCamera.LookAt = targetRoot; // 카메라가 바라볼 위치 (필요에 따라 생략 가능)
    }
}
