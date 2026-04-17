using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class MainMenuView : MonoBehaviour
{
    [Header("Profile Info")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI xpText;

    [Header("Buttons")]
    public Button playButton;
    public RectTransform playButtonRect;
    public Button friendsButton;
    public Button profileButton;
    public Button leaderboardButton;
    public Button logoutButton;

    [Header("Social Feed")]
    public GameObject friendsNotificationBadge;
    public TextMeshProUGUI badgeText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip coinTickClip;

    // Events for the Presenter to listen to
    public event Action OnPlayClicked;
    public event Action OnFriendsClicked;
    public event Action OnProfileClicked;
    public event Action OnLeaderboardClicked;
    public event Action OnLogoutClicked;

    private void Start()
    {
        if (playButton != null) playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());
        if (friendsButton != null) friendsButton.onClick.AddListener(() => OnFriendsClicked?.Invoke());
        if (profileButton != null) profileButton.onClick.AddListener(() => OnProfileClicked?.Invoke());
        if (leaderboardButton != null) leaderboardButton.onClick.AddListener(() => OnLeaderboardClicked?.Invoke());
        if (logoutButton != null) logoutButton.onClick.AddListener(() => OnLogoutClicked?.Invoke());
    }

    public void UpdateProfile(string username, int level, int coins, int xp)
    {
        if (usernameText != null) usernameText.text = username;
        if (levelText != null) levelText.text = $"Lvl {level}";
        if (coinsText != null) coinsText.text = coins.ToString();
        if (xpText != null) xpText.text = $"XP: {xp}";
    }

    public void AnimateProfileStats(int targetCoins, int targetXP, float duration = 1.5f)
    {
        // Start from current displayed (or 0 if new)
        int startCoins = 0;
        int startXP = 0;

        // Coins Animation
        DOTween.To(() => startCoins, x => {
            startCoins = x;
            if (coinsText != null) coinsText.text = startCoins.ToString();
            // Play a tick sound every few counts or just rhythmic
            if (audioSource != null && coinTickClip != null && x % 5 == 0) 
                audioSource.PlayOneShot(coinTickClip, 0.5f);
        }, targetCoins, duration).SetEase(Ease.OutQuad)
        .OnComplete(() => {
            if (coinsText != null) coinsText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        });

        // XP Animation
        DOTween.To(() => startXP, x => {
            startXP = x;
            if (xpText != null) xpText.text = $"XP: {startXP}";
        }, targetXP, duration).SetEase(Ease.OutQuad)
        .OnComplete(() => {
            if (xpText != null) xpText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        });
    }

    public void UpdateSocialBadge(int count)
    {
        if (friendsNotificationBadge != null)
        {
            friendsNotificationBadge.SetActive(count > 0);
            if (badgeText != null) badgeText.text = count.ToString();
        }
    }
}
