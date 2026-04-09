using TMPro;
using UnityEngine;

public class MainMenuScreen : UIScreen
{
    public UserData userData;
    private MainMenuView _view;
    private MainMenuPresenter _presenter;

    private void Awake()
    {
        _view = GetComponent<MainMenuView>();
        _presenter = new MainMenuPresenter(_view, userData);
    }

    public override async void Show()
    {
        base.Show();
        if (_presenter != null)
        {
            await _presenter.Initialize();
        }
    }

    private void OnDestroy()
    {
        _presenter?.Cleanup();
    }
}
