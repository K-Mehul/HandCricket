using UnityEngine;
using System;
using DG.Tweening;

public class GamePresenter
{
    private readonly GameView _view;
    private ClientMatchHandler _matchHandler;

    private float _turnAnimationEndTime = 0f;
    private bool _amIBatting = false;

    // Delayed Payload Storage
    private ClientMatchHandler.InningsChangePayload _pendingInningsPayload;
    private ClientMatchHandler.MatchSummaryPayload _pendingSummaryPayload;
    private bool _isTransitioning = false;
    private bool _isSimulationRunning = false;
    private bool _isTurnDecided = false;
    private int _maxOvers = 0;

    // Timer Logic
    private float _currentTime;
    private Tween _timerTween;
    private const float TOSS_DURATION = 10f;
    private const float DECISION_DURATION = 10f;
    private const float TURN_DURATION = 5f;

    public GamePresenter(GameView view)
    {
        _view = view;
        _matchHandler = ClientMatchHandler.Instance;
    }

    public void Subscribe()
    {
        // Ensure we have a match handler if it was null at construction
        _matchHandler = ClientMatchHandler.Instance;

        // Unsubscribe first to avoid duplicates
        //Cleanup();


        if (_matchHandler == null)
        {
            Debug.LogWarning("GamePresenter: Cannot subscribe because ClientMatchHandler.Instance is null.");
            return;
        }

        Debug.Log("Game Presenter : Subscribe Game Presenter");
        // Network Events
        _matchHandler.OnTossStarted += HandleTossStarted;
        _matchHandler.OnTossResult += HandleTossResult;
        _matchHandler.OnGameStarted += HandleGameStarted;
        _matchHandler.OnTurnResult += HandleTurnResult;
        _matchHandler.OnInningsChanged += HandleInningsChanged;
        _matchHandler.OnGameOver += HandleGameOver;
        _matchHandler.OnMatchSummary += HandleMatchSummary;

        // View Events
        _view.OnDecisionMade += (decision) => {
            if (_isTurnDecided) return;
            _isTurnDecided = true;

            StopTimer();
            _view.ShowYourTurn(false);
            _view.SetInputInteractivity(false);
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                TutorialManager.Instance.OnActionCompleted("Decision");
                if (TutorialMatchSimulation.Instance != null)
                {
                    TutorialMatchSimulation.Instance.OnDecisionMade(decision);
                }
            }
            else
            {
                _matchHandler.SendDecision(decision);
            }
        };
        _view.OnNumberPicked += (num) => {
            if (_isTurnDecided) return;
            _isTurnDecided = true;

            StopTimer();
            _view.ShowYourTurn(false);
            _view.SetInputInteractivity(false);
            
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                TutorialManager.Instance.OnActionCompleted("Turn");
                if (TutorialMatchSimulation.Instance != null)
                {
                    TutorialMatchSimulation.Instance.OnNumberPicked(num);
                }
            }
            else
            {
                _matchHandler.SendTurnInput(num);
            }
        };
        _view.OnTossCalled += (call) => {
            if (_isTurnDecided) return;
            _isTurnDecided = true;

            StopTimer();
            _view.ShowYourTurn(false);
            _view.SetInputInteractivity(false);
            
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                TutorialManager.Instance.OnActionCompleted("Toss");
                if (TutorialMatchSimulation.Instance != null)
                {
                    TutorialMatchSimulation.Instance.OnTossCalled(call);
                }
            }
            else
            {
                _matchHandler.SendTossSelection(call);
            }
        };
        _view.OnBackToLobby += () => {
            Cleanup();
            _matchHandler.Reset();
            _view.SetStadiumActive(false);
            _view.ResetAnimators();
            UIScreenManager.Instance.Show("LobbyScreen");
        };
    }

    public void Cleanup()
    {
        StopTimer();
        // Ironclad Cleanup: Kill all background timers and delayed logic calls 
        // to prevent "Zombie Timers" from Match 1 affecting Match 2.
        DOTween.KillAll(); 

        if (_matchHandler == null) return;
        
        _matchHandler.OnTossStarted -= HandleTossStarted;
        _matchHandler.OnTossResult -= HandleTossResult;
        _matchHandler.OnGameStarted -= HandleGameStarted;
        _matchHandler.OnTurnResult -= HandleTurnResult;
        _matchHandler.OnInningsChanged -= HandleInningsChanged;
        _matchHandler.OnGameOver -= HandleGameOver;
        _matchHandler.OnMatchSummary -= HandleMatchSummary;

        //_matchHandler.Reset();
        Debug.Log("[GamePresenter] Match Handler Reset called during Cleanup.");
    }

    public void Init(bool isMeTossingFallback)
    {
        Debug.Log("[GamePresenter] Initializing fresh Match UI.");
        _matchHandler = ClientMatchHandler.Instance;

        // Forceful speed reset in case previous match ended during slow-mo
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        //Cleanup();
        
        // IRON RESET: Clear any visual state from Match #1
        if (_view != null) 
        {
            _view.ShowPanel("Wait");
        }

        _isTransitioning = false;
        _isSimulationRunning = false;
        _isTurnDecided = false;

        // CATCH-UP LOGIC: Jump to the latest known state
        // 1. If Game already started (CATCH-UP LOGIC)
        // If this fires on a fresh match, it means stale data from a previous session leaked.
        if (_matchHandler.LastGameStart != null && !string.IsNullOrEmpty(_matchHandler.LastGameStart.batting_player))
        {
            Debug.Log($"[GamePresenter] Catch-up: Game already started (Batting: {_matchHandler.LastGameStart.batting_player}). Match ID: {(_matchHandler.CurrentMatch != null ? _matchHandler.CurrentMatch.Id : "UNKNOWN")}");
            HandleGameStarted(_matchHandler.LastGameStart.batting_player,
                             (_matchHandler.LastGameStart.batting_player == NakamaSessionManager.Session.UserId));
            return;
        }

        //// 2. If Toss Result is already known
        if (_matchHandler.LastTossResult != null && !string.IsNullOrEmpty(_matchHandler.LastTossResult.toss_winner))
        {
            Debug.Log($"[GamePresenter] Catch-up: Toss Result already known (Winner: {_matchHandler.LastTossResult.toss_winner}). Match ID: {(_matchHandler.CurrentMatch != null ? _matchHandler.CurrentMatch.Id : "UNKNOWN")}");
            HandleTossResult(_matchHandler.LastTossResult.toss_winner, _matchHandler.LastTossResult.outcome);
            return;
        }

        //// 3. If Toss just started or is active
        if (_matchHandler.LastTossStart != null && !string.IsNullOrEmpty(_matchHandler.LastTossStart.initiator_id))
        {
            Debug.Log($"[GamePresenter] Catch-up: Toss active (Initiator: {_matchHandler.LastTossStart.initiator_id}). Match ID: {(_matchHandler.CurrentMatch != null ? _matchHandler.CurrentMatch.Id : "UNKNOWN")}");
            HandleTossStarted(_matchHandler.LastTossStart.initiator_id);
            return;
        }

        // Standard Fallback from parameter
        Debug.Log("ToSS");
        _view.ShowPanel("Toss");
        _view.UpdateNames(_matchHandler.MyUsername, _matchHandler.OpponentUsername);
        
        if (isMeTossingFallback)
        {
            HandleTossStarted(NakamaSessionManager.Session.UserId);
        }
        else
        {
            _view.SetTossInteractable(false, "Opponent calling toss...");
            _view.ShowYourTurn(false);
            _view.UpdateTimer(0, false);
        }
    }

    private void HandleTossStarted(string initiatorId)
    {
        Debug.Log("Handle Toss Started");
        // IDEMPOTENCY GUARD: If we are already on the Toss screen and haven't made a choice yet,
        // ignore redundant "Start" messages (e.g., from catch-up resyncs).
        if (_view != null && _view.IsPanelActive("Toss") && !_isTurnDecided)
        {
            Debug.Log("[GamePresenter] Ignoring redundant TossStarted message.");
            return;
        }

        // Debounce: If already in Decision or Game, ignore toss starts
        if (_view != null && (_view.IsPanelActive("Decision") || _view.IsPanelActive("Game") || _view.IsPanelActive("Result"))) return;

        bool isMeTossing = (initiatorId == NakamaSessionManager.Session.UserId);

        Debug.Log("[TOSS      SCREEN       OPENNNNNNNNNNNN]");

        _view.ShowPanel("Toss");
        _view.UpdateNames(_matchHandler.MyUsername, _matchHandler.OpponentUsername);
        
        if (isMeTossing)
        {
            _view.SetTossInteractable(true, "CALL THE TOSS!");
            _view.ShowYourTurn(true);
            _isTurnDecided = false;

            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                TutorialManager.Instance.RegisterTarget("TossButtons", _view.tossButtonsRect);
                TutorialManager.Instance.SetState(TutorialManager.TutorialState.Match_Toss);
                // In tutorial, we DON'T start the timer, or we give a massive duration
                _view.UpdateTimer(0, false); 
            }
            else
            {
                StartTimer(TOSS_DURATION, () => {
                    _view.SetTossInteractable(false, "Time's up!");
                    if (_isTurnDecided) return;
                    _isTurnDecided = true;

                    string pick = UnityEngine.Random.value > 0.5f ? "HEADS" : "TAILS";
                    _view.FlashAutoSelection(pick);
                    
                    DOVirtual.DelayedCall(1.2f, () => {
                       _matchHandler.SendTossSelection(pick);
                    });
                });
            }
        }
        else
        {
            _view.SetTossInteractable(false, "Opponent calling toss...");
            _view.ShowYourTurn(false);
            _view.UpdateTimer(0, false);
        }
    }

    private void HandleTossResult(string winnerId, string outcome)
    {
        Debug.Log("HANDLE TOSSSSSS   RESULTTTT");

        // IDEMPOTENCY GUARD: If we are already past the Toss screen, ignore redundant/resync calls
        if (_view != null && (_view.IsPanelActive("Decision") || _view.IsPanelActive("Game") || _view.IsPanelActive("Result"))) 
        {
            Debug.Log("[GamePresenter] Ignoring redundant TossResult message.");
            return;
        }

        bool won = (winnerId == NakamaSessionManager.Session.UserId);
        string resultMsg = $"Outcome: {outcome}. " + (won ? "YOU WON!" : "OPPONENT WON!");
        
        _view.SetTossInteractable(false, resultMsg);
        _view.ShowPhaseBanner(won ? "YOU WON! CHOOSE BAT/BOWL" : "OPPONENT CHOOSING...", 2.0f);
        _view.ShowYourTurn(false);
        StopTimer();

        if (won)
        {
            // Small delay before showing decision panel
            DOVirtual.DelayedCall(1.2f, () => {
                _view.ShowPanel("Decision");
                _view.ShowYourTurn(true);
                _isTurnDecided = false;
                
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
                {
                    TutorialManager.Instance.RegisterTarget("DecisionButtons", _view.decisionButtonsRect);
                    TutorialManager.Instance.SetState(TutorialManager.TutorialState.Match_Decision);
                    _view.UpdateTimer(0, false);
                }
                else
                {
                    StartTimer(DECISION_DURATION, () => {
                        _view.SetInputInteractivity(false);
                        if (_isTurnDecided) return;
                        _isTurnDecided = true;

                        string pick = UnityEngine.Random.value > 0.5f ? "BAT" : "BOWL";
                        _view.FlashAutoSelection(pick);
                        
                        DOVirtual.DelayedCall(1.2f, () => {
                            _matchHandler.SendDecision(pick);
                        });
                    });
                }
            });
        }
    }

    private void HandleGameStarted(string battingPlayerId, bool amBatting)
    {
        Debug.Log("HANDLE GAME STARTED");

        _amIBatting = amBatting;
        _view.ShowPanel("Game");
        _view.UpdateRole(_amIBatting ? "BATTING" : "BOWLING");
        _view.SetUniforms(_amIBatting);
        _view.ClearBallHistory();
            
        // SEQUENTIAL BANNER KICKOFF
        _view.ShowPhaseBanner("MATCH START!", 1.5f, () => {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                // In tutorial, we pause the sequence for the Opponent Intro
                TutorialManager.Instance.SetState(TutorialManager.TutorialState.Match_Intro);
                
                Action onIntroComplete = null;
                onIntroComplete = () => {
                    TutorialManager.Instance.OnMatchIntroFinished -= onIntroComplete;
                    ShowRoleAndStart();
                };
                TutorialManager.Instance.OnMatchIntroFinished += onIntroComplete;
            }
            else
            {
                // Normal flow
                ShowRoleAndStart();
            }
        });
        
        _maxOvers = _matchHandler != null ? _matchHandler.MaxOvers : 0;
        
        // Initial Names for Footer
        string myName = _matchHandler.MyUsername;
        string oppName = _matchHandler.OpponentUsername;
        _view.UpdateNames(_amIBatting ? myName : oppName, _amIBatting ? oppName : myName);
        
        _view.UpdateMatchInfo(0, _maxOvers);
        _view.UpdateScoreDisplay("SCORE: 0", "Match Starting...", 0, _maxOvers);

        _view.SetStadiumActive(true);
        _view.ResetAnimators();
    }

    private void ShowRoleAndStart()
    {
        // Small buffer before Role Banner
        DOVirtual.DelayedCall(0.5f, () => {
            _view.ShowRoleBanner(_amIBatting ? "BATTING" : "BOWLING");

            DOVirtual.DelayedCall(1.5f, () => {
                PrepareTurn();
            });
        });
    }

    private void PrepareTurn()
    {
        // PHASE GUARD: Never start a turn timer if we aren't actually on the Gameplay screen
        if (_view == null || !_view.IsPanelActive("Game"))
        {
            Debug.LogWarning("[GamePresenter] PrepareTurn blocked: Gameplay panel is not active.");
            return;
        }

        _isTurnDecided = false;
        _view.SetInputInteractivity(true);
        _view.ShowYourTurn(true);

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
        {
            TutorialManager.Instance.RegisterTarget("NumberInputs", _view.numberButtonsRect);
            TutorialManager.Instance.SetState(_amIBatting ? TutorialManager.TutorialState.Match_Gameplay_Batting : TutorialManager.TutorialState.Match_Gameplay_Bowling);
            _view.UpdateTimer(0, false);
        }
        else
        {
            StartTimer(TURN_DURATION, () => {
                _view.SetInputInteractivity(false);
                if (_isTurnDecided) return;
                _isTurnDecided = true;

                int pick = UnityEngine.Random.Range(1, 7);
                _view.FlashAutoSelection(pick.ToString());
                
                DOVirtual.DelayedCall(1.2f, () => {
                    _matchHandler.SendTurnInput(pick);
                });
            });
        }
    }

    private void HandleTurnResult(ClientMatchHandler.TurnResultPayload data)
    {
        string eventType = string.IsNullOrEmpty(data.@event) ? data.event_type : data.@event;
        string statusMsg = "";

        string batLabel = _amIBatting ? "You" : "Opponent";
        string bowlLabel = _amIBatting ? "Opponent" : "You";

        if (eventType == "WICKET")
        {
            statusMsg = $"WICKET! ({batLabel}: {data.bat_input} vs {bowlLabel}: {data.bowl_input})";
            _turnAnimationEndTime = Time.time + 1.8f;
        }
        else
        {
            statusMsg = $"Last Ball: {batLabel} hit {data.bat_input} ({bowlLabel}: {data.bowl_input})";
            _turnAnimationEndTime = Time.time + 1.8f;
        }

        string scoreMsg = $"Score: {data.batsman_score}/{data.batsman_wickets} / {_matchHandler.MaxWickets} Wkts ({data.balls} balls)";
        if (data.target > 0) scoreMsg += $"\nTarget: {data.target}";

        // Stop timer and hide indicator immediately
        _view.ShowYourTurn(false);
        StopTimer();
        _view.SetInputInteractivity(false);

        // Trigger the 3D Simulation
        _isSimulationRunning = true;
        _view.PlayPerfectSimulation(data.bat_input, data.bowl_input, () => {
            // Update score once simulation finishes its movement
            int displayBalls = data.balls; 
            _view.UpdateBallHistory(eventType == "WICKET" ? "W" : data.bat_input.ToString());

            // FIX: Increment displayBalls for UI only (to show 0.1 after 1st ball, not 0.0)
            int finalDisplayBalls = displayBalls + 1;
            string myScoreMsg = $"SCORE: {data.batsman_score}/{data.batsman_wickets}";
            _view.UpdateScoreDisplay(myScoreMsg, statusMsg, finalDisplayBalls, _maxOvers, data.target);
            
            // Set Names for current batsman/bowler
            string myName = _matchHandler.MyUsername;
            string oppName = _matchHandler.OpponentUsername;
            _view.UpdateNames(_amIBatting ? myName : oppName, _amIBatting ? oppName : myName);

            UpdateAtmosphere(displayBalls, data.target, data.batsman_score);

            // Play Banners and only then mark simulation as finished
            Action finishSim = () => {
                _isSimulationRunning = false;
                if (!_isTransitioning) PrepareTurn();
            };

            if (eventType == "WICKET") _view.PlayScoringAnimation("WICKET", finishSim);
            else if (data.bat_input == 4) _view.PlayScoringAnimation("FOUR", finishSim);
            else if (data.bat_input == 6) _view.PlayScoringAnimation("SIX", finishSim);
            else finishSim();
        });
    }

    private void UpdateAtmosphere(int balls, int target, int score)
    {
        float targetVolume = 0.4f; // Calm/Default
        int totalBalls = _maxOvers * 6;
        int ballsRemaining = totalBalls - balls;

        // HIGH STAKES: Last Over
        if (ballsRemaining > 0 && ballsRemaining <= 6)
        {
            targetVolume = 0.75f;
        }

        // HIGH STAKES: Close Run Chase (assume 2nd innings if target > 0)
        if (target > 0)
        {
            int runsNeeded = target - score;
            if (runsNeeded > 0 && runsNeeded <= 12)
            {
                // Intensify if it's both last over AND close score
                targetVolume = (ballsRemaining <= 6) ? 0.95f : 0.75f;
            }
        }

        _view.SetCrowdVolume(targetVolume, 2.5f);
    }

    private void HandleInningsChanged(ClientMatchHandler.InningsChangePayload data)
    {
        _isTransitioning = true;
        _view.SetInputInteractivity(false);
        _pendingInningsPayload = data;
        
        // Wait for ALL simulation and banners (Scoring or otherwise) to finish
        float checkInterval = 0.2f;
        Action checkAndSwitch = null;
        checkAndSwitch = () => {
            if (!_isSimulationRunning && !_view.IsBannerPlaying) 
            {
                SwitchInnings();
            }
            else
            {
                DOVirtual.DelayedCall(checkInterval, () => checkAndSwitch());
            }
        };
        
        checkAndSwitch();
    }

    private void SwitchInnings()
    {
        Debug.Log("SWUTCH INNINGS");
        _isTransitioning = false;
        _view.ShowPanel("Game");
        
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        _view.ClearBallHistory();
        _amIBatting = (_pendingInningsPayload.batting_player_id == NakamaSessionManager.Session.UserId);
        _view.UpdateRole(_amIBatting ? "BATTING" : "BOWLING");
        _view.SetUniforms(_amIBatting);

        // SEQUENTIAL BANNER KICKOFF FOR 2ND INNINGS
        _view.ShowPhaseBanner($"TARGET: {_pendingInningsPayload.target}", 2.0f, () => {
            DOVirtual.DelayedCall(0.5f, () => {
                _view.ShowRoleBanner(_amIBatting ? "BATTING" : "BOWLING");
                
                DOVirtual.DelayedCall(1.5f, () => {
                    PrepareTurn();
                });
            });
        });
        
        string myName = _matchHandler.MyUsername;
        string oppName = _matchHandler.OpponentUsername;
        _view.UpdateNames(_amIBatting ? myName : oppName, _amIBatting ? oppName : myName);

        _view.UpdateScoreDisplay($"SCORE: 0/0", _pendingInningsPayload.message, 0, _maxOvers, _pendingInningsPayload.target);
        
        UpdateAtmosphere(0, _pendingInningsPayload.target, 0);
    }

    private void HandleGameOver(ClientMatchHandler.GameOverPayload data)
    {
        _isTransitioning = true;
        _view.SetInputInteractivity(false);
        
        // Wait for final animation to finish before updating score or stopping stadium
        float checkInterval = 0.2f;
        Action checkFinish = null;
        checkFinish = () => {
            if (!_isSimulationRunning && !_view.IsBannerPlaying) {
                bool iWon = (data.winner == NakamaSessionManager.Session.Username);
                if (data.winner == "DRAW") iWon = false;

                string finalMsg = (data.winner == "DRAW") ? "DRAW!" : (iWon ? "VICTORY!" : "DEFEAT!");
                _view.UpdateScoreDisplay("", $"{finalMsg}\nReason: {data.reason}");
                
                _view.ShowYourTurn(false);
                StopTimer();
                _view.SetStadiumActive(false);
                _view.ResetAnimators();
            } else {
                DOVirtual.DelayedCall(checkInterval, () => checkFinish());
            }
        };
        checkFinish();
    }

    private void HandleMatchSummary(ClientMatchHandler.MatchSummaryPayload data)
    {
        _pendingSummaryPayload = data;
        
        // Wait for ALL animations and banners to finish before showing results
        float checkInterval = 0.2f;
        Action checkAndShowResult = null;
        checkAndShowResult = () => {
            if (!_isSimulationRunning && !_view.IsBannerPlaying) {
                bool iWon = (data.winner == _matchHandler.MyUsername);
                string title = iWon ? "VICTORY!" : "DEFEAT";
                if (data.winner == "DRAW") title = "DRAW";

                string details = $"Earnings:\nCoins: +{data.coins_earned}\nXP: +{data.xp_earned}";
                StopTimer();
                _view.ShowResult(title, details, data.leveled_up, data.new_level);
            } else {
                DOVirtual.DelayedCall(checkInterval, () => checkAndShowResult());
            }
        };
        checkAndShowResult();
    }

    // --- Timer Handling ---

    private void StartTimer(float duration, Action onTimeout)
    {
        StopTimer();
        _currentTime = duration;
        _view.UpdateTimer(_currentTime, true);

        _timerTween = DOTween.To(() => _currentTime, x => {
            _currentTime = x;
            _view.UpdateTimer(_currentTime, true);
        }, 0f, duration).SetEase(Ease.Linear).OnComplete(() => {
            _view.UpdateTimer(0, false);
            _view.ShowYourTurn(false);
            onTimeout?.Invoke();
        });
    }

    private void StopTimer()
    {
        if (_timerTween != null)
        {
            _timerTween.Kill();
            _timerTween = null;
        }
        _view.UpdateTimer(0, false);
    }
}
