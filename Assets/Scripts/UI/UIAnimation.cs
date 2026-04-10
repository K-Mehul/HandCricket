using DG.Tweening;
using UnityEngine;

public class UIAnimation : MonoBehaviour
{
    public enum AnimType
    {
        Fade,
        Scale,
        Slide,
        Punch,
        Move
    }

    [Header("Settings")]
    public AnimType animationType = AnimType.Fade;
    public float duration = 0.5f;
    public float delay = 0f;
    public Ease ease = Ease.OutQuint;
    public bool playOnEnable = true;

    [Header("Values")]
    public Vector3 fromValue = Vector3.zero;
    public Vector3 toValue = Vector3.one;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        // Ensure CanvasGroup exists for fade
        if (animationType == AnimType.Fade && canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    [ContextMenu("Play Animation")]
    public void Play()
    {
        Prepare();

        switch (animationType)
        {
            case AnimType.Fade:
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = fromValue.x;
                    canvasGroup.DOFade(toValue.x, duration).SetDelay(delay).SetEase(ease).SetUpdate(true);
                }
                break;

            case AnimType.Scale:
                transform.localScale = fromValue;
                transform.DOScale(toValue, duration).SetDelay(delay).SetEase(ease).SetUpdate(true);
                break;

            case AnimType.Slide:
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = fromValue;
                    rectTransform.DOAnchorPos(toValue, duration).SetDelay(delay).SetEase(ease).SetUpdate(true);
                }
                break;

            case AnimType.Punch:
                transform.DOPunchScale(toValue, duration, 10, 1).SetDelay(delay).SetUpdate(true);
                break;

            case AnimType.Move:
                transform.localPosition = fromValue;
                transform.DOLocalMove(toValue, duration).SetDelay(delay).SetEase(ease).SetUpdate(true);
                break;
        }
    }

    public void Prepare()
    {
        DOTween.Kill(transform);
        if (canvasGroup != null) DOTween.Kill(canvasGroup);

        // Reset to initial state or from value
        switch (animationType)
        {
            case AnimType.Fade:
                if (canvasGroup != null) canvasGroup.alpha = fromValue.x;
                break;
            case AnimType.Scale:
                transform.localScale = fromValue;
                break;
            case AnimType.Slide:
                if (rectTransform != null) rectTransform.anchoredPosition = fromValue;
                break;
        }
    }
}
