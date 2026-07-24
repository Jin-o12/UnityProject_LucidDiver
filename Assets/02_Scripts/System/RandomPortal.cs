using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RandomPortal : MonoBehaviour
{
    [Serializable]
    private class PortalBooth
    {
        [Header("전화부스 확인용")]
        public GameObject boothRoot;

        [Header("선택된 전화부스에서만 활성화")]
        public GameObject portalObject;
        public GameObject spotLightObject;
        public GameObject pointLightObject;
    }

    [Header("전화부스 포탈 목록")]
    [SerializeField] private PortalBooth[] portalBooths;

    [Header("테스트 설정")]
    [SerializeField] private bool useFixedIndex;
    [SerializeField, Min(0)] private int fixedIndex;

    private int selectedPortalIndex = -1;

    private void Start()
    {
        // 게임 시작 시 세 전화부스 중 사용할 포탈 하나를 선택한다.
        SelectRandomPortal();
    }

    public void SelectRandomPortal()
    {
        if (portalBooths == null || portalBooths.Length == 0)
        {
            Debug.LogError("RandomExitPortalSelector: 등록된 전화부스가 없습니다.");
            return;
        }

        // 테스트 모드에서는 지정한 번호를 사용하고, 일반 실행에서는 무작위로 선택한다.
        selectedPortalIndex = useFixedIndex
            ? Mathf.Clamp(fixedIndex, 0, portalBooths.Length - 1)
            : UnityEngine.Random.Range(0, portalBooths.Length);

        for (int i = 0; i < portalBooths.Length; i++)
        {
            // 선택된 전화부스의 포탈 관련 오브젝트만 활성화한다.
            SetPortalBoothActive(portalBooths[i], i == selectedPortalIndex);
        }

        GameObject selectedBooth = portalBooths[selectedPortalIndex].boothRoot;

        Debug.Log(
            selectedBooth != null
                ? $"활성화된 탈출 전화부스: {selectedBooth.name}"
                : $"활성화된 탈출 전화부스 인덱스: {selectedPortalIndex}"
        );
    }

    private void SetPortalBoothActive(PortalBooth portalBooth, bool isActive)
    {
        if (portalBooth == null)
        {
            return;
        }

        // 전화부스 부모는 건드리지 않고 포탈 기능 오브젝트만 변경한다.
        SetObjectActive(portalBooth.portalObject, isActive);
        SetObjectActive(portalBooth.spotLightObject, isActive);
        SetObjectActive(portalBooth.pointLightObject, isActive);
    }

    private void SetObjectActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
