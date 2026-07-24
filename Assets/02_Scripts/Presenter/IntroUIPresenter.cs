using UnityEngine;
using UnityEngine.InputSystem;

public class IntroUIPresenter : MonoBehaviour
{
    UIManager uiManager;                    // UI 메니저 인스턴스
    private InputAction openSettingAction;  // 설정 창 열기 이벤트
    private SettingUI settingUI;            // 설정 UI 캐시

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnOpenSettingUI += OpenSettingPopup;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnOpenSettingUI -= OpenSettingPopup;
    }

    private void Start()
    {
        uiManager = UIManager.Instance;
    }

    /* 설졍 UI 열기 */
    public void OpenSettingPopup()
    {
        if (settingUI == null)
        {
            settingUI = uiManager.Open<SettingUI>();
            return;
        }

        if (settingUI.gameObject.activeInHierarchy == false)
        {
            uiManager.Open<SettingUI>();
        }
        else
        {
            uiManager.Close<SettingUI>();
        }
    }
}
