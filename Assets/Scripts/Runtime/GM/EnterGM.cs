using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EnterGM : MonoBehaviour
{
    public GameObject _gmPanel;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Awake()
    {
        if (TryGetComponent<Button>(out var button)) button.onClick.AddListener(ToggleGM);
    }

    private void Update()
    {
        // 开发快捷键独立于可改绑的游戏操作，避免把组合键登记为玩法按键。
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.gKey.wasPressedThisFrame
            || !keyboard.ctrlKey.isPressed || !keyboard.shiftKey.isPressed) return;
        if (_gmPanel != null && !_gmPanel.activeSelf && !InputManager.Instance.IsRebinding)
            _gmPanel.SetActive(true);
    }

    private void ToggleGM()
    {
        if (_gmPanel != null) _gmPanel.SetActive(!_gmPanel.activeSelf);
    }
#endif
}
