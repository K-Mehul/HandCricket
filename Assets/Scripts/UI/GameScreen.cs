using UnityEngine;

public class GameScreen : UIScreen
{
    private GameView _view;
    private GamePresenter _presenter;

    void Awake()
    {
        _view = GetComponent<GameView>();
        if (_view == null) _view = gameObject.AddComponent<GameView>();
        
        _presenter = new GamePresenter(_view);
    }

    protected override void OnShow()
    {
        base.OnShow();
        _presenter.Subscribe();
        
        // Initial State check
        if (ClientMatchHandler.Instance != null && !string.IsNullOrEmpty(ClientMatchHandler.Instance.LastTossInitiatorId))
        {
            bool isMeTossing = (ClientMatchHandler.Instance.LastTossInitiatorId == NakamaSessionManager.Session.UserId);
            _presenter.Init(isMeTossing);
        }
        else
        {
            _view.ShowPanel("Wait");
        }

        // Request latest state if possible
        ClientMatchHandler.Instance?.RequestMatchState();
    }

    void OnDisable()
    {
        _presenter?.Cleanup();
    }

    void OnDestroy()
    {
        _presenter?.Cleanup();
    }
}