using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SocialNotificationUI : MonoBehaviour
{
    private static SocialNotificationUI _instance;
    public static SocialNotificationUI Instance => _instance;

    [Header("UI References")]
    public GameObject toastPanel;
    public TextMeshProUGUI notificationText;
    public Button acceptButton;
    public Button declineButton;
    public Button closeButton;

    private string pendingMatchId;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (toastPanel != null) toastPanel.SetActive(false);
    }

    private void Start()
    {
        Debug.Log("SocialNotificationUI: Subscribing to MatchInviteReceived event.");
        SocialService.Instance.OnMatchInviteReceived += OnInviteReceived;
        
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (declineButton != null) declineButton.onClick.AddListener(Hide);
        if (acceptButton != null) acceptButton.onClick.AddListener(OnAcceptMatchInvite);
    }

    private void OnDestroy()
    {
        if (SocialService.Instance != null)
            SocialService.Instance.OnMatchInviteReceived -= OnInviteReceived;
    }

    public void ShowMessage(string message, float duration = 5f)
    {
        if (toastPanel == null) return;

        toastPanel.SetActive(true);
        notificationText.text = message;
        
        // Hide match-specific buttons
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (declineButton != null) declineButton.gameObject.SetActive(false);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), duration);
    }

    private void OnInviteReceived(string matchId, string senderUsername, string stadium, int stake, int overs, int wickets)
    {
        Debug.Log($"SocialNotificationUI: Showing Match Invite from {senderUsername} for stadium {stadium}");
        if (toastPanel == null) return;

        pendingMatchId = matchId;
        toastPanel.SetActive(true);
        notificationText.text = $"{senderUsername} challenged you!\nVenue: {stadium} | {overs} Overs | {stake} Coins";

        // Show match-specific buttons
        if (acceptButton != null) acceptButton.gameObject.SetActive(true);
        if (declineButton != null) declineButton.gameObject.SetActive(true);

        CancelInvoke(nameof(Hide));
    }

    private async void OnAcceptMatchInvite()
    {
        if (string.IsNullOrEmpty(pendingMatchId)) return;

        Hide();
        
        Debug.Log($"Accepting challenge: {pendingMatchId}");
        
        // Join the private match
        await MatchmakingService.Instance.FindPrivateMatch(pendingMatchId);
    }

    private void Hide()
    {
        if (toastPanel != null) toastPanel.SetActive(false);
    }
}
