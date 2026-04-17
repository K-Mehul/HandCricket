using Nakama;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Nakama.TinyJson;    

/// <summary>
/// Manages Matchmaking (Global & Private) and Match Joining.
/// </summary>
public class MatchmakingService
{
    private static MatchmakingService _instance;
    public static MatchmakingService Instance => _instance ?? (_instance = new MatchmakingService());

    private IMatchmakerTicket matchmakerTicket;
    private bool _isInitialized = false;
    
    /// <summary>
    /// Initializes the service and subscribes to matchmaker events.
    /// </summary>
    private MatchmakingService()
    {
    }

    public void Init(ISocket socket)
    {
        if (socket == null || _isInitialized) return;

        socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
        _isInitialized = true;
        Debug.Log("MatchmakingService: Initialized.");
    }

    private async void OnMatchmakerMatched(IMatchmakerMatched matched)
    {
        Debug.Log($"Match Found! Joining Match ID: {matched.MatchId}");
        
        try 
        {
            // 1. Ensure Handler exists and Reset BEFORE joining
            MainThreadDispatcher.Enqueue(() => {
                if (ClientMatchHandler.Instance == null)
                {
                    var handlerObj = new GameObject("ClientMatchHandler");
                    handlerObj.AddComponent<ClientMatchHandler>();
                    Object.DontDestroyOnLoad(handlerObj);
                    ClientMatchHandler.Instance = handlerObj.GetComponent<ClientMatchHandler>();
                    ClientMatchHandler.Instance.Prepare();
                }
                ClientMatchHandler.Instance.Reset();
            });

             var match = await NakamaService.Socket.JoinMatchAsync(matched);
             Debug.Log("Successfully joined match.");
             
              // Update Handler with Match Info
             MainThreadDispatcher.Enqueue(() => {
                 ClientMatchHandler.Instance.SetMatch(match);
                 
                 // NEW: Extract names from matchmaker result
                 string myId = NakamaSessionManager.Session.UserId;
                 string p1Name = "Me";
                 string p2Name = "Opponent";

                 foreach(var user in matched.Users)
                 {
                     if(user.Presence.UserId == myId) p1Name = user.Presence.Username;
                     else p2Name = user.Presence.Username;
                 }

                 // Show Intro Screen first
                 UIScreenManager.Instance.Show("MatchIntroScreen");
                 var intro = UIScreenManager.Instance.GetCurrentScreen<MatchIntroScreen>();
                 if (intro != null)
                 {
                     string info = match.Label;
                     try {
                         var labelData = match.Label.FromJson<Dictionary<string, object>>();
                          if(labelData != null) {
                             // Use keys from match_handler.lua (st, sk, ov, wk)
                             string stadiumName = labelData.ContainsKey("st") ? labelData["st"].ToString() : "Gully Cricket";
                             string stake = labelData.ContainsKey("sk") ? labelData["sk"].ToString() : "0";
                             string overs = labelData.ContainsKey("ov") ? labelData["ov"].ToString() : "2";

                             // Attempt to map to displayName via StadiumService
                             var stadiumObj = StadiumService.Instance != null ? StadiumService.Instance.GetStadium(stadiumName) : null;
                             string finalName = stadiumObj != null ? stadiumObj.displayName : stadiumName;

                             info = $"{finalName}\nStake: {stake} | {overs} Overs";
                          }
                     } catch {}
                     
                     intro.Init(p1Name, p2Name, info);
                 }
                 else 
                 {
                     // Fallback
                     Debug.Log("FALLBACK GAME SCENE");
                    UIScreenManager.Instance.Show("GameScene");
                 }
             });
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining match: " + e.Message);
        }
    }

    private ISocket socket => NakamaService.Socket;

    /// <summary>
    /// Finds a global match using Nakama's Matchmaker.
    /// Uses a wildcard query to match with any available player.
    /// </summary>
    /// <param name="stake">The amount of coins to wager (default 0)</param>
    /// <param name="stadiumName">The name of the stadium preset</param>
    /// <param name="minCount">Minimum players (default 2)</param>
    /// <param name="maxCount">Maximum players (default 2)</param>
    public async Task FindGlobalMatch(int stake, string stadiumName = "Gully Cricket", int minCount = 2, int maxCount = 2)
    {
        Debug.Log($"Finding Global Match in {stadiumName} with Stake: {stake}...");

        // Ensure string properties don't have spaces as Bleve tokenize them
        string safeStadium = stadiumName.Replace(" ", "_");

        // Query: Find players with the exact safe string.
        var query = $"+properties.stake:{stake} +properties.stadium:{safeStadium}";
        
        var stringProps = new Dictionary<string, string>();
        stringProps.Add("stadium", safeStadium);

        var numericProps = new Dictionary<string, double>();
        numericProps.Add("stake", (double)stake);

        try
        {
            var ticket = await socket.AddMatchmakerAsync(
                query, 
                minCount, 
                maxCount,
                stringProps, 
                numericProps);

            Debug.Log($"Matchmaker Ticket: {ticket.Ticket}");
            matchmakerTicket = ticket;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error finding match: {e.Message}");
        }
    }

    /// <summary>
    /// Cancels the current matchmaking request.
    /// </summary>
    public async Task CancelMatchmaking()
    {
        if (matchmakerTicket != null)
        {
            await socket.RemoveMatchmakerAsync(matchmakerTicket.Ticket);
            matchmakerTicket = null;
            Debug.Log("Matchmaking Cancelled");
        }
    }

    /// <summary>
    /// Starts searching for a private match with a specific code.
    /// Acts as both 'Host' (if code is new) and 'Joiner' (if code exists).
    /// </summary>
    /// <param name="code">The 8-character unique code</param>
    /// <param name="overs">Number of overs (default 2)</param>
    /// <param name="wickets">Number of wickets (default 2)</param>
    /// <param name="stake">The stake (default 0)</param>
    public async Task FindPrivateMatch(string code, int stake = 0, int overs = 2, int wickets = 2)
    {
        Debug.Log($"Looking for Private Match with code: {code} and Stake: {stake}");

        // We use a specific query to find ONLY people looking for this code.
        // Queries can filter by string properties.
        var query = $"+properties.code:{code}"; 

        var stringProps = new Dictionary<string, string>();
        stringProps.Add("code", code);
        stringProps.Add("stadium", "Private");

        var numericProps = new Dictionary<string, double>();
        numericProps.Add("stake", (double)stake);
        numericProps.Add("overs", (double)overs);
        numericProps.Add("wickets", (double)wickets);

        try
        {
            // Min 2, Max 2 for 1v1
            var ticket = await socket.AddMatchmakerAsync(
                query, 
                2, 
                2, 
                stringProps,
                numericProps
            ); 

            Debug.Log($"Private Ticket: {ticket.Ticket}");
            matchmakerTicket = ticket;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error private matchmaking: {e.Message}");
        }
    }

    /// <summary>
    /// Generates a random 8-character numeric/alphanumeric code.
    /// </summary>
    public string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[8];
        var random = new System.Random();

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    public async Task RegisterPrivateRoom(string code, string stadium, int stake, int overs, int wickets, int minLevel = 1)
    {
        try
        {
            var payload = new System.Collections.Generic.Dictionary<string, object> {
                { "code", code },
                { "stadium", stadium },
                { "stake", stake.ToString() },
                { "overs", overs.ToString() },
                { "wickets", wickets.ToString() },
                { "minLevel", minLevel.ToString() },
                { "host_id", NakamaSessionManager.Session.UserId }
            }.ToJson();

            await NakamaService.Client.RpcAsync(NakamaSessionManager.Session, "register_private_room", payload);
            Debug.Log($"Private room {code} registered at {stadium}.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to register private room: {e.Message}");
        }
    }

    /// <summary>
    /// Requests an Authoritative Bot Match from the Server.
    /// Used as a fallback when global matchmaking times out.
    /// </summary>
    public async Task RequestBotMatch(string stadium)
    {
        Debug.Log($"Requesting Bot Match for {stadium}...");
        try
        {
            var payload = new Dictionary<string, object> {
                { "stadium", stadium }
            }.ToJson();

            var response = await NakamaService.Client.RpcAsync(NakamaSessionManager.Session, "request_bot_match", payload);
            if (response.Payload != null)
            {
                var data = response.Payload.FromJson<Dictionary<string, object>>();
                if (data.ContainsKey("match_id"))
                {
                    string matchId = data["match_id"].ToString();
                    Debug.Log($"Bot Match created! Joining Match ID: {matchId}");
                    JoinMatchById(matchId);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to request bot match: {e.Message}");
        }
    }

    /// <summary>
    /// Helper to directly join a Match by ID
    /// </summary>
    public async void JoinMatchById(string matchId)
    {
        try 
        {
            // 1. Ensure Handler exists and Reset BEFORE joining
            MainThreadDispatcher.Enqueue(() => {
                if (ClientMatchHandler.Instance == null)
                {
                    var handlerObj = new GameObject("ClientMatchHandler");
                    handlerObj.AddComponent<ClientMatchHandler>();
                    UnityEngine.Object.DontDestroyOnLoad(handlerObj);
                    ClientMatchHandler.Instance = handlerObj.GetComponent<ClientMatchHandler>();
                    ClientMatchHandler.Instance.Prepare();
                }
                //ClientMatchHandler.Instance.Reset();
            });

            var match = await socket.JoinMatchAsync(matchId);
            
            MainThreadDispatcher.Enqueue(() => {
                ClientMatchHandler.Instance.SetMatch(match);
                
                string p1Name = "Me";
                string p2Name = "Opponent";

                // Initialize names from session
                p1Name = NakamaSessionManager.Session.Username;

                // TRY to get bot name from label
                string info = "Bot Match";
                if (!string.IsNullOrEmpty(match.Label)) {
                    try {
                        var labelData = match.Label.FromJson<Dictionary<string, object>>();
                        if(labelData != null) {
                           // Use keys from match_handler.lua (st, sk, ov)
                           string stadiumName = labelData.ContainsKey("st") ? labelData["st"].ToString() : "Gully Cricket";
                           string stake = labelData.ContainsKey("sk") ? labelData["sk"].ToString() : "0";
                           string overs = labelData.ContainsKey("ov") ? labelData["ov"].ToString() : "2";

                           // Attempt to map to displayName via StadiumService
                           var stadiumObj = StadiumService.Instance != null ? StadiumService.Instance.GetStadium(stadiumName) : null;
                           string finalName = stadiumObj != null ? stadiumObj.displayName : stadiumName;

                           info = $"{finalName}\nStake: {stake} | {overs} Overs";

                           if (labelData.ContainsKey("bot_name")) {
                               p2Name = labelData["bot_name"].ToString();
                           }
                        }
                    } catch {}
                }

                foreach(var user in match.Presences)
                {
                    if(user.UserId != NakamaSessionManager.Session.UserId) p2Name = user.Username;
                }

                UIScreenManager.Instance.Show("MatchIntroScreen");
                var intro = UIScreenManager.Instance.GetCurrentScreen<MatchIntroScreen>();
                if (intro != null)
                {
                    intro.Init(p1Name, p2Name, info);
                }
                else 
                {
                    Debug.Log("BOT MATCH GAME SCENE");
                    UIScreenManager.Instance.Show("GameScene");
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining match by ID: " + e.Message);
        }
    }
}

