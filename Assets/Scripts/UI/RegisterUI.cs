using TMPro;
using UnityEngine;

public class RegisterUI : UIScreen
{
    public TMP_InputField email;
    public TMP_InputField password;
    public TMP_InputField username;
    public TextMeshProUGUI errorText;

    AuthService auth = new();
    ProfileService profile = new();

    void Start()
    {
        if (username != null)
        {
            username.onValueChanged.AddListener(OnUsernameChanged);
        }
    }

   

    private async void OnUsernameChanged(string val)
    {
        if (string.IsNullOrEmpty(val) || val.Length < 3)
        {
            if (errorText != null) errorText.text = "";
            return;
        }

        bool available = await auth.CheckUsername(val);
        if (!available)
        {
            if (errorText != null) errorText.text = "Username already exists.";
        }
        else
        {
            if (errorText != null) errorText.text = "<color=green>Username available!</color>";
        }
    }

    public async void Register()
    {
        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(password.text) || string.IsNullOrEmpty(username.text))
        {
           if(errorText != null) errorText.text = "Please fill in all fields.";
           return;
        }

        
        var result =
            await auth.Register(
                email.text,
                password.text,
                username.text);

        if (!result.IsSuccess)
        {
            Debug.Log(result.Error);
            if(errorText != null) errorText.text = result.Error;
            return;
        }

        GoLogin();
        Debug.Log("Registered Successfully");
    }

    public void GoLogin()
    {
        UIScreenManager.Instance
        .Show("LoginScreen");
    }
}
