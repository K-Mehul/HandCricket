using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchmakingScreen : UIScreen
{
    private MatchmakingView _view;
    private MatchmakingPresenter _presenter;

    private void Awake()
    {
        _view = GetComponent<MatchmakingView>();
        if (_view == null)
        {
            _view = gameObject.AddComponent<MatchmakingView>();
        }

        _presenter = new MatchmakingPresenter(_view);
    }

    private void OnDestroy()
    {
        _presenter?.Cleanup();
    }

    public void Init(string stadiumName, Sprite icon, string code = null)
    {
        _presenter.Initialize(stadiumName, icon, code);
    }

    protected override void OnShow()
    {
        base.OnShow();
    }

    protected override void OnHide()
    {
        base.OnHide();
        _presenter?.Cleanup(); // Ensure logic stops when hidden
    }

    void Update()
    {
        _presenter?.Update();
    }
}

