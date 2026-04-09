using System;
using System.Threading.Tasks;
using UnityEngine;

public class MatchmakingPresenter
{
    private readonly MatchmakingView _view;
    private readonly UserData _userData;
    private readonly UIScreenManager _screenManager;
    
    private float _timer = 0f;
    private bool _isActive = false;
    private string _currentCode;
    private string _currentStadium;
    private bool _isPrivateMatch;

    public MatchmakingPresenter(MatchmakingView view)
    {
        _view = view;
        _screenManager = UIScreenManager.Instance;

        SubscribeToViewEvents();
    }

    private void SubscribeToViewEvents()
    {
        _view.OnCancelClicked += HandleCancel;
        _view.OnCopyClicked += HandleCopyCode;
    }

    public void Cleanup()
    {
        _isActive = false;
    }

    public void Initialize(string stadiumName, Sprite icon, string code = null)
    {
        _currentStadium = stadiumName;
        _currentCode = code;
        _isPrivateMatch = !string.IsNullOrEmpty(code);
        _timer = 0f;
        _isActive = true;

        _view.SetStadiumIcon(icon);
        UpdateSearchStatus();
        UpdateTimerUI();
    }

    private void UpdateSearchStatus()
    {
        string message = _isPrivateMatch 
            ? $"Waiting for opponent...\nRoom Code: {_currentCode}" 
            : $"Searching for opponent in {_currentStadium}...";
        
        _view.SetSearchingInfo(message, _isPrivateMatch);
    }

    private void UpdateProfileUI() {}

    public void Update()
    {
        if (!_isActive) return;

        _timer += Time.deltaTime;
        UpdateTimerUI();

        // AI Ghost Bot Fallback (30 Seconds)
        if (!_isPrivateMatch && _timer >= 10f)
        { 
            _isActive = false;
            TriggerBotMatch();
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(_timer / 60f);
        int seconds = Mathf.FloorToInt(_timer % 60f);
        _view.SetTimer(string.Format("{0:00}:{1:00}", minutes, seconds));
    }

    private void HandleCopyCode()
    {
        if (string.IsNullOrEmpty(_currentCode)) return;
        
        ClipboardHelper.Copy(_currentCode);
        _view.ShowCopyStatus(true);
        
        // Hide status after 2 seconds
        HideCopyStatusAfterDelay();
    }

    private async void HideCopyStatusAfterDelay()
    {
        await Task.Delay(2000);
        _view.ShowCopyStatus(false);
    }

    private async void HandleCancel()
    {
        _isActive = false;
        
        if (MatchmakingService.Instance != null)
        {
            await MatchmakingService.Instance.CancelMatchmaking();
        }

        _screenManager.Show("LobbyScreen");
    }

    private async void TriggerBotMatch()
    {
        if (MatchmakingService.Instance != null)
        {
             await MatchmakingService.Instance.CancelMatchmaking();
             await MatchmakingService.Instance.RequestBotMatch(_currentStadium);
        }
    }
}
