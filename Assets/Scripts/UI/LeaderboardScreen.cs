using UnityEngine;

[RequireComponent(typeof(LeaderboardView))]
public class LeaderboardScreen : UIScreen
{
    private LeaderboardView _view;
    private LeaderboardPresenter _presenter;
    private bool _isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        _view = GetComponent<LeaderboardView>();
        _presenter = new LeaderboardPresenter(_view);
        _isInitialized = true;
    }

    public override void Show()
    {
        base.Show();
        Initialize();
        _ = _presenter.RefreshLeaderboard();
    }

    private void OnDestroy()
    {
        _presenter?.Cleanup();
    }
}
