using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Nakama;

public class SocialPresenter
{
    private readonly SocialView _view;
    private readonly SocialService _socialService;
    private string _activeTabId = "friends";
    private string _searchQuery = "";

    // Tab ID to Nakama Friend State mapping
    // 0=Friend, 1=Invite Sent, 2=Invite Received, 3=Blocked
    private readonly Dictionary<string, int> _tabStateMap = new Dictionary<string, int>
    {
        { "friends", 0 },
        { "incoming", 2 },
        { "sent", 1 }
    };

    public SocialPresenter(SocialView view)
    {
        _view = view;
        _socialService = SocialService.Instance;

        // Subscribe to View events
        _view.OnTabSelected += HandleTabSelected;
        _view.OnAddFriend += HandleAddFriend;
        _view.OnSearchChanged += HandleSearchChanged;
        _view.OnBack += HandleBack;

        // Subscribe to Service events
        _socialService.OnSocialUpdate += RefreshList;

        InitializeTabs();
    }

    private void InitializeTabs()
    {
        var configs = new List<(string id, string name)>
        {
            ("friends", "Friends"),
            ("incoming", "Requests"),
            ("sent", "Sent")
        };

        _view.CreateTabs(configs);
        _view.UpdateTabVisuals(_activeTabId);
    }

    private void HandleTabSelected(string tabId)
    {
        _activeTabId = tabId;
        _view.UpdateTabVisuals(_activeTabId);
        RefreshList();
    }

    private void HandleSearchChanged(string query)
    {
        _searchQuery = query;
        RefreshList();
    }

    private async void HandleAddFriend(string username)
    {
        username = username.Trim();
        if (string.IsNullOrEmpty(username))
        {
            _view.SetStatus("Enter a username!");
            return;
        }

        _view.SetStatus($"Sending request to {username}...");
        var result = await _socialService.AddFriendByUsername(username);
        
        _view.SetStatus(result.message);
        if (result.success)
        {
            _view.ClearInputs();
            RefreshList();
        }
    }

    public async void RefreshList()
    {
        _view.SetStatus($"Loading {_activeTabId}...");
        _view.ClearContainer();

        var allFriends = await _socialService.ListFriends();
        int targetState = _tabStateMap[_activeTabId];

        int count = 0;
        foreach (var friend in allFriends)
        {
            // Filter by Tab Relationship State
            if (friend.State != targetState) continue;

            // Filter by Search
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                if (!friend.User.Username.ToLower().Contains(_searchQuery.ToLower()))
                    continue;
            }

            SpawnFriendItem(friend);
            count++;
        }

        if (count == 0)
        {
            _view.SetStatus($"No {_activeTabId} found.");
        }
        else
        {
            _view.SetStatus($"Showing {count} entries.");
        }
    }

    private void SpawnFriendItem(IApiFriend friend)
    {
        GameObject go = GameObject.Instantiate(_view.itemPrefab, _view.listContainer);
        var ui = go.GetComponent<FriendItemUI>();
        // FriendItemUI handles its own action internal logic, but we pass the refresh callback
        ui.Setup(friend, RefreshList);
    }

    private void HandleBack()
    {
        UIScreenManager.Instance.Show("MainMenuScreen");
    }

    public void Cleanup()
    {
        if (_socialService != null) _socialService.OnSocialUpdate -= RefreshList;
    }
}
