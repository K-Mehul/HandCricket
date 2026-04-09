using UnityEngine;

[RequireComponent(typeof(ProfileView))]
public class ProfileScreen : UIScreen
{
    private ProfileView _view;
    private ProfilePresenter _presenter;
    private bool _isInitialized = false;
    [SerializeField] private UserData _userData;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        _view = GetComponent<ProfileView>();
        _presenter = new ProfilePresenter(_view, GetUserData());
        _isInitialized = true;
    }

    private UserData GetUserData()
    {
        return _userData; 
    }

    public override void Show()
    {
        base.Show();
        Initialize();
        _presenter.RefreshUI();
    }

    private void OnDestroy()
    {
        _presenter?.Cleanup();
    }
}
