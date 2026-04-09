using UnityEngine;

public class SplashScreen : UIScreen
{
    private SplashView _view;

    void Awake()
    {
        _view = GetComponent<SplashView>();
    }

    public void UpdateLoadingStatus(string status)
    {
        if (_view != null) 
        {
            _view.SetLoadingText(status);
        }
    }
}
