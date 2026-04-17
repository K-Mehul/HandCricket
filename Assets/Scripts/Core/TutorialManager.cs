using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using Nakama.TinyJson;
using System.Linq;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public enum TutorialState
    {
        None,
        Welcome,
        MainMenu_Play,
        Lobby_Global,
        Stadium_Pick,
        Matchmaking_Wait,
        Match_Intro,
        Match_Toss,
        Match_Decision,
        Match_Gameplay_Batting,
        Match_Gameplay_Bowling,
        Match_Complete
    }

    public TutorialState CurrentState { get; private set; } = TutorialState.None;
    private bool _isTutorialSessionActive = false; // Persistent flag for match session
    private bool _isSessionCompleted = false;      // Persistent flag for post-match
    public bool IsTutorialActive => (_isTutorialSessionActive || (CurrentState != TutorialState.None && CurrentState != TutorialState.Match_Complete)) && !_isSessionCompleted;
    public event Action OnMatchIntroFinished;

    private Dictionary<string, RectTransform> _targetRegistry = new Dictionary<string, RectTransform>();
    private int _currentSubStep = 0;
    private bool _battingExplained = false;
    private bool _bowlingExplained = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (CoachDialogUI.Instance != null)
        {
            CoachDialogUI.Instance.OnMessageComplete += () => {
                // Show the highlight only after coach finished talking
                UpdateHighlightOnly();

                // Auto-hide coach after a delay if in gameplay state
                if (CurrentState == TutorialState.Match_Gameplay_Batting || CurrentState == TutorialState.Match_Gameplay_Bowling)
                {
                    StartCoroutine(HideCoachDelayed(3.5f));
                }
            };
        }
    }

    private IEnumerator<WaitForSeconds> HideCoachDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Only hide if we are still in the same state
        if (CurrentState == TutorialState.Match_Gameplay_Batting || CurrentState == TutorialState.Match_Gameplay_Bowling)
        {
            CoachDialogUI.Instance?.Hide();
        }
    }

    public void RegisterTarget(string key, RectTransform target)
    {
        _targetRegistry[key] = target;
        // If we are waiting for this specific target, update spotlight immediately
        UpdateSpotlightForCurrentState();
    }

    public void StartTutorial()
    {
        SetState(TutorialState.Welcome);
    }

    public void SetState(TutorialState newState)
    {
        Debug.Log($"[TutorialManager] State Change: {CurrentState} -> {newState}");
        CurrentState = newState;
        _currentSubStep = 0;
        
        // When state change, stop any current spotlight so we don't show old highlight while new text is typing
        SpotlightUI.Instance?.Hide();
        
        UpdateSpotlightForCurrentState();
    }

    private void UpdateSpotlightForCurrentState()
    {
        if (!IsTutorialActive)
        {
            SpotlightUI.Instance?.Hide();
            CoachDialogUI.Instance?.Hide();
            return;
        }

        switch (CurrentState)
        {
            case TutorialState.Welcome:
                if (_currentSubStep == 0)
                {
                    CoachDialogUI.Instance?.ShowMessage("Welcome to Hand Cricket! As a special bonus, I've credited some starting tokens and XP to your account!", true);
                }
                else
                {
                    CoachDialogUI.Instance?.ShowMessage("I'm your Team Coach. I'll guide you through your first match and help you master the game!", true);
                }
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.MainMenu_Play:
                CoachDialogUI.Instance?.ShowMessage("First, click on the PLAY button to enter the match lobby.", false);
                SpotlightUI.Instance?.ShowFullOverlay(); // Block until speaking finishes
                break;

            case TutorialState.Lobby_Global:
                CoachDialogUI.Instance?.ShowMessage("Now, tap on GLOBAL MATCH to find a practice session.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Stadium_Pick:
                CoachDialogUI.Instance?.ShowMessage("Excellent! Now, select the Gully Cricket stadium to start your journey.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Matchmaking_Wait:
                CoachDialogUI.Instance?.ShowMessage("Finding you a practice match. Just a moment...", true);
                SpotlightUI.Instance?.ShowFullOverlay();
                // Start the local fake match simulation
                if (TutorialMatchSimulation.Instance != null)
                {
                    _isTutorialSessionActive = true; // LOCK ON
                    TutorialMatchSimulation.Instance.StartSimulation();
                }
                break;

            case TutorialState.Match_Intro:
                if (_currentSubStep == 0)
                {
                    CoachDialogUI.Instance?.ShowMessage("Here's your opponent! Today we play a standard match.", true);
                }
                else
                {
                    CoachDialogUI.Instance?.ShowMessage("In a standard match, you need to score more runs than your opponent to win. Good luck!", true);
                }
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Match_Toss:
                CoachDialogUI.Instance?.ShowMessage("It's time for the TOSS! Pick Heads or Tails. Winner chooses to Bat or Bowl first.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Match_Decision:
                CoachDialogUI.Instance?.ShowMessage("You won the toss! Choose to BAT first to set a high score.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Match_Gameplay_Batting:
                if (!_battingExplained)
                {
                    CoachDialogUI.Instance?.ShowMessage("BATTING: Pick a number. If it's different from the bowler, you get those runs!", false);
                    SpotlightUI.Instance?.ShowFullOverlay();
                    _battingExplained = true;
                }
                break;

            case TutorialState.Match_Gameplay_Bowling:
                if (!_bowlingExplained)
                {
                    CoachDialogUI.Instance?.ShowMessage("BOWLING: Try to guess the batsman's number. If you match, it's a WICKET!", false);
                    SpotlightUI.Instance?.ShowFullOverlay();
                    _bowlingExplained = true;
                }
                break;
        }
    }

    private void UpdateHighlightOnly()
    {
        switch (CurrentState)
        {
            case TutorialState.MainMenu_Play: TryHighlight("PlayButton"); break;
            case TutorialState.Lobby_Global: TryHighlight("GlobalButton"); break;
            case TutorialState.Stadium_Pick: TryHighlight("FirstStadium"); break;
            case TutorialState.Match_Toss: TryHighlight("TossButtons"); break;
            case TutorialState.Match_Decision: TryHighlight("DecisionButtons"); break;
            case TutorialState.Match_Gameplay_Batting: TryHighlight("NumberInputs"); break;
            case TutorialState.Match_Gameplay_Bowling: TryHighlight("NumberInputs"); break;
        }
    }

    private void TryHighlight(string key)
    {
        if (_targetRegistry.TryGetValue(key, out RectTransform target))
        {
            SpotlightUI.Instance?.Show(target);
        }
        else
        {
            // If target not yet registered (e.g. screen loading), it will update once RegisterTarget is called.
            SpotlightUI.Instance?.Hide();
        }
    }

    public void OnActionCompleted(string actionId)
    {
        // Simple logic to advance tutorial based on actions
        switch (CurrentState)
        {
            case TutorialState.Welcome:
                if (actionId == "Continue")
                {
                    _currentSubStep++;
                    if (_currentSubStep >= 2) SetState(TutorialState.MainMenu_Play);
                    else UpdateSpotlightForCurrentState();
                }
                break;
            case TutorialState.MainMenu_Play:
                if (actionId == "Play") SetState(TutorialState.Lobby_Global);
                break;
            case TutorialState.Lobby_Global:
                if (actionId == "Global") SetState(TutorialState.Stadium_Pick);
                break;
            case TutorialState.Stadium_Pick:
                if (actionId == "Stadium") SetState(TutorialState.Matchmaking_Wait);
                break;
            case TutorialState.Match_Intro:
                if (actionId == "Continue") 
                {
                    _currentSubStep++;
                    if (_currentSubStep >= 2) OnMatchIntroFinished?.Invoke();
                    else UpdateSpotlightForCurrentState();
                }
                break;
            case TutorialState.Match_Toss:
                if (actionId == "Toss") SpotlightUI.Instance?.Hide(); // Hide highlight when clicked
                break;
            case TutorialState.Match_Decision:
                if (actionId == "Decision") SpotlightUI.Instance?.Hide();
                break;
            case TutorialState.Match_Gameplay_Batting:
            case TutorialState.Match_Gameplay_Bowling:
                if (actionId == "Turn") SpotlightUI.Instance?.Hide();
                break;
        }
    }

    public async void CompleteTutorial()
    {
        SetState(TutorialState.Match_Complete);
        _isTutorialSessionActive = false;
        _isSessionCompleted = true; // LOCK ON for this session
        
        if (ClientMatchHandler.Instance != null)
        {
            ClientMatchHandler.Instance.IsLocalSimulation = false;
        }
        
        // Save local fallback immediately
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // 1. Grant Rewards via RPC
        await GrantTutorialRewards();
    }

    private async Task GrantTutorialRewards()
    {
        if (NakamaSessionManager.Session == null) return;

        Debug.Log("[TutorialManager] Requesting Database Rewards via RPC...");
        try
        {
            // Call the server-side RPC to grant coins/xp
            await NakamaService.Client.RpcAsync(NakamaSessionManager.Session, "complete_tutorial", "{}");
            Debug.Log("[TutorialManager] Tutorial Rewards Granted successfully.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TutorialManager] Failed to grant rewards (maybe already claimed?): {e.Message}");
        }
    }


    public async Task<bool> CheckTutorialStatusFromBackend()
    {
        if (_isSessionCompleted) return true;
        if (NakamaSessionManager.Session == null) return PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

        try
        {
            var result = await NakamaService.Client.ReadStorageObjectsAsync(NakamaSessionManager.Session, new[] {
                new StorageObjectId { Collection = "metadata", Key = "tutorial_status", UserId = NakamaSessionManager.Session.UserId }
            });

            if (result.Objects != null && result.Objects.Any())
            {
                var data = result.Objects.First().Value.FromJson<Dictionary<string, bool>>();
                bool completed = data.ContainsKey("completed") && data["completed"];
                
                if (completed)
                {
                    PlayerPrefs.SetInt("TutorialCompleted", 1);
                    return true;
                }
            }
            
            // If we're here, either no object exists or completed is false
            // This means we should CLEAR the local flag to allow re-triggering (e.g. after DB wipe)
            if (PlayerPrefs.HasKey("TutorialCompleted"))
            {
                Debug.Log("[TutorialManager] Tutorial not found in backend. Clearing local completion flag.");
                PlayerPrefs.DeleteKey("TutorialCompleted");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TutorialManager] Could not fetch tutorial status: {e.Message}");
        }

        return false;
    }
}
