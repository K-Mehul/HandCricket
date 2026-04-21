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
                    CoachDialogUI.Instance?.ShowMessage("Welcome to Hand Cricket! I've added some starting tokens and XP to help you begin your journey.", true);
                }
                else
                {
                    CoachDialogUI.Instance?.ShowMessage("I'm your Team Coach. I'll guide you step-by-step so you can quickly learn how to play and win matches!!", true);
                }
                SpotlightUI.Instance?.ShowFullOverlay();
                CoachDialogUI.Instance?.Reposition(null);
                break;

            case TutorialState.MainMenu_Play:
                CoachDialogUI.Instance?.ShowMessage("Tap on the PLAY button to start a match. This is where your journey begins.", false);
                SpotlightUI.Instance?.ShowFullOverlay(); // Block until speaking finishes
                break;

            case TutorialState.Lobby_Global:
                CoachDialogUI.Instance?.ShowMessage("Select GLOBAL MATCH to join a practice game and learn the basics.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Stadium_Pick:
                CoachDialogUI.Instance?.ShowMessage("Choose the Gully Cricket stadium. Each match takes place in a stadium like this.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Matchmaking_Wait:
                CoachDialogUI.Instance?.ShowMessage("Finding you an opponent... Get ready to play your first Hand Cricket match!", true);
                SpotlightUI.Instance?.ShowFullOverlay();
                CoachDialogUI.Instance?.Reposition(null);
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
                    CoachDialogUI.Instance?.ShowMessage("Here's your opponent! In Hand Cricket, both players choose numbers each turn.", true);
                }
                else if(_currentSubStep == 1)
                {
                    CoachDialogUI.Instance?.ShowMessage("In a match, your goal is simple: score more runs than your opponent to win.", true);
                }
                else
                {
                    CoachDialogUI.Instance?.ShowMessage("You pick 4 and opponent picks 2 → You score 4 runs. If both pick 4 → OUT!", true);
                }
                SpotlightUI.Instance?.ShowFullOverlay();
                CoachDialogUI.Instance?.Reposition(null);
                break;

            case TutorialState.Match_Toss:
                CoachDialogUI.Instance?.ShowMessage("It's time for the toss! Pick Heads or Tails. The winner decides who bats or bowls first.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Match_Decision:
                CoachDialogUI.Instance?.ShowMessage("You won the toss! Choose to BAT first. Batting first means you set the target score.", false);
                SpotlightUI.Instance?.ShowFullOverlay();
                break;

            case TutorialState.Match_Gameplay_Batting:
                if (!_battingExplained)
                {
                    CoachDialogUI.Instance?.ShowMessage("BATTING: Choose a number from 1 to 6. If your number is different from the bowler's, you score those runs. But if both numbers match, you're OUT!", false);
                    SpotlightUI.Instance?.ShowFullOverlay();
                    _battingExplained = true;
                }
                break;

            case TutorialState.Match_Gameplay_Bowling:
                if (!_bowlingExplained)
                {
                    CoachDialogUI.Instance?.ShowMessage("BOWLING: Choose a number from 1 to 6. If your number matches the batsman's number, you take a WICKET and end their turn!", false);
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
            CoachDialogUI.Instance?.Reposition(target);
        }
        else
        {
            // If target not yet registered (e.g. screen loading), it will update once RegisterTarget is called.
            SpotlightUI.Instance?.Hide();
            CoachDialogUI.Instance?.Reposition(null);
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
                    if (_currentSubStep >= 3) OnMatchIntroFinished?.Invoke();
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
