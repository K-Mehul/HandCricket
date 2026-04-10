using UnityEngine;

public abstract class UIScreen : MonoBehaviour
{
    protected UIAnimationGroup animationGroup;

    protected virtual void Awake()
    {
        animationGroup = GetComponent<UIAnimationGroup>();
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        if (animationGroup != null)
        {
            animationGroup.PlayGroup();
        }
        OnShow();
    }


    public virtual void Hide()
    {
        // For transitions out, we could add PlayExit() logic here in the future
        gameObject.SetActive(false);
        OnHide();
    }


    protected virtual void OnShow() { }

    protected virtual void OnHide() { }
}
