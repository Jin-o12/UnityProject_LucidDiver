using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DoorInteractable : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openParameterName = "IsOpen";
    [SerializeField] private bool canToggle = true;

    [Header("Interaction")]
    [SerializeField] private Image interactionGuideImage;

    public int DoorOpen_AudioID = 10303;

    private bool isPlayerInRange;
    private bool isOpen;

    private void Awake()
    {
        // Animator가 수동 연결되지 않았을 때 자식에서 자동으로 찾는다
        if (doorAnimator == null)
            doorAnimator = GetComponentInChildren<Animator>();

        // 상호작용 안내 이미지는 시작 시 숨긴다
        SetInteractionGuideVisible(false);
    }

    private void Update()
    {
        // 플레이어가 상호작용 범위 안에 없으면 입력을 무시한다
        if (!isPlayerInRange)
            return;

        // F 키 입력 시 문 상호작용을 실행한다
        if (Input.GetKeyDown(KeyCode.F))
            Interact();
    }

    private void Interact()
    {
        // 토글이 불가능한 문은 한 번 열린 뒤 다시 동작하지 않게 한다
        if (!canToggle && isOpen)
            return;

        // 문 열림 상태를 반전한다
        isOpen = !isOpen;

        // 문 상태에 맞는 VFX를 재생한다
        string doorVfxId = isOpen ? GameplayVFXIds.DoorOpen : GameplayVFXIds.DoorClose;
        VFXService.Instance?.Play(doorVfxId, transform.position, transform.rotation);

        // Animator 파라미터를 변경해 문 애니메이션을 재생한다
        if (doorAnimator != null)
            doorAnimator.SetBool(openParameterName, isOpen);

        // 문 열기/닫기 사운드 재생 이벤트를 AudioManager에 전달한다
        GlobalEventBus.OnPlay3DSoundRequested?.Invoke(DoorOpen_AudioID, transform.position);

        // 문 상태와 관계없이 플레이어가 범위 안에 있으면 안내 이미지를 계속 표시한다
        SetInteractionGuideVisible(isPlayerInRange);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 상호작용 범위에 들어왔는지 확인한다
        if (!other.CompareTag("Player"))
            return;

        // 플레이어가 상호작용 범위 안에 있음을 저장한다
        isPlayerInRange = true;

        // 문이 열려 있든 닫혀 있든 상호작용 안내 이미지를 표시한다
        SetInteractionGuideVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 상호작용 범위에서 나갔는지 확인한다
        if (!other.CompareTag("Player"))
            return;

        // 플레이어가 상호작용 범위 밖으로 나갔음을 저장한다
        isPlayerInRange = false;

        // 범위 밖으로 나가면 상호작용 안내 이미지를 숨긴다
        SetInteractionGuideVisible(false);
    }

    private void SetInteractionGuideVisible(bool visible)
    {
        // 상호작용 안내 Image가 연결되지 않았으면 처리하지 않는다
        if (interactionGuideImage == null)
            return;

        // Image 컴포넌트가 붙은 오브젝트를 표시하거나 숨긴다
        interactionGuideImage.gameObject.SetActive(visible);
    }
}
