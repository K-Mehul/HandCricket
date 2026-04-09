using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SocialTabView : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI tabNameText;
    public Button button;

    public string TabId { get; private set; }
    private Action<string> _onSelected;

    public void Setup(string id, string displayName, Action<string> onSelectedCallback)
    {
        TabId = id;
        tabNameText.text = displayName;
        _onSelected = onSelectedCallback;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _onSelected?.Invoke(TabId));
    }

    public void SetSelected(bool isSelected, Color activeColor, Color idleColor)
    {
        if (tabNameText != null)
        {
            tabNameText.color = isSelected ? activeColor : idleColor;
            tabNameText.fontStyle = isSelected ? FontStyles.Underline : FontStyles.Normal;
        }
    }
}
