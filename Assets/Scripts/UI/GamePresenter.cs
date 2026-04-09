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
        if (_matchHandler == null) _matchHandler = ClientMatchHandler.Instance;
        
        // Unsubscribe first to avoid duplicates
        Cleanup();

        if (_matchHandler == null)
        {
            Debug.LogWarning("GamePresenter: Cannot subscribe because ClientMatchHandler.Instance is null.");
            return;
        }

        // Network Events
        _matchHandler.OnTossResult += HandleTossResult;
        _matchHandler.OnGameStarted += HandleGameStarted;
        _matchHandler.OnTurnResult += HandleTurnResult;
        _matchHandler.OnInningsChanged += HandleInningsChanged;
        _matchHandler.OnGameOver += HandleGameOver;
        _matchHandler.OnMatchSummary += HandleMatchSummary;

        // View Events
        _view.OnDecisionMade += (decision) => {
            StopTimer();
            _view.ShowYourTurn(false);
            _matchHandler.SendDecision(decision);
        };
        _view.OnNumberPicked += (num) => {
            StopTimer();
            _view.ShowYourTurn(false);
            _view.SetInputInteractivity(false);
            _matchHandler.SendTurnInput(num);
        };
        _view.OnTossCalled += (call) => {
            StopTimer();
            _view.ShowYourTurn(false);
            _matchHandler.SendTossSelection(call);
        };
        _view.OnBackToLobby += () => {
            _view.SetStadiumActive(false);
            _view.ResetAnimators();
            UIScreenManager.Instance.Show("LobbyScreen");
        };
    }

    public void Cleanup()
    {
        if (_matchHandler == null) return;
        
        _matchHandler.OnTossResult -= HandleTossResult;
        _matchHandler.OnGameStarted -= HandleGameStarted;
        _matchHandler.OnTurnResult -= HandleTurnResult;
        _matchHandler.OnInningsChanged -= HandleInningsChanged;
        _matchHandler.OnGameOver -= HandleGameOver;
        _matchHandler.OnMatchSummary -= HandleMatchSummary;
    }

    public void Init(bool isMeTossing)
    {
        // Forceful speed reset in case previous match ended during slow-mo
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        _isTransitioning = false;
        _view.ShowPanel("Toss");
        _view.UpdateNames(_matchHandler.MyUsername, _matchHandler.OpponentUsername);
        
        if (isMeTossing)
        {
            _view.SetTossInteractable(true, "CALL THE TOSS!");
            _view.ShowYourTurn(true);
            StartTimer(TOSS_DURATION, () => {
                string pick = UnityEngine.Random.value > 0.5f ? "HEADS" : "TAILS";
                _view.FlashAutoSelection(pick);
                
                DOVirtual.DelayedCall(1.2f, () => {
                   _matchHandler.SendTossSelection(pick);
                });
            });
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
                StartTimer(DECISION_DURATION, () => {
                    string pick = UnityEngine.Random.value > 0.5f ? "BAT" : "BOWL";
                    _view.FlashAutoSelection(pick);
                    
                    DOVirtual.DelayedCall(1.2f, () => {
                        _matchHandler.SendDecision(pick);
                    });
                });
            });
        }
    }

    private void HandleGameStarted(string battingPlayerId, bool amBatting)
    {
        _amIBatting = amBatting;
        _view.ShowPanel("Game");
        _view.UpdateRole(_amIBatting ? "BATTING" : "BOWLING");
        _view.ClearBallHistory();
            
        // SEQUENTIAL BANNER KICKOFF
        _view.ShowPhaseBanner("MATCH START!", 1.5f, () => {
             // Small buffer before Role Banner
             DOVirtual.DelayedCall(0.5f, () => {
                 _view.ShowRoleBanner(_amIBatting ? "BATTING" : "BOWLING");

                 DOVirtual.DelayedCall(1.5f, () => {
                    PrepareTurn();
                });
            });
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

    private void PrepareTurn()
    {
        _view.SetInputInteractivity(true);
        _view.ShowYourTurn(true);
        StartTimer(TURN_DURATION, () => {
            int pick = UnityEngine.Random.Range(1, 7);
            _view.FlashAutoSelection(pick.ToString());
            
            DOVirtual.DelayedCall(1.2f, () => {
                _view.SetInputInteractivity(false);
                _matchHandler.SendTurnInput(pick);
            });
        });
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
            int displayBalls = data.balls + 1; 
            string myScoreMsg = $"SCORE: {data.batsman_score}/{data.batsman_wickets}";
            _view.UpdateScoreDisplay(myScoreMsg, statusMsg, displayBalls, _maxOvers, data.target);
            
            // Set Names for current batsman/bowler
            string myName = _matchHandler.MyUsername;
            string oppName = _matchHandler.OpponentUsername;
            _view.UpdateNames(_amIBatting ? myName : oppName, _amIBatting ? oppName : myName);
            
            // Auto-clear history if we just finished an over (multiple of 6)
            // if (displayBalls > 0 && displayBalls % 6 == 0) _view.ClearBallHistory();
            
            _view.UpdateBallHistory(eventType == "WICKET" ? "W" : data.bat_input.ToString());

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
        _isTransitioning = false;
        _view.ShowPanel("Game");
        
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        _view.ClearBallHistory();
        _amIBatting = (_pendingInningsPayload.batting_player_id == NakamaSessionManager.Session.UserId);
        _view.UpdateRole(_amIBatting ? "BATTING" : "BOWLING");

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
