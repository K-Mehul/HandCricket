using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MainMenuView : MonoBehaviour
{
    [Header("Profile Info")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI xpText;

    [Header("Buttons")]
    public Button playButton;
    public Button friendsButton;
    public Button profileButton;
    public Button leaderboardButton;
    public Button logoutButton;

    [Header("Social Feed")]
    public GameObject friendsNotificationBadge;
    public TextMeshProUGUI badgeText;

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

    public void UpdateSocialBadge(int count)
    {
        if (friendsNotificationBadge != null)
        {
            friendsNotificationBadge.SetActive(count > 0);
            if (badgeText != null) badgeText.text = count.ToString();
        }
    }
}
