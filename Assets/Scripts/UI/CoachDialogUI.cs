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

    private RectTransform _containerRect;
    private bool _isCurrentPositionTop = false;

    private void Awake()
    {
        Instance = this;
        _containerRect = container.GetComponent<RectTransform>();
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() => TutorialManager.Instance?.OnActionCompleted("Continue"));
        }
        Hide();
    }

    /// <summary>
    /// Moves the coach to a safe position based on the highlighted target.
    /// </summary>
    public void Reposition(RectTransform target)
    {
        // if (target == null)
        // {
        //     SetPosition(false); // Default to bottom
        //     return;
        // }

        // // Calculate if the target is in the top or bottom half of the screen
        // Vector3[] corners = new Vector3[4];
        // target.GetWorldCorners(corners);
        // float targetCenterY = (corners[0].y + corners[2].y) * 0.5f;
        
        // // If target is in bottom half (less than half screen height), move Coach to TOP
        // // If target is in top half, move Coach to BOTTOM
        // bool shouldBeTop = targetCenterY < (Screen.height * 0.5f);
        // SetPosition(shouldBeTop);
    }

    private void SetPosition(bool top)
    {
        // if (_containerRect == null) return;
        
        // // Only animate if position actually changes
        // if (_isCurrentPositionTop == top && gameObject.activeInHierarchy) return;
        // _isCurrentPositionTop = top;

        // // Vertical Slide Logic
        // // Top: Anchor (0.5, 1), Pivot (0.5, 1), Y = -50
        // // Bottom: Anchor (0.5, 0), Pivot (0.5, 0), Y = 50
        
        // Vector2 targetAnchor = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        // float targetY = top ? -50f : 50f;

        // _containerRect.DOKill();
        
        // // We set anchors/pivot immediately, then animate anchoredPosition
        // _containerRect.anchorMin = targetAnchor;
        // _containerRect.anchorMax = targetAnchor;
        // _containerRect.pivot = targetAnchor;

        // // Smooth Vertical Slide
        // _containerRect.DOAnchorPosY(targetY, fadeDuration).SetEase(Ease.OutBack);
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
