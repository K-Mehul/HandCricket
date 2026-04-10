using TMPro;
using UnityEngine;
using DG.Tweening;

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


    public override void Show()
    {
        base.Show();

        if(errorText != null) errorText.text = string.Empty;
        if(username != null) username.text = string.Empty;
        if(password != null) password.text = string.Empty;
        if(email != null) email.text = string.Empty;
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
            if (errorText != null)
            {
                errorText.text = "Username already exists.";
                // Try to find a UIAnimation on the error text or just do it via code for specific feedback
                var anim = errorText.GetComponent<UIAnimation>();
                if (anim != null && anim.animationType == UIAnimation.AnimType.Punch) anim.Play();
                else errorText.transform.DOPunchPosition(new Vector3(10, 0, 0), 0.5f);
                return;
            }
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
            if (errorText != null)
            {
                errorText.text = "Please fill in all fields.";
                // Try to find a UIAnimation on the error text or just do it via code for specific feedback
                var anim = errorText.GetComponent<UIAnimation>();
                if (anim != null && anim.animationType == UIAnimation.AnimType.Punch) anim.Play();
                else errorText.transform.DOPunchPosition(new Vector3(10, 0, 0), 0.5f);
                return;
            }

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
            if (errorText != null)
            {
                errorText.text = result.Error;
                // Try to find a UIAnimation on the error text or just do it via code for specific feedback
                var anim = errorText.GetComponent<UIAnimation>();
                if (anim != null && anim.animationType == UIAnimation.AnimType.Punch) anim.Play();
                else errorText.transform.DOPunchPosition(new Vector3(10, 0, 0), 0.5f);
            }
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
