using TMPro;
using UnityEngine;

public class LoginUI : UIScreen
{
    public TMP_InputField email;
    public TMP_InputField password;
    public TextMeshProUGUI errorText;
    public UserData userData;

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
    }


    public async void Login()
    {
        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(password.text))
        {
            if(errorText != null) errorText.text = "Please fill in all fields.";
            return;
        }

        var result =
            await auth.LoginEmail(
                email.text,
                password.text);

        if (result.IsSuccess)
        {
            Debug.Log("Login Success");
            await NakamaService.ConnectSocket(NakamaSessionManager.Session);
            
            SocialService.Instance.Initialize(NakamaService.Socket);
            await SocialService.Instance.SetOnlineStatus(true);

            UIScreenManager.Instance.Show("MainMenuScreen");
        }
        else
        {
            Debug.Log(result.Error);
            if(errorText != null) errorText.text = result.Error;
        }
    }

    public void GoSignup()
    {
        UIScreenManager.Instance
        .Show("SignupScreen");
    }
}
