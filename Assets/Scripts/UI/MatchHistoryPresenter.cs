using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MatchHistoryPresenter
{
    private readonly MatchHistoryView _view;
    private readonly SocialService _socialService;

    public MatchHistoryPresenter(MatchHistoryView view)
    {
        _view = view;
        _socialService = SocialService.Instance;

        _view.OnBackClicked += HandleBack;
    }

    public async Task RefreshHistory()
    {
        _view.ShowLoading(true);
        _view.ShowEmptyState(false);
        _view.ClearList();

        try
        {
            var history = await _socialService.FetchMatchHistoryAsync();

            _view.ShowLoading(false);

            if (history == null || history.Count == 0)
            {
                _view.ShowEmptyState(true);
            }
            else
            {
                foreach (var entry in history)
                {
                    _view.AddEntry(entry);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Presenter: Failed to refresh history: " + e.Message);
            _view.ShowLoading(false);
            _view.ShowEmptyState(true); // Fallback
        }
    }

    private void HandleBack()
    {
        UIScreenManager.Instance.Show("ProfileScreen");
    }

    public void Cleanup()
    {
        _view.OnBackClicked -= HandleBack;
    }
}
