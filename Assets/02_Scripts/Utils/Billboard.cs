/// <summary>
/// 빌보드 효과 스크릡트
/// 어떤 각도에서도 메인 카메라를 정면으로 바라보게 합니다
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // 카메라가 늦게 생길 수 있으므로 매 프레임 유효하면 갱신
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        if (mainCameraTransform != null)
        {
            // 자신의 앞면 방향과 카메라의 앞면 방향을 일치시켜 앞을 보게 함
            transform.forward = mainCameraTransform.forward;
        }
    }
}
