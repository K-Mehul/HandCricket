using UnityEngine;

public class ProfilePresenter
{
    private readonly ProfileView _view;
    private readonly UserData _userData;

    public ProfilePresenter(ProfileView view, UserData userData)
    {
        _view = view;
        _userData = userData;

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _view.OnBackClicked += HandleBack;
        _view.OnBattingTabClicked += () => _view.ShowTab(true);
        _view.OnBowlingTabClicked += () => _view.ShowTab(false);
        _view.OnMatchHistoryClicked += () => UIScreenManager.Instance.Show("MatchHistoryScreen");
        
        _userData.OnDataChanged += RefreshUI;
    }

    public void Cleanup()
    {
        _view.OnBackClicked -= HandleBack;
        _userData.OnDataChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        _view.SetUserInfo(_userData.username, _userData.level, _userData.coins);

        // Calculate Batting Stats
        float batAvg = _userData.Matches > 0 ? (float)_userData.TotalRuns / _userData.Matches : 0;
        float sr = _userData.BallsFaced > 0 ? ((float)_userData.TotalRuns / _userData.BallsFaced) * 100f : 0;
        
        _view.SetBattingStats(
            _userData.TotalRuns, 
            _userData.HighestScore, 
            batAvg, 
            sr, 
            _userData.FourCount, 
            _userData.SixCount, 
            _userData.Matches
        );

        // Calculate Bowling Stats
        string bb = _userData.BestWickets + "/" + _userData.BestRuns;
        float bowlEcon = _userData.BallsBowled > 0 ? ((float)_userData.RunsConceded / _userData.BallsBowled) * 6f : 0;
        float bowlAvg = _userData.TotalWickets > 0 ? (float)_userData.RunsConceded / _userData.TotalWickets : 0;
        float bowlSR = _userData.TotalWickets > 0 ? (float)_userData.BallsBowled / _userData.TotalWickets : 0;

        _view.SetBowlingStats(
            _userData.TotalWickets,
            bb,
            bowlEcon,
            bowlAvg,
            bowlSR,
            _userData.Matches
        );

        // General Stats
        string record = $"{_userData.Wins}W / {_userData.Losses}L / {_userData.Draws}D";
        float winPercent = _userData.Matches > 0 ? ((float)_userData.Wins / _userData.Matches) * 100f : 0;
        string winRatioStr = $"Win rate: {winPercent:F1}%";

        _view.SetGeneralStats(record, winRatioStr);
        
        _view.ShowTab(true); // Default to batting tab
    }

    private void HandleBack()
    {
        UIScreenManager.Instance.Show("MainMenuScreen");
    }
}
