using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SpotlightUI : MonoBehaviour
{
    public static SpotlightUI Instance;

    [Header("Panel References")]
    [SerializeField] private RectTransform panelTop;
    [SerializeField] private RectTransform panelBottom;
    [SerializeField] private RectTransform panelLeft;
    [SerializeField] private RectTransform panelRight;
    
    [Header("Hole Reference")]
    [SerializeField] private RectTransform spotlightHole;
    [SerializeField] private Image pulseRing;
    
    [Header("Settings")]
    [SerializeField] private float transitionDuration = 0.5f;

    private CanvasGroup _canvasGroup;
    private Tween _pulseTween;
    private RectTransform _rectTransform;

    private void Awake()
    {
        Instance = this;
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        if (pulseRing != null) pulseRing.raycastTarget = false;

        // Ensure Tutorial Canvas is always on top
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        Hide();
    }

    private void ForceFullScreen()
    {
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
        _rectTransform.localPosition = Vector3.zero;
        _rectTransform.localScale = Vector3.one;
    }

    public void Show(RectTransform target)
    {
        gameObject.SetActive(true);
        ForceFullScreen();
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (target != null)
        {
            UpdatePanels(target);
            StartPulse();
        }
        else
        {
            ShowFullOverlay();
        }
    }

    public void ShowFullOverlay()
    {
        gameObject.SetActive(true);
        ForceFullScreen();
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        StopPulse();
        
        // Make panelTop cover EVERYTHING
        SetPanelAnchors(panelTop, Vector2.zero, Vector2.one);
        // Hide others
        SetPanelAnchors(panelBottom, Vector2.zero, Vector2.zero);
        SetPanelAnchors(panelLeft, Vector2.zero, Vector2.zero);
        SetPanelAnchors(panelRight, Vector2.zero, Vector2.zero);
        
        // Hide hole
        spotlightHole.anchoredPosition = new Vector2(-5000, -5000);
    }

    private void UpdatePanels(RectTransform target)
    {
        if (target == null) return;

        // 1. Get Target Rect in local space of this full-screen container
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        Vector2 localMin, localMax;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, RectTransformUtility.WorldToScreenPoint(cam, corners[0]), cam, out localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, RectTransformUtility.WorldToScreenPoint(cam, corners[2]), cam, out localMax);

        // 2. Add Padding (10%)
        float width = localMax.x - localMin.x;
        float height = localMax.y - localMin.y;
        localMin -= new Vector2(width * 0.1f, height * 0.1f);
        localMax += new Vector2(width * 0.1f, height * 0.1f);

        // 3. Update Hole visual Container
        Vector2 center = (localMin + localMax) * 0.5f;
        Vector2 size = new Vector2(localMax.x - localMin.x, localMax.y - localMin.y);
        spotlightHole.anchoredPosition = center;
        spotlightHole.sizeDelta = size;
        if (pulseRing != null) pulseRing.rectTransform.sizeDelta = size;

        // 4. Position Panels using Normalized Anchors (Bulletproof for all resolutions)
        // Convert local coordinates to 0..1 range relative to parent rect
        Rect r = _rectTransform.rect;
        float nMinX = Mathf.InverseLerp(r.xMin, r.xMax, localMin.x);
        float nMaxX = Mathf.InverseLerp(r.xMin, r.xMax, localMax.x);
        float nMinY = Mathf.InverseLerp(r.yMin, r.yMax, localMin.y);
        float nMaxY = Mathf.InverseLerp(r.yMin, r.yMax, localMax.y);

        // TOP
        SetPanelAnchors(panelTop, new Vector2(0, nMaxY), new Vector2(1, 1));
        // BOTTOM
        SetPanelAnchors(panelBottom, new Vector2(0, 0), new Vector2(1, nMinY));
        // LEFT
        SetPanelAnchors(panelLeft, new Vector2(0, nMinY), new Vector2(nMinX, nMaxY));
        // RIGHT
        SetPanelAnchors(panelRight, new Vector2(nMaxX, nMinY), new Vector2(1, nMaxY));
    }

    private void SetPanelAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    public void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        StopPulse();
    }

    private void StartPulse()
    {
        StopPulse();
        if (pulseRing == null) return;

        pulseRing.gameObject.SetActive(true);
        pulseRing.transform.localScale = Vector3.one;
        Color c = pulseRing.color;
        c.a = 0.5f;
        pulseRing.color = c;

        _pulseTween = DOTween.Sequence()
            .Append(pulseRing.transform.DOScale(1.05f, 0.8f))
            .Join(pulseRing.DOFade(0f, 0.8f))
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopPulse()
    {
        _pulseTween?.Kill();
        if (pulseRing != null) pulseRing.gameObject.SetActive(false);
    }
}
