using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class CoachDialogUI : MonoBehaviour
{
    public static CoachDialogUI Instance;

    [Header("UI References")]
    [SerializeField] private CanvasGroup container;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private Image coachAvatar;
    
    [SerializeField] private Button continueButton;
    
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float textSpeed = 0.05f;

    public event Action OnMessageComplete;
    private bool _shouldShowContinue;

    private Tween _textTween;

    private void Awake()
    {
        Instance = this;
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() => TutorialManager.Instance?.OnActionCompleted("Continue"));
        }
        Hide();
    }

    public void ShowMessage(string message, bool showContinueButton = true)
    {
        _shouldShowContinue = showContinueButton;
        gameObject.SetActive(true);
        container.DOKill();
        container.DOFade(1f, fadeDuration);

        if (continueButton != null) continueButton.gameObject.SetActive(false);

        // Typewriter effect
        _textTween?.Kill();
        dialogText.text = "";
        int characterCount = 0;
        _textTween = DOTween.To(() => characterCount, x => {
            characterCount = x;
            dialogText.text = message.Substring(0, characterCount);
        }, message.Length, message.Length * textSpeed)
        .SetEase(Ease.Linear)
        .OnComplete(() => {
            if (_shouldShowContinue && continueButton != null) continueButton.gameObject.SetActive(true);
            OnMessageComplete?.Invoke();
        });
    }

    public void Hide()
    {
        container.DOKill();
        container.DOFade(0f, fadeDuration).OnComplete(() => gameObject.SetActive(false));
    }

    public void SetAvatar(Sprite sprite)
    {
        if (coachAvatar != null) coachAvatar.sprite = sprite;
    }
}
