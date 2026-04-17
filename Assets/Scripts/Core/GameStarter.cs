using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public class GameStarter : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        Debug.Log("GameStarter: Startup Sequence Started.");

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

        // --- Step 1: Restore Session ---
        Debug.Log("GameStarter: Attempting to restore session...");
        Task<bool> restoreTask = NakamaSessionManager.RestoreAsync();
        
        // Wait for Task to finish without blocking WebGL main thread
        while (!restoreTask.IsCompleted) { yield return null; }
        
        bool sessionRestored = false;
        if (restoreTask.IsFaulted)
        {
            Debug.LogError($"GameStarter: Session restoration failed: {restoreTask.Exception}");
        }
        else
        {
            sessionRestored = restoreTask.Result;
        }

        Debug.Log($"GameStarter: Session restored = {sessionRestored}");

        // --- Step 2: Connect Socket (if restored) ---
        if (sessionRestored)
        {
            if (splash != null) splash.UpdateLoadingStatus("Restoring Session...");
            Debug.Log("GameStarter: Connecting Socket...");
            
            Task connectTask = NakamaService.ConnectSocket(NakamaSessionManager.Session);
            while (!connectTask.IsCompleted) { yield return null; }

            if (connectTask.IsFaulted)
            {
                Debug.LogError($"GameStarter: Socket connection failed: {connectTask.Exception}");
            }
            else
            {
                Debug.Log("GameStarter: Initializing SocialService...");
                SocialService.Instance.Initialize(NakamaService.Socket);
                
                Debug.Log("GameStarter: Setting Online Status...");
                Task statusTask = SocialService.Instance.SetOnlineStatus(true);
                while (!statusTask.IsCompleted) { yield return null; }

                // SYNC TUTORIAL STATUS FROM BACKEND
                if (TutorialManager.Instance != null)
                {
                    Debug.Log("GameStarter: Syncing Tutorial Status from Backend...");
                    var tutorialTask = TutorialManager.Instance.CheckTutorialStatusFromBackend();
                    while (!tutorialTask.IsCompleted) { yield return null; }
                }
            }
        }

        // --- Step 3: Wait for minimum splash time ---
        float elapsed = Time.time - startTime;
        if (elapsed < splashMinTime)
        {
            float waitTime = splashMinTime - elapsed;
            Debug.Log($"GameStarter: Waiting for splash delay ({waitTime}s)...");
            yield return new WaitForSeconds(waitTime); // Native WebGL-safe wait
        }

        // --- Step 4: Final Navigation ---
        if (sessionRestored)
        {
            Debug.Log("GameStarter: Finalizing. Moving to MainMenu.");
            UIScreenManager.Instance.Show("MainMenuScreen");
        }
        else
        {
            Debug.Log("GameStarter: No saved session. Moving to LoginScreen.");
            UIScreenManager.Instance.Show("LoginScreen");
        }
    }
}
