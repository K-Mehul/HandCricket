using TMPro;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;

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
   

    private float _debounceTimer = 0f;
    private const float DEBOUNCE_TIME = 0.5f;
    private string _lastCheckedUsername = "";

    private void Update()
    {
        if (_debounceTimer > 0)
        {
            _debounceTimer -= Time.deltaTime;
            if (_debounceTimer <= 0)
            {
                _ = PerformUsernameCheck(username.text);
            }
        }
    }

    private void OnUsernameChanged(string val)
    {
        if (string.IsNullOrEmpty(val) || val.Length < 3)
        {
            if (errorText != null) errorText.text = "";
            return;
        }

        // Delay the check
        _debounceTimer = DEBOUNCE_TIME;
    }

    private async Task PerformUsernameCheck(string val)
    {
        if (val == _lastCheckedUsername) return;
        _lastCheckedUsername = val;

        Debug.Log($"RegisterUI: Checking username availability for '{val}'...");
        bool available = await auth.CheckUsername(val);
        Debug.Log($"RegisterUI: Username '{val}' available = {available}");

        if (!available)
        {
            if (errorText != null)
            {
                errorText.text = "Username already exists.";
                var anim = errorText.GetComponent<UIAnimation>();
                if (anim != null && anim.animationType == UIAnimation.AnimType.Punch) anim.Play();
                else errorText.transform.DOPunchPosition(new Vector3(10, 0, 0), 0.5f);
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
