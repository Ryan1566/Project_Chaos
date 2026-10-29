using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One replaceable input binding per setting row; input logic belongs to InputManager.</summary>
public class SettingRow_Keybind : SettingRowBase
{
    public Action OnRebindRequested;
    public Action OnResetRequested;
    private Button _keyButton, _resetButton;
    private TextMeshProUGUI _keyText;
    private KeyIconText _keyIconText;
    private string _display = "";
    private Sprite _icon;
    private Color _iconColor = Color.white;
    private bool _listening;
    private void Awake()
    {
        _keyButton = Find<Button>("KeyButton");
        _keyText = Find<TextMeshProUGUI>("KeyText", false);
        _keyIconText = Find<KeyIconText>("KeyText", false);
        _resetButton = Find<Button>("ResetButton", false);
        // Preserve old prefab nodes, but no second binding or listener exists.
        foreach (var node in GetComponentsInChildren<Transform>(true))
            if (node.name == "MouseButton" || node.name == "MouseText") node.gameObject.SetActive(false);
        if (_keyButton != null) _keyButton.onClick.AddListener(() => OnRebindRequested?.Invoke());
        if (_resetButton != null) _resetButton.onClick.AddListener(() => OnResetRequested?.Invoke());
    }
    public void SetKeyText(string text) { SetKeyDisplay(text, null, Color.white); }
    public void SetKeyDisplay(string text, Sprite icon, Color color)
    { _display = text ?? ""; _icon = icon; _iconColor = color; Render(); }
    public void SetListening(bool listening, string prompt = null)
    { _listening = listening; Render(prompt); ApplyClickableState(); }
    private void Render(string prompt = null)
    {
        string text = _listening ? (string.IsNullOrEmpty(prompt) ? "--" : prompt) : _display;
        if (_keyIconText != null) _keyIconText.SetDisplay(text, _listening ? null : _icon, _iconColor);
        else SetText(_keyText, text);
    }
    public override void SetClickable(bool clickable) { base.SetClickable(clickable); ApplyClickableState(); }
    public override void SetInteractable(bool interactable)
    { base.SetInteractable(interactable); if (_keyIconText != null) _keyIconText.SetDimmed(!interactable); }
    private void ApplyClickableState()
    {
        if (_keyButton != null) _keyButton.interactable = Clickable && !_listening;
        if (_resetButton != null) _resetButton.interactable = Clickable && !_listening;
    }
}
