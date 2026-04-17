using System.Threading.Tasks;
using UnityEngine;

public class MainMenuPresenter
{
    private readonly MainMenuView _view;
    private readonly UserData _userData;
    private readonly SocialService _socialService;
    private readonly UIScreenManager _screenManager;

    public MainMenuPresenter(MainMenuView view, UserData userData)
    {
        _view = view;
        _userData = userData;
        _socialService = SocialService.Instance;
        _screenManager = UIScreenManager.Instance;

        SubscribeToViewEvents();
        SubscribeToDataEvents();
    }

    private void SubscribeToViewEvents()
    {
        _view.OnPlayClicked += () => _screenManager.Show("LobbyScreen");
        _view.OnFriendsClicked += () => _screenManager.Show("SocialScreen");
        _view.OnProfileClicked += () => _screenManager.Show("ProfileScreen");
        _view.OnLeaderboardClicked += () => _screenManager.Show("LeaderboardScreen");
        _view.OnLogoutClicked += HandleLogout;
    }

    private void SubscribeToDataEvents()
    {
        _userData.OnDataChanged += UpdateProfileUI;
        _socialService.OnSocialUpdate += UpdateSocialBadge;
    }

    public void Cleanup()
    {
        _userData.OnDataChanged -= UpdateProfileUI;
        if (_socialService != null)
            _socialService.OnSocialUpdate -= UpdateSocialBadge;
    }

    public async Task Initialize()
    {
        UpdateProfileUI();
        UpdateSocialBadge();

        // Check if this is the first time showing the bonus for this session
        string bonusKey = "BonusAnimated_" + NakamaSessionManager.Session?.UserId;
        bool isNewForBonus = NakamaSessionManager.Session != null && !PlayerPrefs.HasKey(bonusKey);

        await RefreshProfileData();
        
        if (isNewForBonus && (_userData.coins > 0 || _userData.xp > 0))
        {
            // Reset UI briefly to 0 for the animation effect
            _view.UpdateProfile(_userData.username, _userData.level, 0, 0);
            _view.AnimateProfileStats(_userData.coins, _userData.xp);
            
            PlayerPrefs.SetInt(bonusKey, 1);
            PlayerPrefs.Save();
        }

        _socialService.RefreshPendingCount();
        
        // Tutorial Check (Local/Cached only)
        CheckTutorialStart();
    }

    private void CheckTutorialStart()
    {
        // Simple logic for first time user: Matches == 0
        if (_userData.Matches == 0 && !PlayerPrefs.HasKey("TutorialCompleted"))
        {
            if (TutorialManager.Instance != null && _view.playButtonRect != null)
            {
                TutorialManager.Instance.RegisterTarget("PlayButton", _view.playButtonRect);
                TutorialManager.Instance.StartTutorial();
            }
        }
    }

    private void UpdateProfileUI()
    {
        _view.UpdateProfile(_userData.username, _userData.level, _userData.coins, _userData.xp);
    }

    private void UpdateSocialBadge()
    {
        _view.UpdateSocialBadge(_socialService.PendingCount);
    }

    public async Task RefreshProfileData()
    {
        if (NakamaSessionManager.Session == null) return;

        try
        {
            var account = await NakamaService.Client.GetAccountAsync(NakamaSessionManager.Session);
            _userData.username = account.User.Username;
            _userData.SetFromWallet(account.Wallet);
            
            await NakamaService.FetchUserStatsAsync(NakamaSessionManager.Session, _userData);
            
            // View updates automatically via OnDataChanged
        }
        catch (System.Exception e)
        {
            Debug.LogError("MainMenuPresenter: Failed to refresh account: " + e.Message);
        }
    }

    private void HandleLogout()
    {
        NakamaSessionManager.Logout();
        _screenManager.Show("LoginScreen");
    }
}
