using TMPro;
using UnityEngine;

public class LoginUI : UIScreen
{
    public TMP_InputField email;
    public TMP_InputField password;
    public TextMeshProUGUI errorText;
    public UnityEngine.UI.Button loginButton;
    public UserData userData;
    
    private bool _isLoggingIn = false;
    private string _originalLoginText = "LOGIN";

    AuthService auth = new();

    void Start()
    {
        userData?.Clear();
        // Redundant restoration removed. Handled by GameStarter.
    }

    public override void Show()
    {
        base.Show();

        if(email != null) email.text = string.Empty;
        if(password != null) password.text = string.Empty;
        if(errorText != null) errorText.text = string.Empty;
        
        SetLoadingState(false);
    }


    public async void Login()
    {
        if (_isLoggingIn) return;

        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(password.text))
        {
            if (errorText != null) errorText.text = "Please fill in all fields.";
            return;
        }

        SetLoadingState(true);

        try
        {
            var result = await auth.LoginEmail(email.text, password.text);

            if (result.IsSuccess)
            {
                Debug.Log("Login Success");
                await NakamaService.ConnectSocket(NakamaSessionManager.Session);

                SocialService.Instance.Initialize(NakamaService.Socket);
                await SocialService.Instance.SetOnlineStatus(true);

                // Sync tutorial only once upon login
                if (TutorialManager.Instance != null)
                {
                    await TutorialManager.Instance.CheckTutorialStatusFromBackend();
                }

                UIScreenManager.Instance.Show("MainMenuScreen");
            }
            else
            {
                Debug.Log(result.Error);
                if (errorText != null) errorText.text = result.Error;
                SetLoadingState(false); // Only restore if failed, as screen will change on success
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Login Error: {e.Message}");
            if (errorText != null) errorText.text = "An unexpected error occurred.";
            SetLoadingState(false);
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        _isLoggingIn = isLoading;
        if (loginButton != null)
        {
            loginButton.interactable = !isLoading;
            var btnText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (isLoading)
                {
                    _originalLoginText = btnText.text;
                    btnText.text = "LOGGING IN...";
                }
                else
                {
                    btnText.text = _originalLoginText;
                }
            }
        }
    }

    public void GoSignup()
    {
        UIScreenManager.Instance
        .Show("SignupScreen");
    }
}
