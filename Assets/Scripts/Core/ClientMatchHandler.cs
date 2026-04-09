using Nakama;
using UnityEngine;
using System;
using System.Collections.Generic;

public class ClientMatchHandler : MonoBehaviour
{
    public static ClientMatchHandler Instance;

    private IMatch currentMatch;
    private ISocket socket => NakamaService.Socket;

    // State Cache
    public string LastTossInitiatorId { get; private set; }

    // Events for UI to subscribe to
    // These events decouple the UI from the network logic
    public event Action<string> OnTossStarted;
    public event Action<string, string> OnTossResult; // winner, outcome
    public event Action<string, bool> OnGameStarted; // opponentId, amIBatting
    // Updated TurnResult with full stats
    public event Action<TurnResultPayload> OnTurnResult; 
    public event Action<InningsChangePayload> OnInningsChanged;
    public event Action<GameOverPayload> OnGameOver;
    public event Action<MatchSummaryPayload> OnMatchSummary; // OpCode 106

    // Player Info
    public string MyUsername { get; private set; }
    public string OpponentUsername { get; private set; }
    public int MaxWickets { get; private set; }
    public int MaxOvers { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void Prepare()
    {
        // Unsubscribe first to avoid double-subscription if called multiple times
        socket.ReceivedMatchState -= OnReceivedMatchState;
        socket.ReceivedMatchPresence -= OnReceivedMatchPresence;
        
        socket.ReceivedMatchState += OnReceivedMatchState;
        socket.ReceivedMatchPresence += OnReceivedMatchPresence;
    }

    public void SetMatch(IMatch match)
    {
        currentMatch = match;
        Debug.Log("Match Handler Set for: " + match.Id);
        
        // Populate Usernames
        MyUsername = NakamaSessionManager.Session.Username;
        OpponentUsername = "Waiting...";

        foreach (var p in match.Presences)
        {
            if (p.UserId != NakamaSessionManager.Session.UserId)
            {
                OpponentUsername = p.Username;
            }
        }
    }

    // Deprecated single init
    public void Initialize(IMatch match)
    {
        currentMatch = match;
        // Unsubscribe first
        socket.ReceivedMatchState -= OnReceivedMatchState;
        socket.ReceivedMatchState += OnReceivedMatchState;
        Debug.Log("Match Handler Initialized for: " + match.Id);
        
        // Populate Usernames
        MyUsername = NakamaSessionManager.Session.Username;
        foreach (var p in match.Presences)
        {
            if (p.UserId != NakamaSessionManager.Session.UserId)
            {
                OpponentUsername = p.Username;
            }
        }
        if(string.IsNullOrEmpty(OpponentUsername)) OpponentUsername = "Waiting...";
    }

    public void Reset()
    {
        LastTossInitiatorId = null;
        OpponentUsername = "Waiting...";
        MaxOvers = 0;
        MaxWickets = 0;
        currentMatch = null;
        Debug.Log("Client Match Handler Fully Reset.");
    }

    public void LeaveMatch()
    {
        if (socket != null)
        {
            socket.ReceivedMatchState -= OnReceivedMatchState;
            socket.ReceivedMatchPresence -= OnReceivedMatchPresence;
        }
        currentMatch = null;
        Reset();
    }

    void OnDestroy()
    {
        LeaveMatch();
    }

    private void OnReceivedMatchPresence(IMatchPresenceEvent e)
    {
        foreach (var p in e.Joins)
        {
            if (p.UserId != NakamaSessionManager.Session.UserId)
            {
                Debug.Log("Opponent Joined: " + p.Username);
                OpponentUsername = p.Username;
            }
        }
    }

    private void OnReceivedMatchState(IMatchState matchState)
    {
        MainThreadDispatcher.Enqueue(() => {
            HandleMatchState(matchState);
        });
    }

    private void HandleMatchState(IMatchState state)
    {
        string json = System.Text.Encoding.UTF8.GetString(state.State);
        Debug.Log($"Received OpCode {state.OpCode}: {json}");

        // OpCodes defined in Lua:
        // 100: Toss Started
        // 101: Toss Result
        // 102: Game Start
        // 103: Turn Result

        switch (state.OpCode)
        {
            case 100:
                var tossStart = JsonUtility.FromJson<TossStartPayload>(json);
                
                // Set Names Early (Fix for "Waiting..." issue)
                if (tossStart.p1_id == NakamaSessionManager.Session.UserId)
                {
                    MyUsername = tossStart.p1_username;
                    OpponentUsername = tossStart.p2_username;
                }
                else
                {
                    MyUsername = tossStart.p2_username;
                    OpponentUsername = tossStart.p1_username;
                }

                LastTossInitiatorId = tossStart.initiator_id; 
                OnTossStarted?.Invoke(tossStart.initiator_id);
                break;
            case 101:
                var tossResult = JsonUtility.FromJson<TossResultPayload>(json);
                OnTossResult?.Invoke(tossResult.toss_winner, tossResult.outcome);
                break;
            case 102:
                var gameStart = JsonUtility.FromJson<GameStartPayload>(json);
                bool amIBatting = (gameStart.batting_player == NakamaSessionManager.Session.UserId);
                
                // Update Usernames from payload
                if (gameStart.p1_id == NakamaSessionManager.Session.UserId)
                {
                    MyUsername = gameStart.p1_username;
                    OpponentUsername = gameStart.p2_username;
                }
                else
                {
                    MyUsername = gameStart.p2_username;
                    OpponentUsername = gameStart.p1_username;
                }

                MaxWickets = gameStart.max_wickets;
                MaxOvers = gameStart.max_overs;

                OnGameStarted?.Invoke(gameStart.batting_player, amIBatting);
                break;
            case 103:
                var turnResult = JsonUtility.FromJson<TurnResultPayload>(json);
                OnTurnResult?.Invoke(turnResult);
                break;
            case 104:
                var inningsChange = JsonUtility.FromJson<InningsChangePayload>(json);
                OnInningsChanged?.Invoke(inningsChange);
                break;
            case 105:
                var gameOver = JsonUtility.FromJson<GameOverPayload>(json);
                OnGameOver?.Invoke(gameOver);
                break;
            case 106:
                var summary = JsonUtility.FromJson<MatchSummaryPayload>(json);
                OnMatchSummary?.Invoke(summary);
                break;
        }
    }

    // --- SEND ACTIONS ---

    public async void SendTossSelection(string selection) // "Heads" or "Tails"
    {
        var data = new TossSelectionPayload { selection = selection };
        await socket.SendMatchStateAsync(currentMatch.Id, 1, JsonUtility.ToJson(data));
    }

    public async void SendDecision(string choice) // "Bat" or "Bowl"
    {
        var data = new DecisionPayload { choice = choice };
        await socket.SendMatchStateAsync(currentMatch.Id, 2, JsonUtility.ToJson(data));
    }

    public async void SendTurnInput(int input) // 1-6
    {
        Debug.Log($"[ClientMatchHandler] Sending Turn Input: {input}");
        var data = new TurnInputPayload { input = input };
        try 
        {
            await socket.SendMatchStateAsync(currentMatch.Id, 3, JsonUtility.ToJson(data));
            Debug.Log("[ClientMatchHandler] Turn Input Sent Successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ClientMatchHandler] Failed to send turn input: {e.Message}");
        }
    }

    /// <summary>
    /// Explicitly requests the current match state from the server.
    /// Used for recovering state if the client disconnects or misses a packet (Lag/Late Join).
    /// </summary>
    public async void RequestMatchState()
    {
        Debug.Log("[ClientMatchHandler] Requesting Full Match State...");
        await socket.SendMatchStateAsync(currentMatch.Id, 4, "{}");
    }

    // Payloads
    [Serializable] public class TossSelectionPayload { public string selection; }
    [Serializable] public class DecisionPayload { public string choice; }
    [Serializable] public class TurnInputPayload { public int input; }
    [Serializable] public class TossStartPayload { 
        public string initiator_id; 
        public string p1_username;
        public string p2_username;
        public string p1_id;
        public string p2_id;
    }
    [Serializable] public class TossResultPayload { public int section; public string toss_winner; public string outcome; }
[Serializable] public class GameStartPayload { 
    public int section;
    public string batting_player; 
    public string p1_username;
    public string p2_username;
    public string p1_id;
    public string p2_id;
    public int max_wickets;
    public int max_overs;
}
[Serializable] public class TurnResultPayload { 
    public int bat_input; 
    public int bowl_input; 
    public string event_type; // "event" in JSON, mapping manually might be needed if naming differs
    public string @event; // Helper for JSON casing
    public int batsman_score;
    public int batsman_wickets;
    public int balls;
    public int innings;
    public int target;
}
[Serializable] public class InningsChangePayload { 
    public int target; 
    public string message;
    public string batting_player_id;
}
[Serializable] public class GameOverPayload { 
    public string winner; 
    public string reason;
}

[Serializable] public class MatchSummaryPayload {
    public string winner;
    public string reason;
    public int coins_earned;
    public int xp_earned;
    public int new_level;
    public bool leveled_up;
}
}
