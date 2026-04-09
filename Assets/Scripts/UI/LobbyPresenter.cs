using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LobbyPresenter
{
    private readonly LobbyView _view;
    private readonly UserData _userData;
    private readonly MatchmakingService _matchmakingService;
    private readonly UIScreenManager _screenManager;

    private string _pendingRoomCode;
    private string _pendingStadiumName;
    private Sprite _pendingStadiumIcon;
    private int _pendingStake;
    private int _pendingOvers;
    private int _pendingWickets;

    public LobbyPresenter(LobbyView view, UserData userData)
    {
        _view = view;
        _userData = userData;
        _matchmakingService = MatchmakingService.Instance;
        _screenManager = UIScreenManager.Instance;

        SubscribeToViewEvents();
    }

    private void SubscribeToViewEvents()
    {
        _view.OnFindMatch += () => _ = HandleFindMatch();
        _view.OnCancelMatch += () => _ = HandleCancelMatch();
        _view.OnCreatePrivate += () => _ = HandleCreatePrivate();
        _view.OnJoinMatch += (code) => _ = HandleJoinMatch(code);
        _view.OnConfirmJoin += () => _ = HandleConfirmJoin();
        _view.OnCancelJoin += () => _view.ShowJoinPreview(false);
        _view.OnOpenStadiumSelect += () => _screenManager.Show("StadiumSelectScreen");
        _view.OnBack += () => {
            _matchmakingService.CancelMatchmaking();
            _screenManager.Show("MainMenuScreen");
        };
        _view.OnBack += () => {
            _matchmakingService.CancelMatchmaking();
            _screenManager.Show("MainMenuScreen");
        };
    }

    // Profile data is now handled primarily by MainMenuPresenter
    private void UpdateUsernameAndWallet() {}

    private async Task HandleFindMatch()
    {
        var stadiumUI = _screenManager.GetScreen<StadiumSelectUI>("StadiumSelectScreen");
        if (stadiumUI == null) return;

        stadiumUI.ShowWithCallback(async stadium => {
            if (!ValidateRequirements(stadium.stake, stadium.minLevel)) return;

            _view.UpdateStadiumInfo(stadium.displayName, $"{stadium.overs} Overs | {stadium.wickets} Wickets | {stadium.stake} Coins");
            
            _screenManager.Show("MatchmakingScreen");
            var mmScreen = _screenManager.GetCurrentScreen<MatchmakingScreen>();
            if (mmScreen != null) mmScreen.Init(stadium.displayName, stadium.icon);

            await _matchmakingService.FindGlobalMatch(stadium.stake, stadium.name);
        });
    }

    private async Task HandleCancelMatch()
    {
        await _matchmakingService.CancelMatchmaking();
        _view.SetStatus("Matchmaking Cancelled");
        _view.SetMatchmakingState(false);
    }

    private async Task HandleCreatePrivate()
    {
        var stadiumUI = _screenManager.GetScreen<StadiumSelectUI>("StadiumSelectScreen");
        if (stadiumUI == null) return;

        stadiumUI.ShowWithCallback(async stadium => {
            if (!ValidateRequirements(stadium.stake, stadium.minLevel)) return;

            _view.SetStatus("Generating Code...");
            string code = _matchmakingService.GenerateJoinCode();
            
            await _matchmakingService.RegisterPrivateRoom(code, stadium.displayName, stadium.stake, stadium.overs, stadium.wickets, stadium.minLevel);

            _screenManager.Show("MatchmakingScreen");
            var mmScreen = _screenManager.GetCurrentScreen<MatchmakingScreen>();
            if (mmScreen != null) mmScreen.Init(stadium.displayName, stadium.icon, code);

            await _matchmakingService.FindPrivateMatch(code, stadium.stake, stadium.overs, stadium.wickets);
        });
    }

    private async Task HandleJoinMatch(string code)
    {
        code = code.ToUpper();
        if (string.IsNullOrEmpty(code) || code.Length != 8)
        {
            _view.SetStatus("Invalid Code.");
            return;
        }

        _view.SetStatus("Fetching room details...");
        var details = await SocialService.Instance.GetPrivateRoomDetails(code);
        
        if (details == null)
        {
            _view.SetStatus("Room not found or expired.");
            return;
        }

        _pendingRoomCode = code;
        _pendingStadiumName = details.ContainsKey("stadium") ? details["stadium"] : "Unknown Stadium";
        
        // Fetch local asset for the icon
        var stadiumData = StadiumService.Instance != null ? StadiumService.Instance.GetStadium(_pendingStadiumName) : null;
        _pendingStadiumIcon = stadiumData != null ? stadiumData.icon : null;

        _pendingStake = int.Parse(details["stake"]);
        _pendingOvers = int.Parse(details["overs"]);
        _pendingWickets = int.Parse(details["wickets"]);
        int minLevel = details.ContainsKey("minLevel") ? int.Parse(details["minLevel"]) : 0;

        if (!ValidateRequirements(_pendingStake, minLevel)) return;

        string detailMsg = $"Stadium: {details["stadium"]}\nStake: {details["stake"]} Coins\nRules: {details["overs"]} Overs | {details["wickets"]} Wickets";
        _view.ShowJoinPreview(true, detailMsg);
    }

    private async Task HandleConfirmJoin()
    {
        _view.ShowJoinPreview(false);
        _view.SetStatus($"Joining Room {_pendingRoomCode}...");

        _screenManager.Show("MatchmakingScreen");
        var mmScreen = _screenManager.GetCurrentScreen<MatchmakingScreen>();
        if (mmScreen != null) mmScreen.Init(_pendingStadiumName, _pendingStadiumIcon, _pendingRoomCode);

        await _matchmakingService.FindPrivateMatch(_pendingRoomCode, _pendingStake, _pendingOvers, _pendingWickets); 
    }

    private bool ValidateRequirements(int stake, int minLevel)
    {
        if (_userData.coins < stake)
        {
            _view.SetStatus($"Insufficient Coins! Need {stake}.");
            return false;
        }
        if (_userData.level < minLevel)
        {
            _view.SetStatus($"Level {minLevel} Required!");
            return false;
        }
        return true;
    }

}
