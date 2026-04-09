using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchmakingView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI searchingText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Image stadiumIcon;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private TextMeshProUGUI copyStatusText;

    public event Action OnCancelClicked;
    public event Action OnCopyClicked;

    private void Awake()
    {
        if (cancelButton != null) cancelButton.onClick.AddListener(() => OnCancelClicked?.Invoke());
        if (copyCodeButton != null) copyCodeButton.onClick.AddListener(() => OnCopyClicked?.Invoke());
        if (copyStatusText != null) copyStatusText.gameObject.SetActive(false);
    }

    public void SetSearchingInfo(string message, bool showCopyButton)
    {
        if (searchingText != null) searchingText.text = message;
        if (copyCodeButton != null) copyCodeButton.gameObject.SetActive(showCopyButton);
    }

    public void SetTimer(string timeString)
    {
        if (timerText != null) timerText.text = timeString;
    }

    public void SetStadiumIcon(Sprite icon)
    {
        if (stadiumIcon != null)
        {
            stadiumIcon.sprite = icon;
            Debug.Log($"MatchmakingView: Set Icon to {(icon != null ? icon.name : "NULL")}");
        }
        else
        {
            Debug.LogWarning("MatchmakingView: stadiumIcon Image reference is MISSING in Inspector!");
        }
    }

    public void ShowCopyStatus(bool show)
    {
        if (copyStatusText != null)
        {
            copyStatusText.text = "Copied!";
            copyStatusText.gameObject.SetActive(show);
        }
    }
}
