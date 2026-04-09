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

        await RefreshProfileData();
        _socialService.RefreshPendingCount();
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
