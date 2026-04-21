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
    public UnityEngine.UI.Button registerButton;
    
    private bool _isRegistering = false;
    private string _originalRegisterText = "REGISTER";

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
        
        SetLoadingState(false);
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
        if (_isRegistering) return;

        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(password.text) || string.IsNullOrEmpty(username.text))
        {
            if (errorText != null)
            {
                errorText.text = "Please fill in all fields.";
                var anim = errorText.GetComponent<UIAnimation>();
                if (anim != null && anim.animationType == UIAnimation.AnimType.Punch) anim.Play();
                else errorText.transform.DOPunchPosition(new Vector3(10, 0, 0), 0.5f);
                return;
            }
            return;
        }

        SetLoadingState(true);

        try
        {
            var result = await auth.Register(email.text, password.text, username.text);

            if (!result.IsSuccess)
            {
                Debug.Log(result.Error);
                if (errorText != null)
                {
                    errorText.text = result.Error;
                    var anim = errorText.GetComponent<UIAnimation>();
                    if (anim != null && anim.animationType == UIAnimation.AnimType.Punch) anim.Play();
                    else errorText.transform.DOPunchPosition(new Vector3(10, 0, 0), 0.5f);
                }
                SetLoadingState(false);
                return;
            }

            Debug.Log("Registered Successfully. Auto-logging in...");
            
            // Connect socket with the session (linking or fresh)
            await NakamaService.ConnectSocket(NakamaSessionManager.Session);

            if (SocialService.Instance != null)
            {
                SocialService.Instance.Initialize(NakamaService.Socket);
                _ = SocialService.Instance.SetOnlineStatus(true);
            }

            // Sync tutorial status
            if (TutorialManager.Instance != null)
            {
                _ = TutorialManager.Instance.CheckTutorialStatusFromBackend();
            }

            UIScreenManager.Instance.Show("MainMenuScreen");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Register Error: {e.Message}");
            if (errorText != null) errorText.text = "An unexpected error occurred.";
            SetLoadingState(false);
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        _isRegistering = isLoading;
        if (registerButton != null)
        {
            registerButton.interactable = !isLoading;
            var btnText = registerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (isLoading)
                {
                    _originalRegisterText = btnText.text;
                    btnText.text = "REGISTERING...";
                }
                else
                {
                    btnText.text = _originalRegisterText;
                }
            }
        }
    }

    public void GoLogin()
    {
        UIScreenManager.Instance
        .Show("LoginScreen");
    }
}
