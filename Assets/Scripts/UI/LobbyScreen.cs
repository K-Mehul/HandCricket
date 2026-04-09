using UnityEngine;

public class LobbyScreen : UIScreen
{
    public UserData userData;
    
    private LobbyView _view;
    private LobbyPresenter _presenter;

    void Awake()
    {
        _view = GetComponent<LobbyView>();
        if (_view == null) _view = gameObject.AddComponent<LobbyView>();
        
        _presenter = new LobbyPresenter(_view, userData);
    }

    protected override void OnShow()
    {
        base.OnShow();
        _view.SetStatus("Ready");
        _view.ShowJoinPreview(false);
        _view.ClearJoinInput();
        _view.SetMatchmakingState(false);
    }

    void OnDestroy()
    {
    }

    // Compatibility method for StadiumSelectUI
    public void SetSelectedStadium(StadiumData data)
    {
        _view.UpdateStadiumInfo(data.displayName, $"{data.overs} Overs | {data.wickets} Wickets | {data.stake} Coins");
    }
}
