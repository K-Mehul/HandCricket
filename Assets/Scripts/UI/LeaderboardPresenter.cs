using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LeaderboardPresenter
{
    private readonly LeaderboardView _view;
    private readonly SocialService _socialService;

    private int _currentRequestCount = 0;
    private bool _friendsOnly = false;
    
    private LeaderboardView.LeaderboardCategory _currentCategory = LeaderboardView.LeaderboardCategory.Wins;

    public LeaderboardPresenter(LeaderboardView view)
    {
        _view = view;
        _socialService = SocialService.Instance;

        _view.OnBackClicked += HandleBack;
        _view.OnCategoryChanged += HandleCategoryChange;
        _view.OnFriendsFilterChanged += HandleFriendsFilterChange;
    }

    public async Task RefreshLeaderboard()
    {
        int requestId = ++_currentRequestCount;
        
        _view.ShowLoading(true);
        _view.ShowEmptyState(false);
        _view.ClearList();
        _view.UpdateTabVisuals(_currentCategory);

        string leaderboardId = GetLeaderboardId();

        try
        {
            var entries = await _socialService.FetchLeaderboardAsync(leaderboardId, 50, _friendsOnly);

            // If a newer request has started, ignore this one
            if (requestId != _currentRequestCount) return;

            _view.ShowLoading(false);

            if (entries == null || entries.Count == 0)
            {
                _view.ShowEmptyState(true);
            }
            else
            {
                foreach (var entry in entries)
                {
                    _view.AddEntry(entry);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("LeaderboardPresenter: Failed to refresh: " + e.Message);
            _view.ShowLoading(false);
            _view.ShowEmptyState(true);
        }
    }

    private string GetLeaderboardId()
    {
        return (_currentCategory == LeaderboardView.LeaderboardCategory.Wins) ? "global_wins" : "global_runs";
    }

    private void HandleCategoryChange(LeaderboardView.LeaderboardCategory cat)
    {
        if (_currentCategory == cat) return;
        _currentCategory = cat;
        _ = RefreshLeaderboard();
    }

    private void HandleFriendsFilterChange(bool friendsOnly)
    {
        if (_friendsOnly == friendsOnly) return;
        _friendsOnly = friendsOnly;
        _ = RefreshLeaderboard();
    }

    private void HandleBack()
    {
        UIScreenManager.Instance.Show("MainMenuScreen");
    }

    public void Cleanup()
    {
        _view.OnBackClicked -= HandleBack;
        _view.OnCategoryChanged -= HandleCategoryChange;
        _view.OnFriendsFilterChanged -= HandleFriendsFilterChange;
    }
}
