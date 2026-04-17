using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class StadiumSelectUI : UIScreen
{
    [Header("Data")]
    public StadiumRegistry registry;

    [Header("UI References")]
    public Transform container;
    public GameObject stadiumCardPrefab;
    public Button closeButton;

    [Header("Navigation")]
    public string previousScreen = "LobbyScreen";

    private List<GameObject> activeCards = new List<GameObject>();
    private System.Action<StadiumData> _onSelectionComplete;

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void ShowWithCallback(System.Action<StadiumData> callback)
    {
        _onSelectionComplete = callback;
        UIScreenManager.Instance.Show(gameObject.name);
    }

    protected override void OnShow()
    {
        base.OnShow();
        if (registry == null)
        {
            registry = Resources.Load<StadiumRegistry>("StadiumRegistry");
        }
        PopulateStadiums();

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
        {
            TutorialManager.Instance.SetState(TutorialManager.TutorialState.Stadium_Pick);
            // We highlight the first card once it's populated (next frame or short delay)
            Invoke("RegisterFirstStadiumTarget", 0.1f);
        }
    }

    private void RegisterFirstStadiumTarget()
    {
        if (activeCards.Count > 0)
        {
            var cardUI = activeCards[0].GetComponent<StadiumCardUI>();
            if (cardUI != null)
            {
                TutorialManager.Instance.RegisterTarget("FirstStadium", cardUI.GetComponent<RectTransform>());
            }
        }
    }

    private void PopulateStadiums()
    {
        // Clear old ones
        foreach (var card in activeCards) Destroy(card);
        activeCards.Clear();

        // Fetch dynamic list from StadiumService (cached from backend)
        var stadiums = StadiumService.Instance != null ? StadiumService.Instance.GetStadiums() : (registry != null ? registry.stadiums : new List<StadiumData>());

        if (stadiums == null) return;

        // Sort by level ascending, then by stake, then by name
        stadiums.Sort((a, b) => {
            int levelCompare = a.minLevel.CompareTo(b.minLevel);
            if (levelCompare != 0) return levelCompare;
            int stakeCompare = a.stake.CompareTo(b.stake);
            if (stakeCompare != 0) return stakeCompare;
            return string.Compare(a.name, b.name, StringComparison.Ordinal);
        });

        foreach (var data in stadiums)
        {
            GameObject go = Instantiate(stadiumCardPrefab, container);
            activeCards.Add(go);

            var cardUI = go.GetComponent<StadiumCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(data, OnStadiumSelected);
            }
        }
    }

    private void OnStadiumSelected(StadiumData data)
    {
        Debug.Log($"Stadium Selected: {data.name}");
        
        if (_onSelectionComplete != null)
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                if (TutorialMatchSimulation.Instance != null)
                {
                    TutorialMatchSimulation.Instance.SetMatchConfig(data.overs, data.wickets);
                }
                TutorialManager.Instance.OnActionCompleted("Stadium");
            }
            _onSelectionComplete.Invoke(data);
            _onSelectionComplete = null;
            // Do NOT call Close() here, as the callback handles transitioning to the next screen (e.g. MatchmakingScreen).
        }
        else
        {
            // Fallback for legacy calls
            LobbyScreen lobby = UIScreenManager.Instance.GetCurrentScreen<LobbyScreen>();
            if (lobby != null) lobby.SetSelectedStadium(data);
            Close();
        }
    }

    private void Close()
    {
        UIScreenManager.Instance.Show(previousScreen);
    }
}
