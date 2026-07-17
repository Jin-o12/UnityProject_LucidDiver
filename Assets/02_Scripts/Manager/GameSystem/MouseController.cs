using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    // 게임 시작 시 커서를 화면 내에 가두고 커서 숨김
    void OnEnable()
    {
        GlobalEventBus.OnMouseLocked += LockMouse;
    }

    void OnDisable()
    {
        GlobalEventBus.OnMouseLocked -= LockMouse;
    }

    void Start()
    {
        LockMouse(true);
    }

    void LockMouse(bool isLocked)
    {
        if (isLocked)
        {
            // 인벤토리 UI가 열려 있으면 커서를 잠그지 않습니다.
            InventoryUI activeInventory = FindFirstObjectByType<InventoryUI>();
            if (activeInventory != null && activeInventory.gameObject.activeInHierarchy)
                return;

            // 커서를 화면에 가둠
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            // 커서 잠금 해제
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
