using UnityEngine;

public class SocialScreen : UIScreen
{
    public SocialView view;
    private SocialPresenter _presenter;

    void Awake()
    {
        if (view == null) view = GetComponent<SocialView>();
        _presenter = new SocialPresenter(view);
    }

    protected override void OnShow()
    {
        base.OnShow();
        view.ClearInputs();
        _presenter.RefreshList();
    }

    void OnDestroy()
    {
        _presenter?.Cleanup();
    }
}
