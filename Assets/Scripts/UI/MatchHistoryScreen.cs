using UnityEngine;

[RequireComponent(typeof(MatchHistoryView))]
public class MatchHistoryScreen : UIScreen
{
    private MatchHistoryView _view;
    private MatchHistoryPresenter _presenter;
    private bool _isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        _view = GetComponent<MatchHistoryView>();
        _presenter = new MatchHistoryPresenter(_view);
        _isInitialized = true;
    }

    public override void Show()
    {
        base.Show();
        Initialize();
        _ = _presenter.RefreshHistory();
    }

    private void OnDestroy()
    {
        _presenter?.Cleanup();
    }
}
