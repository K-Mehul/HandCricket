using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class MatchHistoryView : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer;
    public GameObject entryPrefab;
    public Button backButton;
    public GameObject loadingPanel;
    public GameObject emptyStatePanel;

    public event Action OnBackClicked;

    private void Awake()
    {
        backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
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

    public void AddEntry(SocialService.MatchHistoryEntry entry)
    {
        GameObject go = Instantiate(entryPrefab, contentContainer);
        var item = go.GetComponent<MatchHistoryItemUI>();
        if (item != null)
        {
            item.SetData(entry);
        }
    }
}
