using UnityEngine;
using System.Threading.Tasks;

public class GameStarter : MonoBehaviour
{

    async void Awake()
    {
        // Initialization
        if (GetComponent<MainThreadDispatcher>() == null)
            gameObject.AddComponent<MainThreadDispatcher>();

        NakamaService.Initialize();

        // Show Splash Screen first
        UIScreenManager.Instance.Show("SplashScreen");
        var splash = UIScreenManager.Instance.GetScreen<SplashScreen>("SplashScreen");
        if (splash != null) splash.UpdateLoadingStatus("Initializing Game...");

        // Start timing the splash screen
        float splashMinTime = 2.5f;
        float startTime = Time.time;

        // Try to restore session from token in background
        bool sessionRestored = await NakamaSessionManager.RestoreAsync();

        if (sessionRestored)
        {
            if (splash != null) splash.UpdateLoadingStatus("Restoring Session...");
            await NakamaService.ConnectSocket(NakamaSessionManager.Session);
            SocialService.Instance.Initialize(NakamaService.Socket);
            await SocialService.Instance.SetOnlineStatus(true);
        }

        // Wait for minimum splash time if needed
        float elapsed = Time.time - startTime;
        if (elapsed < splashMinTime)
        {
            await Task.Delay((int)((splashMinTime - elapsed) * 1000));
        }

        // Final Navigation
        if (sessionRestored)
        {
            Debug.Log("Session Restored successfully.");
            UIScreenManager.Instance.Show("MainMenuScreen");
        }
        else
        {
            Debug.Log("No saved session found. Showing Login Screen.");
            UIScreenManager.Instance.Show("LoginScreen");
        }
    }
}

//    bool online =
//OnlineStatusService.OnlineUsers
//.ContainsKey(friend.UserId)
//&&
//OnlineStatusService.OnlineUsers[
//    friend.UserId];
