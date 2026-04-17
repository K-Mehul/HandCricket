using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class TutorialMatchSimulation : MonoBehaviour
{
    public static TutorialMatchSimulation Instance;

    private int _userScore = 0;
    private int _botScore = 0;
    private bool _isUserBatting = true;
    private int _balls = 0;
    private int _turnCount = 0;
    private int _target = 0;
    private int _inningsCount = 1;

    private int _maxOvers = 1;
    private int _maxWickets = 1;

    private readonly string[] BotNames = { "Ravi", "Mehul", "Simran", "Virat", "Rohit", "Dhoni", "Sachin" };
    private string _currentBotName;
    private int _ballsThisInnings = 0;
    private bool _isProcessingTurn = false;

    private void Awake()
    {
        Instance = this;
    }

    public void SetMatchConfig(int overs, int wickets)
    {
        _maxOvers = overs;
        _maxWickets = wickets;
        Debug.Log($"Tutorial: Configured for {_maxOvers} Overs, {_maxWickets} Wickets.");
    }

    public void StartSimulation()
    {
        // Safety: Ensure Match Handler exists so UI can subscribe
        if (ClientMatchHandler.Instance == null)
        {
            var handlerObj = new GameObject("ClientMatchHandler");
            handlerObj.AddComponent<ClientMatchHandler>();
            DontDestroyOnLoad(handlerObj);
            ClientMatchHandler.Instance = handlerObj.GetComponent<ClientMatchHandler>();
            ClientMatchHandler.Instance.Prepare();
        }

        ClientMatchHandler.Instance.IsLocalSimulation = true;
        _currentBotName = BotNames[UnityEngine.Random.Range(0, BotNames.Length)];

        _turnCount = 0;
        _balls = 0;
        _userScore = 0;
        _botScore = 0;
        _isUserBatting = true;
        _ballsThisInnings = 0;
        _target = 0;
        _inningsCount = 1;
        _isProcessingTurn = false; // Reset lock
        
        Debug.Log($"[TutorialSimulation] STARTING. Target: {_target}, Innings: {_inningsCount}");

        StartCoroutine(SimulateMatchFlow());
    }

    private IEnumerator SimulateMatchFlow()
    {
        yield return new WaitForSeconds(1.5f);

        // 1. Transition to Intro Screen
        if (UIScreenManager.Instance != null && ClientMatchHandler.Instance != null)
        {
            // Ensure names are set correctly for Intro
            typeof(ClientMatchHandler).GetProperty("MyUsername")?.SetValue(ClientMatchHandler.Instance, NakamaSessionManager.Session.Username);
            typeof(ClientMatchHandler).GetProperty("OpponentUsername")?.SetValue(ClientMatchHandler.Instance, _currentBotName);

            UIScreenManager.Instance.Show("MatchIntroScreen");
            var intro = UIScreenManager.Instance.GetCurrentScreen<MatchIntroScreen>();
            if (intro != null)
            {
                intro.Init(NakamaSessionManager.Session.Username, _currentBotName, $"Practice @ Selected Stadium\n{_maxOvers} Overs | {_maxWickets} Wickets");
            }
        }

        TutorialManager.Instance.SetState(TutorialManager.TutorialState.Match_Intro);
        yield return new WaitForSeconds(3.5f);

        // 2. Transition to Game Scene
        if (UIScreenManager.Instance != null && ClientMatchHandler.Instance != null)
        {
            // IMPORTANT: Set LastTossInitiatorId so GameScreen.OnShow calls Presenter.Init
            typeof(ClientMatchHandler).GetProperty("LastTossInitiatorId")?.SetValue(ClientMatchHandler.Instance, NakamaSessionManager.Session.UserId);
            Debug.Log("TUTORIAL MATCH GAME SCENE");
            UIScreenManager.Instance.Show("GameScene");
        }

        TutorialManager.Instance.SetState(TutorialManager.TutorialState.Match_Toss);
        
        // Trigger Toss UI (Wait 1s for GamePresenter to subscribe)
        yield return new WaitForSeconds(1.0f);
        RaiseMatchEvent("OnTossStarted", new object[] { NakamaSessionManager.Session.UserId });
    }

    public void OnTossCalled(string selection)
    {
        StartCoroutine(ProcessToss(selection));
    }

    private IEnumerator ProcessToss(string userSelection)
    {
        yield return new WaitForSeconds(1.5f);
        // Random outcome for realism, but user always "wins" toss for tutorial progress
        string outcome = UnityEngine.Random.value > 0.5f ? "HEADS" : "TAILS";
        RaiseMatchEvent("OnTossResult", new object[] { NakamaSessionManager.Session.UserId, outcome });
        TutorialManager.Instance.SetState(TutorialManager.TutorialState.Match_Decision);
    }

    public void OnDecisionMade(string decision)
    {
        StartCoroutine(ProcessDecision(decision));
    }

    private IEnumerator ProcessDecision(string decision)
    {
        yield return new WaitForSeconds(1f);
        _isUserBatting = (decision == "BAT");
        _ballsThisInnings = 0;
        
        // Apply config to handler so UI knows the scale (using reflection for private setters)
        typeof(ClientMatchHandler).GetProperty("MaxOvers")?.SetValue(ClientMatchHandler.Instance, _maxOvers);
        typeof(ClientMatchHandler).GetProperty("MaxWickets")?.SetValue(ClientMatchHandler.Instance, _maxWickets);

        Debug.Log("RAISE OnGame Started");


        RaiseMatchEvent("OnGameStarted", new object[] { 
            _isUserBatting ? NakamaSessionManager.Session.UserId : "tutorial-bot", 
            _isUserBatting 
        });
    }

    public void OnNumberPicked(int userNum)
    {
        Debug.Log($"[TutorialSimulation] Received Input: {userNum}. ProcessingLock: {_isProcessingTurn}");
        if (_isProcessingTurn) 
        {
            Debug.LogWarning("[TutorialSimulation] Ignoring input - turn already processing.");
            return;
        }

        _isProcessingTurn = true;
        _turnCount++;
        _ballsThisInnings++;
        StartCoroutine(ProcessTurn(userNum));
    }

    private IEnumerator ProcessTurn(int userNum)
    {
        yield return new WaitForSeconds(1f);
        
        int botNum = UnityEngine.Random.Range(1, 7);
        string eventType = "RUN";

        // Forced wicket logic for tutorial length control
        bool forceWicket = (_ballsThisInnings >= 6); 
        if (_ballsThisInnings >= 3 && UnityEngine.Random.value > 0.7f) forceWicket = true;

        if (botNum == userNum || forceWicket)
        {
            botNum = userNum;
            eventType = "WICKET";
        }

        _balls++;
        int turnScore = _isUserBatting ? userNum : botNum;
        if (eventType == "WICKET") turnScore = 0;

        if (_isUserBatting) _userScore += turnScore;
        else _botScore += turnScore;

        Debug.Log($"[TutorialSimulation] Turn: {userNum} vs {botNum} ({eventType}). Total: {(_isUserBatting?_userScore:_botScore)}");

        var result = new ClientMatchHandler.TurnResultPayload
        {
            bat_input = _isUserBatting ? userNum : botNum,
            bowl_input = _isUserBatting ? botNum : userNum,
            batsman_score = _isUserBatting ? _userScore : _botScore,
            batsman_wickets = (eventType == "WICKET") ? 1 : 0,
            balls = _balls - 1, // Offset to match server's "pre-increment" broadcast style
            event_type = eventType,
            @event = eventType,
            target = _target,
            innings = _inningsCount
        };

        RaiseMatchEvent("OnTurnResult", new object[] { result });

        yield return new WaitForSeconds(2.5f);
        _isProcessingTurn = false; // Release lock after simulation time

        bool inningsOver = (eventType == "WICKET" || _balls >= (_maxOvers * 6));
        
        // CHASE LOGIC
        if (_inningsCount == 2)
        {
            int currentBattingScore = _isUserBatting ? _userScore : _botScore;
            if (currentBattingScore >= _target)
            {
                Debug.Log($"[TutorialSimulation] Target ({_target}) reached by team with {currentBattingScore} runs.");
                inningsOver = true;
            }
        }

        if (inningsOver)
        {
            if (_inningsCount == 1)
            {
                Debug.Log($"[TutorialSimulation] INNINGS 1 OVER. Switching. Runs Scored: {(_isUserBatting?_userScore:_botScore)}");
                StartCoroutine(SwitchInnings());
            }
            else
            {
                Debug.Log($"[TutorialSimulation] INNINGS 2 OVER. Final Scores - User: {_userScore}, Bot: {_botScore}, Target: {_target}");
                CompleteTutorialMatch();
            }
        }
    }

    private IEnumerator SwitchInnings()
    {
        _target = _userScore + 1;
        _isUserBatting = !_isUserBatting;
        _balls = 0;
        _ballsThisInnings = 0;
        _inningsCount = 2; // Move to Innings 2

        var payload = new ClientMatchHandler.InningsChangePayload
        {
            target = _target,
            message = "Target Set! Show them your skills.",
            batting_player_id = _isUserBatting ? NakamaSessionManager.Session.UserId : "tutorial-bot"
        };

        RaiseMatchEvent("OnInningsChanged", new object[] { payload });
        yield return null;
    }

    private void CompleteTutorialMatch()
    {
        string winnerName = "DRAW";
        if (_userScore > _botScore)
        {
            winnerName = NakamaSessionManager.Session.Username;
        }
        else if (_botScore > _userScore)
        {
            winnerName = _currentBotName;
        }

        var summary = new ClientMatchHandler.MatchSummaryPayload
        {
            winner = winnerName,
            coins_earned = 150,
            xp_earned = 75,
            leveled_up = true,
            new_level = 2
        };
        RaiseMatchEvent("OnMatchSummary", new object[] { summary });
        
        TutorialManager.Instance.CompleteTutorial();
    }

    private void RaiseMatchEvent(string eventName, object[] args)
    {
        var handler = ClientMatchHandler.Instance;
        if (handler == null) return;

        FieldInfo field = typeof(ClientMatchHandler).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null)
        {
            MulticastDelegate multicastDelegate = field.GetValue(handler) as MulticastDelegate;
            if (multicastDelegate != null)
            {
                foreach (var handlerDelegate in multicastDelegate.GetInvocationList())
                {
                    handlerDelegate.Method.Invoke(handlerDelegate.Target, args);
                }
            }
        }
    }
}
