using Nakama;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Nakama.TinyJson;
using System.Linq;

public class SocialService
{
    private static SocialService _instance;
    public static SocialService Instance => _instance ?? (_instance = new SocialService());

    public event System.Action OnSocialUpdate;
    // MatchId, SenderUsername, StadiumName, Stake, Overs, Wickets
    public event System.Action<string, string, string, int, int, int> OnMatchInviteReceived; 

    public int PendingCount { get; private set; }

    public IClient Client => NakamaService.Client;
    public ISession Session => NakamaSessionManager.Session;

    private SocialService()
    {
    }

    private bool _isInitialized = false;

    public void Initialize(ISocket socket)
    {
        if (socket == null) return;
        
        // Prevent double subscription if Initialize is called multiple times
        if (_isInitialized)
        {
            socket.ReceivedNotification -= OnNotification;
            socket.ReceivedStatusPresence -= OnStatusPresence;
        }

        socket.ReceivedNotification += OnNotification;
        socket.ReceivedStatusPresence += OnStatusPresence;
        _isInitialized = true;
        
        Debug.Log("SocialService: Initialized and subscribed to socket events.");
    }

    private void OnStatusPresence(IStatusPresenceEvent presenceEvent)
    {
        foreach (var presence in presenceEvent.Joins)
        {
            Debug.Log($"Friend {presence.Username} came online.");
        }
        foreach (var presence in presenceEvent.Leaves)
        {
            Debug.Log($"Friend {presence.Username} went offline.");
        }
        
        // Refresh the list to update status dots
        MainThreadDispatcher.Enqueue(() => OnSocialUpdate?.Invoke());
    }

    private void OnNotification(IApiNotification notification)
    {
        // Ignore system notifications (Code < 0) unless they are important
        if (notification.Code < 0) return;

        Debug.Log($"Social Notification Received: Code {notification.Code} | Subject: {notification.Subject}");

        // Code 10 = New Friend Request
        if (notification.Code == 10)
        {
            RefreshPendingCount();
        }
        // Code 20 = Match Invite
        else if (notification.Code == 20)
        {
            Debug.Log($"Match Invite Data: {notification.Content}");
            try 
            {
                var content = notification.Content.FromJson<Dictionary<string, string>>();
                if (content != null && content.ContainsKey("matchId") && content.ContainsKey("username"))
                {
                    string mId = content["matchId"];
                    string uName = content["username"];
                    string sName = content.ContainsKey("stadium") ? content["stadium"] : "Gully";
                    int stake = content.ContainsKey("stake") ? int.Parse(content["stake"]) : 0;
                    int overs = content.ContainsKey("overs") ? int.Parse(content["overs"]) : 5;
                    int wickets = content.ContainsKey("wickets") ? int.Parse(content["wickets"]) : 3;
                    
                    MainThreadDispatcher.Enqueue(() => {
                        Debug.Log($"Invoking OnMatchInviteReceived for {uName} at {sName}");
                        OnMatchInviteReceived?.Invoke(mId, uName, sName, stake, overs, wickets);
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Match Invite Parse Error: {e.Message}");
            }
        }
        // Code 200 = Session Conflict (Login from another device)
        else if (notification.Code == 200)
        {
            try
            {
                var content = notification.Content.FromJson<System.Collections.Generic.Dictionary<string, string>>();
                if (content != null && content.ContainsKey("new_instance_id"))
                {
                    string incomingInstanceId = content["new_instance_id"];
                    
                    // If the incoming instance ID does NOT match this device's instance ID
                    if (incomingInstanceId != NakamaSessionManager.InstanceId)
                    {
                        Debug.LogWarning("SESSION CONFLICT: Another device logged in. Logging out.");
                        MainThreadDispatcher.Enqueue(() =>
                        {
                            NakamaSessionManager.Logout();
                            UIScreenManager.Instance.Show("LoginScreen");
                            NotificationUI.Show("Logged out: Your account was accessed from another device.", false);
                        });
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Session Conflict Parse Error: {e.Message}");
            }
        }
    }

    public async Task RefreshPendingCount()
    {
        var friends = await ListFriends();
        int count = 0;
        foreach (var f in friends)
        {
            if (f.State == 2) count++; // 2 = Received (Pending)
        }
        PendingCount = count;
        
        MainThreadDispatcher.Enqueue(() => OnSocialUpdate?.Invoke());
    }

    /// <summary>
    /// Send a friend request by username.
    /// </summary>
    public async Task<(bool success, string message)> AddFriendByUsername(string username)
    {
        try
        {
            // 1. Check if they are already in any state (Friend, Sent, Received, Blocked)
            var friends = await ListFriends();
            foreach (var f in friends)
            {
                if (f.User.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase))
                {
                    switch (f.State)
                    {
                        case 0: return (false, "Already a friend!");
                        case 1: return (false, "Request already sent.");
                        case 2: 
                            // This is an "Accept" action, let it proceed!
                            break;
                        case 3: return (false, "User is blocked.");
                    }
                }
            }

            await Client.AddFriendsAsync(Session, null, new[] { username });
            Debug.Log($"Friend request sent to: {username}");
            await RefreshPendingCount();
            return (true, $"Request sent to {username}!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to add friend: {e.Message}");
            return (false, "Failed to send request. Check username.");
        }
    }

    /// <summary>
    /// Send a friend request by user ID.
    /// </summary>
    public async Task<(bool success, string message)> AddFriendById(string userId)
    {
        try
        {
            var friends = await ListFriends();
            foreach (var f in friends)
            {
                if (f.User.Id == userId)
                {
                    switch (f.State)
                    {
                        case 0: return (false, "Already a friend!");
                        case 1: return (false, "Request already sent.");
                        case 2:
                            // This is an "Accept" action, let it proceed!
                            break;
                        case 3: return (false, "User is blocked.");
                    }
                }
            }

            await Client.AddFriendsAsync(Session, new[] { userId });
            Debug.Log($"Friend request sent to ID: {userId}");
            await RefreshPendingCount();
            return (true, "Request sent!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to add friend by ID: {e.Message}");
            return (false, "Failed to send request.");
        }
    }

    /// <summary>
    /// Fetch the list of friends (includes pending and invited).
    /// </summary>
    public async Task<IEnumerable<IApiFriend>> ListFriends()
    {
        try
        {
            var result = await Client.ListFriendsAsync(Session);
            return result.Friends;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to list friends: {e.Message}");
            return new List<IApiFriend>();
        }
    }

    /// <summary>
    /// Remove a friend or reject a request.
    /// </summary>
    public async Task RemoveFriend(string userId)
    {
        try
        {
            await Client.DeleteFriendsAsync(Session, new[] { userId });
            Debug.Log($"Friend removed/rejected: {userId}");
            await RefreshPendingCount();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to remove friend: {e.Message}");
        }
    }

    /// <summary>
    /// Block a user.
    /// </summary>
    public async Task BlockUser(string userId)
    {
        try
        {
            await Client.BlockFriendsAsync(Session, new[] { userId });
            Debug.Log($"User blocked: {userId}");
            await RefreshPendingCount();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to block user: {e.Message}");
        }
    }

    /// <summary>
    /// Send a match invitation to a friend with stadium details.
    /// </summary>
    public async Task SendMatchInvite(string userId, string matchId, string myUsername, string stadiumName, int stake, int overs, int wickets)
    {
        try
        {
            var content = new Dictionary<string, string> {
                { "matchId", matchId },
                { "username", myUsername },
                { "stadium", stadiumName },
                { "stake", stake.ToString() },
                { "overs", overs.ToString() },
                { "wickets", wickets.ToString() }
            }.ToJson();

            // Code 20 = Match Invite
            await Client.RpcAsync(Session, "send_social_notification", new Dictionary<string, object> {
                { "user_id", userId },
                { "code", 20 },
                { "content", content },
                { "persistent", false }
            }.ToJson());
            
            Debug.Log($"Match invite sent to {userId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send match invite: {e.Message}");
        }
    }

    /// <summary>
    /// Fetch details for a private room by its join code.
    /// </summary>
    public async Task<Dictionary<string, string>> GetPrivateRoomDetails(string code)
    {
        try
        {
            var payload = new Dictionary<string, string> { { "code", code } }.ToJson();
            var result = await Client.RpcAsync(Session, "get_private_room", payload);
            var response = result.Payload.FromJson<Dictionary<string, object>>();
            
            if (response.ContainsKey("success") && (bool)response["success"])
            {
                // Result from Storage is often a Dictionary<string, object>
                var roomDataObj = response["room_data"] as Dictionary<string, object>;
                if (roomDataObj != null)
                {
                    var roomData = new Dictionary<string, string>();
                    foreach(var kv in roomDataObj) roomData[kv.Key] = kv.Value.ToString();
                    return roomData;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get room details: {e.Message}");
        }
        return null;
    }

    /// <summary>
    /// Update the user's online status. This makes them appear "Online" to friends.
    /// </summary>
    public async Task SetOnlineStatus(bool online)
    {
        try
        {
            if (online)
            {
                // Updating status (even with empty string) tells Nakama to track presence
                await NakamaService.Socket.UpdateStatusAsync("");
                Debug.Log("Broadcasted 'Online' status.");
            }
            // Offline is handled by socket disconnection automatically, 
            // but we can explicitly set it if needed.
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to set online status: {e.Message}");
        }
    }

    /// <summary>
    /// Fetches the recent match history for the current user.
    /// </summary>
    public async Task<List<MatchHistoryEntry>> FetchMatchHistoryAsync()
    {
        try
        {
            // NEW: Read the consolidated 'history' object from 'stats' collection
            var objectIds = new[] { new StorageObjectId { Collection = "stats", Key = "history", UserId = Session.UserId } };
            var result = await Client.ReadStorageObjectsAsync(Session, objectIds);
            
            var history = new List<MatchHistoryEntry>();

            if (result.Objects.Any())
            {
                var storageObj = result.Objects.First();
                var data = storageObj.Value.FromJson<Dictionary<string, List<MatchHistoryEntry>>>();
                if (data != null && data.ContainsKey("matches"))
                {
                    history = data["matches"];
                }
            }

            // The list is already prepended (newest first) on the server, but let's sort to be safe
            history.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
            return history;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SocialService: Failed to fetch match history: {e.Message}");
            return new List<MatchHistoryEntry>();
        }
    }

    [System.Serializable]
    public class MatchHistoryEntry
    {
        public string opponent;
        public string result;
        public int earnings;
        public long timestamp;
        public string match_id;

        public string GetFormattedDate()
        {
            var dt = System.DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            return dt.ToString("dd MMM, hh:mm tt");
        }
    }

    /// <summary>
    /// Fetches a list of leaderboard records.
    /// </summary>
    /// <param name="leaderboardId">ID of the leaderboard (e.g., daily_wins)</param>
    /// <param name="limit">Max records to fetch</param>
    /// <param name="friendsOnly">If true, only fetch records for friends</param>
    public async Task<List<LeaderboardEntry>> FetchLeaderboardAsync(string leaderboardId, int limit = 50, bool friendsOnly = false)
    {
        try
        {
            IApiLeaderboardRecordList result;
            if (friendsOnly)
            {
                // Correct way to show ONLY friends on a leaderboard:
                // 1. Get the friend list
                var friends = await ListFriends();
                var friendIds = new List<string>();
                
                // Add current user to the list
                friendIds.Add(Session.UserId);
                
                // Add all active friends (State == 0)
                int friendCount = 0;
                foreach(var f in friends)
                {
                    Debug.Log($"SocialService: Friend Analysis - {f.User.Username} (ID: {f.User.Id}) State: {f.State}");
                    if(f.State == 0) 
                    {
                        friendIds.Add(f.User.Id);
                        friendCount++;
                    }
                }

                // 2. Fetch records ONLY for these IDs
                Debug.Log($"SocialService: Sending Request for {friendIds.Count} IDs: " + string.Join(", ", friendIds));
                result = await Client.ListLeaderboardRecordsAsync(session: Session, leaderboardId: leaderboardId, ownerIds: friendIds, limit: limit);
                
                // 3. NUCLEAR OPTION: Manually filter results to be 100% sure
                var filteredRecords = new List<IApiLeaderboardRecord>();
                foreach(var record in result.Records)
                {
                    if (friendIds.Contains(record.OwnerId))
                    {
                        filteredRecords.Add(record);
                    }
                    else
                    {
                        Debug.LogWarning($"SocialService: SERVER FILTER FAILED. Stripping ghost record: {record.Username}");
                    }
                }

                var entries = new List<LeaderboardEntry>();
                Debug.Log($"SocialService: Final Validated Response - Found {filteredRecords.Count} records.");
                foreach (var record in filteredRecords)
                {
                    Debug.Log($"SocialService: Adding Record - User: {record.Username} Score: {record.Score}");
                    entries.Add(new LeaderboardEntry
                    {
                        rank = int.Parse(record.Rank),
                        username = record.Username,
                        score = long.Parse(record.Score),
                        userId = record.OwnerId
                    });
                }
                return entries;
            }
            else
            {
                result = await Client.ListLeaderboardRecordsAsync(Session, leaderboardId, null, null, limit);
            }

            var finalEntries = new List<LeaderboardEntry>();
            foreach (var record in result.Records)
            {
                finalEntries.Add(new LeaderboardEntry
                {
                    rank = int.Parse(record.Rank),
                    username = record.Username,
                    score = long.Parse(record.Score),
                    userId = record.OwnerId
                });
            }
            return finalEntries;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to fetch leaderboard {leaderboardId}: {e.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string username;
        public long score;
        public string userId;
    }
}
