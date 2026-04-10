using Nakama;
using UnityEngine;
using System.Threading.Tasks;

public static class NakamaSessionManager
{
    public static ISession Session;
    public static readonly string InstanceId = System.Guid.NewGuid().ToString();

    private static string _sessionKey = "nk_session";
    private static string _refreshKey = "nk_refresh";
    private static bool _initialized = false;

    private static void InitializeProfile()
    {
        if (_initialized) return;

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-profile" && i + 1 < args.Length)
            {
                string profile = args[i + 1];
                _sessionKey = $"nk_session_{profile}";
                _refreshKey = $"nk_refresh_{profile}";
                Debug.Log($"Using profile: {profile}");
                break;
            }
        }
        _initialized = true;
    }

    public static void Save(ISession session)
    {
        InitializeProfile();
        Session = session;
        NakamaService.IsIntentionalDisconnect = false;

        PlayerPrefs.SetString(_sessionKey, session.AuthToken);
        PlayerPrefs.SetString(_refreshKey, session.RefreshToken);
        PlayerPrefs.Save();
    }

    public static async Task<bool> RestoreAsync()
    {
        InitializeProfile();
        var authToken = PlayerPrefs.GetString(_sessionKey, "");
        var refreshToken = PlayerPrefs.GetString(_refreshKey, "");

        if (string.IsNullOrEmpty(authToken))
            return false;

        // Restore session with both tokens
        var session = Nakama.Session.Restore(authToken, refreshToken);

        // Check if token is expired or close to expiry (e.g. within 1 hour)
        if (session.HasExpired(System.DateTime.UtcNow.AddHours(1)))
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                Debug.LogWarning("Session expired and no refresh token available.");
                return false;
            }

            try 
            {
                Debug.Log("Attempting session refresh...");
                var client = NakamaService.Client;
                session = await client.SessionRefreshAsync(session);
                Save(session);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Session refresh failed: {e.Message}");
                return false;
            }
        }

        Session = session;
        NakamaService.IsIntentionalDisconnect = false;
        return true;
    }

    public static async void Logout()
    {
        NakamaService.IsIntentionalDisconnect = true;
        InitializeProfile();
        PlayerPrefs.DeleteKey(_sessionKey);
        PlayerPrefs.DeleteKey(_refreshKey);
        PlayerPrefs.DeleteAll();

        if (NakamaService.Socket != null)
        {
            try 
            {
                await NakamaService.Socket.CloseAsync();
                Debug.Log("Nakama socket closed successfully on logout.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to close Nakama socket: {e.Message}");
            }
            finally
            {
                NakamaService.Socket = null;
            }
        }

        Session = null;
    }
}
