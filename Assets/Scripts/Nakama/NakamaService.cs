using Nakama;
using System.Threading.Tasks;
using UnityEngine;

public class NakamaService
{
    public static IClient Client;
    public static ISocket Socket;

    public static bool IsIntentionalDisconnect = false;
    private static bool _isReconnecting = false;
    private static int _reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 5;

    public static void Initialize()
    {
        Client = new Client(
            "http",
            "127.0.0.1",
            7350,
            "defaultkey"
        );

        Client.Timeout = 10;
    }

    public static async Task ConnectSocket(
        ISession session)
    {
        if (Socket == null)
            Socket = Client.NewSocket();

        Socket.Closed -= OnSocketClosed;
        Socket.Closed += OnSocketClosed;

        if (!Socket.IsConnected)
        {
            await Socket.ConnectAsync(session);
            
            // Initialize Singleton Services once socket is alive
            SocialService.Instance.Initialize(Socket);
            MatchmakingService.Instance.Init(Socket);

            // SYNC STADIUMS FROM BACKEND
            if (StadiumService.Instance != null)
            {
                _ = StadiumService.Instance.SyncStadiumsAsync(session);
            }
        }
    }

    private static void OnSocketClosed(string reason)
    {
        Debug.Log($"Socket closed: {reason}");
        if (IsIntentionalDisconnect) return;

        MainThreadDispatcher.Enqueue(() => {
            _ = StartReconnectionLoop();
        });
    }

    private static async Task StartReconnectionLoop()
    {
        if (_isReconnecting) return;
        _isReconnecting = true;
        _reconnectAttempts = 0;

        Debug.LogWarning("Socket closed unexpectedly. Starting reconnect sequence...");
        NotificationUI.Show("Connection lost. Reconnecting... (15s)", false);

        while (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
        {
            await Task.Delay(3000);
            _reconnectAttempts++;
            
            try
            {
                if (NakamaSessionManager.Session != null)
                {
                    Debug.Log($"Reconnection attempt {_reconnectAttempts} / {MAX_RECONNECT_ATTEMPTS}");
                    
                    if (Socket != null) Socket.Closed -= OnSocketClosed;
                    Socket = Client.NewSocket();
                    Socket.Closed += OnSocketClosed;
                    
                    await Socket.ConnectAsync(NakamaSessionManager.Session);
                    
                    Debug.Log("Successfully reconnected!");
                    NotificationUI.Show("Reconnected to Server!", false);
                    
                    // Reinitialize singletons on new socket
                    SocialService.Instance.Initialize(Socket);
                    MatchmakingService.Instance.Init(Socket);
                    
                    if (StadiumService.Instance != null)
                    {
                        _ = StadiumService.Instance.SyncStadiumsAsync(NakamaSessionManager.Session);
                    }
                    
                    _isReconnecting = false;
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Reconnect failed: {e.Message}");
            }
        }

        // Failure
        _isReconnecting = false;
        Debug.LogError("Max reconnect attempts reached. Logging out.");
        
        NotificationUI.Show("Failed to reconnect. Please log in again.", false);
        NakamaSessionManager.Logout();
        
        if (UIScreenManager.Instance != null)
        {
            UIScreenManager.Instance.Show("LoginScreen");
        }
    }

    public static async Task FetchUserStatsAsync(ISession session, UserData userData)
    {
        try
        {
            var result = await Client.ReadStorageObjectsAsync(session, new[] {
                new StorageObjectId { Collection = "stats", Key = "user_stats", UserId = session.UserId }
            });

            foreach (var obj in result.Objects)
            {
                if (obj.Key == "user_stats")
                {
                    userData.SetStats(obj.Value);
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to fetch user stats: " + e.Message);
        }
    }
}
