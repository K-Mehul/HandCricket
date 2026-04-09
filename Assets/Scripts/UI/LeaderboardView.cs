using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class LeaderboardView : MonoBehaviour
{
    [Header("Navigation")]
    public Button backButton;

    [Header("Category Tabs (Wins / Runs)")]
    public Button winsTabButton;
    public Button runsTabButton;

    [Header("Friends Filter")]
    public Toggle friendsOnlyToggle;

    [Header("List UI")]
    public Transform contentContainer;
    public GameObject leaderboardItemPrefab;
    public GameObject loadingPanel;
    public GameObject emptyStatePanel;

    public event Action OnBackClicked;
    public event Action<LeaderboardCategory> OnCategoryChanged;
    public event Action<bool> OnFriendsFilterChanged;

    public enum LeaderboardCategory { Wins, Runs }

    private void Awake()
    {
        backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        
        winsTabButton.onClick.AddListener(() => OnCategoryChanged?.Invoke(LeaderboardCategory.Wins));
        runsTabButton.onClick.AddListener(() => OnCategoryChanged?.Invoke(LeaderboardCategory.Runs));

        friendsOnlyToggle.onValueChanged.AddListener((val) => OnFriendsFilterChanged?.Invoke(val));
    }

    public void ShowLoading(bool visible)
    {
        if (loadingPanel != null) loadingPanel.SetActive(visible);
    }

    public void ShowEmptyState(bool visible)
    {
        if (emptyStatePanel != null) emptyStatePanel.SetActive(visible);
    }

    public void ClearList()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void AddEntry(SocialService.LeaderboardEntry entry)
    {
        GameObject go = Instantiate(leaderboardItemPrefab, contentContainer);
        var item = go.GetComponent<LeaderboardItemUI>();
        if (item != null)
        {
            item.SetData(entry);
        }
    }

    public void UpdateTabVisuals(LeaderboardCategory cat)
    {
        // Simple color feedback
        if (winsTabButton != null) winsTabButton.GetComponent<Image>().color = (cat == LeaderboardCategory.Wins) ? Color.white : Color.gray;
        if (runsTabButton != null) runsTabButton.GetComponent<Image>().color = (cat == LeaderboardCategory.Runs) ? Color.white : Color.gray;
    }
}
