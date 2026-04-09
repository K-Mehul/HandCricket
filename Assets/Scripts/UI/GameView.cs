using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class GameView : MonoBehaviour
{
    [Header("Top Bar / Status (Legacy - Will be hidden)")]
    public TextMeshProUGUI timerText;
    public GameObject background;
    

    [Header("Footer Match Bar (Main Info)")]
    public TextMeshProUGUI batsmanNameText;
    public TextMeshProUGUI bowlerNameText;
    public TextMeshProUGUI footerOversText;
    public TextMeshProUGUI footerScoreText;
    public TextMeshProUGUI footerTargetText;

    private bool _isBannerPlaying = false;
    public bool IsBannerPlaying => _isBannerPlaying;

    [Header("Toss UI")]
    public GameObject tossPanel;
    public TextMeshProUGUI tossStatusText;
    public Button headsButton;
    public Button tailsButton;

    [Header("Decision UI")]
    public GameObject decisionPanel;
    public Button batButton;
    public Button bowlButton;

    [Header("Gameplay (Number Buttons)")]
    public GameObject gameplayInputPanel;
    public Button[] numberButtons; // Buttons for 1, 2, 3, 4, 5, 6
    public GameObject numberButtonPanel;

    [Header("Animations & Banners")]
    public GameObject wicketBanner;
    public GameObject fourBanner;
    public GameObject sixBanner;
    public GameObject roleBannerPanel;
    public TextMeshProUGUI roleBannerText;
    public GameObject phaseOverlay; // Big banner for phase transitions
    public TextMeshProUGUI phaseText;
    
    [Header("Game Info")]
    public TextMeshProUGUI totalOversText;

    [Header("Ball History")]
    public Transform ballHistoryContainer;
    public GameObject ballIconPrefab;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultDetailsText;
    public GameObject levelUpEffect;
    public Button backToLobbyButton;
    
    [Header("3D Animation References")]
    public Animator batsmanAnimator;
    public Animator bowlerAnimator;
    public Transform ballTransform;
    public Transform pitchPoint;
    public Transform wicketPoint;
    public Transform boundaryPoint;
    public Transform bowlerHandPoint;
    public Transform batsmanContactPoint;
    public GameObject stadiumEnvironment;

    [Header("Wicket Props")]
    public Transform[] stumps;
    public Transform[] bails;
    private Vector3[] _stumpInitPos;
    private Quaternion[] _stumpInitRot;
    private Vector3[] _bailInitPos;
    private Quaternion[] _bailInitRot;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource crowdSource;
    public AudioClip batHitClip;
    public AudioClip wicketClip;
    public AudioClip crowdCheerClip;
    public AudioClip ambientCrowdClip;

    private List<GameObject> _ballIcons = new List<GameObject>();
    private bool _isBallReleased = false;
    private bool _isContactMade = false;


    [Header("Simulation Tuning")]
    public float simSpeedMultiplier = 1.5f;

    // Current Match Simulation State
    private int _currentBatVal;
    private int _currentBowlVal;
    private Action _currentOnComplete;
    private Tween _releaseFallback;

    private float _defaultFOV;
    private Camera _mainCam;
    // Events for the Presenter
    public event Action<string> OnTossCalled; // "HEADS" or "TAILS"
    public event Action<string> OnDecisionMade; // "BAT" or "BOWL"
    public event Action<int> OnNumberPicked;
    public event Action OnBackToLobby;


    private Tween _timerPulseTween;
    private Vector3 _originalTimerScale;

    void Start()
    {
        _mainCam = Camera.main;
        if (_mainCam != null) _defaultFOV = _mainCam.fieldOfView;

        headsButton.onClick.AddListener(() => PulseButton(headsButton, () => OnTossCalled?.Invoke("HEADS")));
        tailsButton.onClick.AddListener(() => PulseButton(tailsButton, () => OnTossCalled?.Invoke("TAILS")));
        batButton.onClick.AddListener(() => PulseButton(batButton, () => OnDecisionMade?.Invoke("BAT")));
        bowlButton.onClick.AddListener(() => PulseButton(bowlButton, () => OnDecisionMade?.Invoke("BOWL")));
        backToLobbyButton.onClick.AddListener(() => PulseButton(backToLobbyButton, () => OnBackToLobby?.Invoke()));

        if (timerText != null)
        {
            // If scale is zero → fix it
            if (timerText.transform.localScale == Vector3.zero)
            {
                timerText.transform.localScale = Vector3.one;
            }

            _originalTimerScale = timerText.transform.localScale;
        }

        for (int i = 0; i < numberButtons.Length; i++)
        {
            int val = i + 1;
            Button btn = numberButtons[i];
            btn.onClick.AddListener(() => PulseButton(btn, () => OnNumberPicked?.Invoke(val)));
        }

        
        CaptureWicketInitialState();
    }

    public void ShowPanel(string panelName)
    {
        bool isGame = panelName == "Game";

        _isBannerPlaying = false; // Forceful reset on panel switch

        // Toggle major panels
        tossPanel.SetActive(panelName == "Toss");
        decisionPanel.SetActive(panelName == "Decision");
        gameplayInputPanel.SetActive(isGame);
        resultPanel.SetActive(panelName == "Result");
        
        if (phaseOverlay != null)
        {
            phaseOverlay.transform.DOKill();
            phaseOverlay.SetActive(false);
        }

        background.SetActive(!isGame);
        SetStadiumActive(isGame);
    }

    public void SetTossInteractable(bool interactable, string statusMsg = "")
    {
        headsButton.interactable = interactable;
        tailsButton.interactable = interactable;
        if (!string.IsNullOrEmpty(statusMsg)) tossStatusText.text = statusMsg;
    }

    public void SetInputInteractivity(bool active)
    {
        //foreach (var btn in numberButtons) btn.interactable = active;

        numberButtonPanel.SetActive(active);

    }

    public void UpdateScoreDisplay(string scoreInfo, string statusMessage, int currentBalls = -1, int maxOvers = -1, int target = -1)
    {
        // Update Footer Score
        if (footerScoreText != null)
        {
            if (footerScoreText.text != scoreInfo)
            {
                footerScoreText.text = scoreInfo;
                // JUICE: Punch the score when it changes
                footerScoreText.transform.DOKill();
                footerScoreText.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5, 0.5f);
            }
        }
        
        // Update Footer Target
        if (footerTargetText != null)
        {
            if (target > 0) footerTargetText.text = $"TARGET: {target}";
            else footerTargetText.text = "";
        }

        // Update Footer Overs
        if (currentBalls >= 0 && maxOvers >= 0)
        {
            int overs = currentBalls / 6;
            int ballsInOver = currentBalls % 6;
            
            // If we are at the end of an over (e.g. 6 balls), it should show as 1.0, not 0.6
            // But usually in cricket 0.6 is 1.0. Let's ensure standard formatting.
            if (footerOversText != null) footerOversText.text = $"OVERS: {overs}.{ballsInOver} / {maxOvers}";
        }
    }

    public void UpdateMatchInfo(int currentBalls, int maxOvers)
    {
        if (footerOversText != null)
        {
            int overs = currentBalls / 6;
            int ballsInOver = currentBalls % 6;
            footerOversText.text = $"OVERS: {overs}.{ballsInOver} / {maxOvers}";
        }
    }

    public void UpdateRole(string role)
    {
        // if (footerRoleText != null) footerRoleText.text = role.ToUpper();
    }

    public void ShowYourTurn(bool show)
    {
        // if (yourTurnIndicator != null) yourTurnIndicator.SetActive(show);
    }

    public void ShowRoleBanner(string roleName)
    {
        if (roleBannerPanel == null || roleBannerText == null) return;
        
        roleBannerText.text = $"YOU ARE {roleName.ToUpper()}";
        
        roleBannerPanel.SetActive(true);
        CanvasGroup group = roleBannerPanel.GetComponent<CanvasGroup>();
        if (group == null) group = roleBannerPanel.AddComponent<CanvasGroup>();
        
        group.alpha = 0;
        roleBannerPanel.transform.localScale = Vector3.one * 0.8f;
        _isBannerPlaying = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(group.DOFade(1, 0.4f));
        seq.Join(roleBannerPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        seq.AppendInterval(1.8f);
        seq.Append(group.DOFade(0, 0.4f));
        seq.OnComplete(() => {
            _isBannerPlaying = false;
            roleBannerPanel.SetActive(false);
        });
    }

    public void UpdateNames(string batsmanName, string bowlerName)
    {
        if (batsmanNameText != null) batsmanNameText.text = batsmanName;
        if (bowlerNameText != null) bowlerNameText.text = bowlerName;
    }

    public void PlayScoringAnimation(string type, Action onComplete = null)
    {
        // Don't show scoring banners if match is over and result screen is up
        if (resultPanel != null && resultPanel.activeInHierarchy) return;

        GameObject banner = null;
        if (type == "WICKET") banner = wicketBanner;
        else if (type == "FOUR") banner = fourBanner;
        else if (type == "SIX") banner = sixBanner;

        if (banner == null)
        {
            onComplete?.Invoke();
            return;
        }

        _isBannerPlaying = true;
        banner.SetActive(true);
        banner.transform.localScale = Vector3.zero;
        banner.transform.localRotation = Quaternion.identity;
        
        Sequence seq = DOTween.Sequence();
        
        if (type == "WICKET")
        {
            // SAD: Slow, heavy, droopy animation
            seq.Append(banner.transform.DOScale(1f, 0.6f).SetEase(Ease.OutSine));
            seq.Join(banner.transform.DOPunchPosition(new Vector3(0, -10, 0), 0.5f, 2, 0.5f));
            seq.Join(banner.transform.DORotate(new Vector3(0, 0, 5), 0.5f).SetEase(Ease.InOutSine));
            seq.AppendInterval(0.8f);
            seq.Append(banner.transform.DOScale(0f, 0.4f).SetEase(Ease.InSine));
        }
        else
        {
            // ENJOYFUL: Bouncy, energetic animation for FOUR/SIX
            float punchAmount = (type == "SIX") ? 1.4f : 1.25f;
            seq.Append(banner.transform.DOScale(punchAmount, 0.3f).SetEase(Ease.OutBack));
            seq.Append(banner.transform.DOPunchRotation(new Vector3(0, 0, 15), 0.4f, 10, 1f));
            seq.Join(banner.transform.DOScale(1f, 0.2f).SetDelay(0.1f));
            seq.AppendInterval(0.6f);
            seq.Append(banner.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        }

        seq.OnComplete(() => {
            banner.SetActive(false);
            _isBannerPlaying = false;
            onComplete?.Invoke();
        });

        // Audio Reactions
        // Wicket sound is now handled in OnReleaseBall impact callback
        if (type != "WICKET") PlaySFX(crowdCheerClip);

    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }

    public void ShowPhaseBanner(string text, float duration = 2f, Action onComplete = null)
    {
        if (phaseOverlay == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        // Cleanup previous animations to prevent ghosting
        phaseOverlay.transform.DOKill(true);
        
        _isBannerPlaying = true;
        phaseOverlay.SetActive(true);
        phaseText.text = text;
        phaseOverlay.transform.localScale = Vector3.zero;
        
        phaseOverlay.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        
        if (duration > 0)
        {
            DOVirtual.DelayedCall(duration, () => {
                phaseOverlay.transform.DOScale(0f, 0.5f).OnComplete(() => {
                    phaseOverlay.SetActive(false);
                    _isBannerPlaying = false;
                    onComplete?.Invoke();
                });
            });
        }
        else
        {
            // If duration is 0, it stays up. Not used for standard ones.
            _isBannerPlaying = false;
        }
    }

    public void UpdateBallHistory(string outcome)
    {
        if (ballHistoryContainer == null || ballIconPrefab == null) return;

        // The auto-clear is now handled in GamePresenter or manually via ClearBallHistory
        // but as a safety, if we have 6, we clear to make room for the new one.
        if (_ballIcons.Count >= 6)
        {
            ClearBallHistory();
        }

        GameObject icon = Instantiate(ballIconPrefab, ballHistoryContainer);
        var text = icon.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = outcome;

        icon.transform.localScale = Vector3.zero;
        icon.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        
        _ballIcons.Add(icon);
    }

    public void ClearBallHistory()
    {
        foreach (var icon in _ballIcons) if(icon != null) Destroy(icon);
        _ballIcons.Clear();
    }

    
    public void ShowResult(string title, string details, bool leveledUp, int newLevel)
    {
        ShowPanel("Result");
        if (resultTitleText != null) resultTitleText.text = title;
        
        string fullDetails = details;
        if (leveledUp) fullDetails += $"\n\nLEVEL UP! Now Level {newLevel}!";
        if (resultDetailsText != null) resultDetailsText.text = fullDetails;

        if (leveledUp && levelUpEffect != null)
        {
            levelUpEffect.SetActive(true);
            DOVirtual.DelayedCall(3f, () => levelUpEffect.SetActive(false));
        }
    }

    // --- Timer & UX Helpers ---

    public void UpdateTimer(float seconds, bool visible)
    {
        if (timerText == null) return;
        timerText.gameObject.SetActive(visible);

        if (!visible) 
        {
            StopPulse();
            return;
        }

        if (timerText.transform.localScale == Vector3.zero)
        {
            timerText.transform.localScale = _originalTimerScale == Vector3.zero
            ? Vector3.one
            : _originalTimerScale;
        }

        int totalSeconds = Mathf.CeilToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{secs:00}";

        if (totalSeconds <= 3) 
        {
            timerText.color = Color.red;
            StartPulse();
        }
        else 
        {
            timerText.color = Color.white;
            StopPulse();
        }
    }

    private void StartPulse()
    {
        if (_timerPulseTween != null && _timerPulseTween.IsActive()) return;
        timerText.transform.localScale = _originalTimerScale;

        _timerPulseTween = timerText.transform
            .DOScale(_originalTimerScale * 1.3f, 0.3f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

    }

    private void StopPulse()
    {
        if (_timerPulseTween != null)
        {
            _timerPulseTween.Kill();
            _timerPulseTween = null;
        }

        if (timerText != null)
        {
            timerText.transform.localScale = _originalTimerScale == Vector3.zero ? Vector3.one : _originalTimerScale;
        }
    }


    private void PulseButton(Button btn, Action callback)
    {
        if (btn == null) return;
        
        // Immediate visual feedback
        btn.transform.DOKill();
        btn.transform.DOPunchScale(Vector3.one * 0.12f, 0.15f, 5, 0.5f);
        
        DOVirtual.DelayedCall(0.5f,()=>{
            callback?.Invoke();
        });
    }

    public void FlashAutoSelection(string choice)
    {
        Button targetBtn = null;
        
        // Match choice to button
        if (choice == "HEADS") targetBtn = headsButton;
        else if (choice == "TAILS") targetBtn = tailsButton;
        else if (choice == "BAT") targetBtn = batButton;
        else if (choice == "BOWL") targetBtn = bowlButton;
        else if (int.TryParse(choice, out int num))
        {
            if (num >= 1 && num <= 6) targetBtn = numberButtons[num - 1];
        }

        if (targetBtn != null)
        {
            targetBtn.image.DOColor(Color.yellow, 0.2f).SetLoops(4, LoopType.Yoyo).OnComplete(() => {
                targetBtn.image.color = Color.white;
            });
        }
        
        // UpdateScoreDisplay("%SAME%", $"Auto-Selected: {choice}!");
    }

    public void PlayPerfectSimulation(int batVal, int bowlVal, Action onComplete)
    {
        ResetAnimators();
        
        if (batsmanAnimator == null || bowlerAnimator == null || ballTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Store state for reactive release
        _currentBatVal = batVal;
        _currentBowlVal = bowlVal;
        _currentOnComplete = onComplete;
        _isBallReleased = false;
        _isContactMade = false;

        // 0. Initial State: Keep UI but Disable Buttons
        SetInputInteractivity(false);
        ballTransform.gameObject.SetActive(false);

        // 1. Bowler Run-up Start
        if (batsmanAnimator != null) batsmanAnimator.speed = 1.0f * simSpeedMultiplier;
        if (bowlerAnimator != null) bowlerAnimator.speed = 1.0f * simSpeedMultiplier;

        // CINEMATIC: Tension Zoom In
        if (_mainCam != null)
        {
            _mainCam.DOKill();
            _mainCam.DOFieldOfView(_defaultFOV - 6f, 2.0f / simSpeedMultiplier).SetEase(Ease.InOutSine);
        }

        bowlerAnimator.SetTrigger("RunUp");
        
        // 2. Set Fallback (strictly for safety if Animation Event fails)
        if (_releaseFallback != null) _releaseFallback.Kill();
        _releaseFallback = DOVirtual.DelayedCall(4.0f, () => {
            if (!_isBallReleased)
            {
                Debug.LogWarning("Bowler Release Fallback Triggered! Ensure Animation Event 'OnReleaseBall' is added to the Bowl/RunUp clip.");
                OnReleaseBall();
            }
        });
    }

    public void OnReleaseBall()
    {
        if (_isBallReleased || ballTransform == null) return;
        _isBallReleased = true;
        if (_releaseFallback != null) _releaseFallback.Kill();

        // Position and Show Ball
        if (bowlerHandPoint != null) ballTransform.position = bowlerHandPoint.position;
        ballTransform.gameObject.SetActive(true);

        // Calculate Trajectory parameters
        bool isWicket = (_currentBatVal == _currentBowlVal);
        string shotTrigger = "Defense";
        string missTrigger = ""; // For two-stage wicket
        float ballSpeed = 1.0f;

        if (isWicket)
        {
            // Randomly choose a shot to "miss"
            string[] missShots = { "Defense", "Drive", "Lofted" };
            missTrigger = missShots[UnityEngine.Random.Range(0, missShots.Length)];
            shotTrigger = missTrigger; // Play miss first
            ballSpeed = 1.0f;
        }
        else
        {
            if (_currentBatVal >= 6) { shotTrigger = "Lofted"; ballSpeed = 1.0f; }
            else if (_currentBatVal >= 4) { shotTrigger = "Drive"; ballSpeed = 1.4f; }
            else { shotTrigger = "Defense"; ballSpeed = 1.8f; }
        }

        // START REACTIVE TRAJECTORY SEQUENCE
        Sequence seq = DOTween.Sequence();

        // Segment 1: Hand to Pitch (Bounce)
        seq.Append(ballTransform.DOJump(pitchPoint.position, 0.4f, 1, 0.65f / simSpeedMultiplier).SetEase(Ease.InQuad));

        // Segment 2: Pitch to Batsman (Hit Point)
        seq.AppendCallback(() => batsmanAnimator.SetTrigger(shotTrigger));
        
        Vector3 batsmanTarget = (batsmanContactPoint != null) ? batsmanContactPoint.position : ballTransform.position;
        seq.Append(ballTransform.DOJump(batsmanTarget, 0.6f, 1, 0.45f / simSpeedMultiplier).SetEase(Ease.Linear));

        seq.AppendCallback(() => {
            if (!_isContactMade) OnBatsmanContact();
        });
        seq.AppendInterval(0.05f / simSpeedMultiplier); // Hit-Stop: Momentary pause for impact feel

        // Segment 3: Result Trajectory (To boundary or wicket)
        float jumpPower = 0f;
        int numJumps = 1;
        float duration = ballSpeed / simSpeedMultiplier;
        Ease flightEase = Ease.OutSine;
        Vector3 finalTarget = boundaryPoint.position;

        if (isWicket)
        {
            jumpPower = 0.2f;
            duration = 0.5f / simSpeedMultiplier;
            flightEase = Ease.Linear;
            finalTarget = wicketPoint.position;
        }
        else if (_currentBatVal >= 6)
        {
            // SIX: High lofted shot, direct to boundary
            jumpPower = 4.0f;
            numJumps = 1;
            duration = 1.6f / simSpeedMultiplier;
            flightEase = Ease.OutQuad;
        }
        else if (_currentBatVal >= 4)
        {
            // FOUR / FIVE: Grounded drive, one or two bounces
            jumpPower = 1.0f;
            numJumps = 2; // Taking one or two bounce
            duration = 1.4f / simSpeedMultiplier;
            flightEase = Ease.OutSine;
        }
        else
        {
            // GROUNDED SHOTS (1, 2, 3 runs)
            jumpPower = 0.3f + (_currentBatVal * 0.1f); // Low jumps
            numJumps = 3; // Multiple bounces
            duration = (1.2f + (_currentBatVal * 0.2f)) / simSpeedMultiplier;
            flightEase = Ease.OutSine;

            // Randomized Direction (-30 to +30 degrees)
            Vector3 direction = (boundaryPoint.position - batsmanTarget).normalized;
            float randomAngle = UnityEngine.Random.Range(-30f, 30f);
            Vector3 rotatedDirection = Quaternion.Euler(0, randomAngle, 0) * direction;

            // Scaled Distance
            float distanceMult = (_currentBatVal == 1) ? 0.35f : (_currentBatVal == 2) ? 0.55f : 0.75f;
            float totalDistance = Vector3.Distance(batsmanTarget, boundaryPoint.position) * distanceMult;
            finalTarget = batsmanTarget + rotatedDirection * totalDistance;
        }

        if (isWicket)
        {
            // SLOW MOTION START: As the ball approaches the wickets
            seq.AppendCallback(() => {
                Time.timeScale = 0.4f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            });

            seq.Append(ballTransform.DOMove(finalTarget, duration).SetEase(flightEase));
            
            // Trigger Stumped and Knock down wickets
            seq.AppendCallback(() => {
                // RESET TIME SCALE ON IMPACT
                Time.timeScale = 1.0f;
                Time.fixedDeltaTime = 0.02f;

                PlaySFX(wicketClip); // Play sound immediately on impact
                batsmanAnimator.SetTrigger("Stumped");
                AnimateWicketImpact();
                
                // Strong shake on hit
                ShakeCamera(0.45f, 0.4f);
            });
        }
        else
        {
            seq.Append(ballTransform.DOJump(finalTarget, jumpPower, numJumps, duration).SetEase(flightEase));
        }

        // 4. Follow-through & Synchronized UI Return
        // Wait for batsman follow-through animation to finish before calling back
        seq.AppendInterval(2.2f / simSpeedMultiplier);

        // 5. Completion
        seq.OnComplete(() => {
            ResetAnimators();
            
            // Hide ball after simulation is finished
            if (ballTransform != null) ballTransform.gameObject.SetActive(false);
            
            _currentOnComplete?.Invoke();
        });
    }

    public void SetStadiumActive(bool active)
    {
        if (stadiumEnvironment != null) stadiumEnvironment.SetActive(active);
        
        if (crowdSource != null)
        {
            if (active)
            {
                if (!crowdSource.isPlaying)
                {
                    crowdSource.clip = ambientCrowdClip;
                    crowdSource.loop = true;
                    crowdSource.playOnAwake = false;
                    crowdSource.Play();
                }
                SetCrowdVolume(0.4f, 1.5f);
            }
            else
            {
                SetCrowdVolume(0f, 1f, () => crowdSource.Stop());
            }
        }
    }

    public void SetCrowdVolume(float targetVolume, float duration, Action onComplete = null)
    {
        if (crowdSource == null) return;
        crowdSource.DOKill();
        var tween = crowdSource.DOFade(targetVolume, duration);
        if (onComplete != null) tween.OnComplete(() => onComplete.Invoke());
    }

    public void ResetAnimators()
    {
        // CINEMATIC: Reset FOV
        if (_mainCam != null)
        {
            _mainCam.DOKill();
            _mainCam.DOFieldOfView(_defaultFOV, 0.5f).SetEase(Ease.OutSine);
        }

        if (batsmanAnimator != null && batsmanAnimator.gameObject.activeInHierarchy)
        {
            batsmanAnimator.speed = 1.0f;
            batsmanAnimator.Rebind();
            batsmanAnimator.Update(0f);
        }
        if (bowlerAnimator != null && bowlerAnimator.gameObject.activeInHierarchy)
        {
            bowlerAnimator.speed = 1.0f;
            bowlerAnimator.Rebind();
            bowlerAnimator.Update(0f);
        }
        ResetWickets();
    }

    private void CaptureWicketInitialState()
    {
        if (stumps != null)
        {
            _stumpInitPos = new Vector3[stumps.Length];
            _stumpInitRot = new Quaternion[stumps.Length];
            for (int i = 0; i < stumps.Length; i++)
            {
                if (stumps[i] == null) continue;
                // Store World Position/Rotation to bypass parent scaling issues
                _stumpInitPos[i] = stumps[i].position;
                _stumpInitRot[i] = stumps[i].rotation;
            }
        }
        if (bails != null)
        {
            _bailInitPos = new Vector3[bails.Length];
            _bailInitRot = new Quaternion[bails.Length];
            for (int i = 0; i < bails.Length; i++)
            {
                if (bails[i] == null) continue;
                _bailInitPos[i] = bails[i].position;
                _bailInitRot[i] = bails[i].rotation;
            }
        }
    }

    private void AnimateWicketImpact()
    {
        // Add strong camera shake for wicket impact
        ShakeCamera(0.4f, 0.7f);

        // Use pitchPoint as the ground level reference
        float groundY = (pitchPoint != null) ? pitchPoint.position.y : 0f;

        if (stumps != null)
        {
            foreach (var s in stumps)
            {
                if (s == null) continue;
                // Wide range of fall directions (X: side to side, Z: back/forward)
                Vector3 jumpOffset = new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), 0, UnityEngine.Random.Range(-1.0f, -0.2f));
                Vector3 jumpTarget = s.position + jumpOffset;
                jumpTarget.y = groundY; 
                
                float jumpPower = UnityEngine.Random.Range(0.3f, 0.6f);
                s.DOJump(jumpTarget, jumpPower, 1, 0.6f);
                // Random tumble rotation
                s.DORotate(new Vector3(UnityEngine.Random.Range(-60, 60), UnityEngine.Random.Range(-30, 30), UnityEngine.Random.Range(-60, 60)), 0.6f);
            }
        }
        if (bails != null)
        {
            foreach (var b in bails)
            {
                if (b == null) continue;
                // Bails fly even more wildly
                Vector3 jumpOffset = new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0, UnityEngine.Random.Range(-1.2f, -0.4f));
                Vector3 jumpTarget = b.position + jumpOffset;
                jumpTarget.y = groundY; 
                
                float jumpPower = UnityEngine.Random.Range(1.2f, 2.0f);
                b.DOJump(jumpTarget, jumpPower, 1, 0.7f);
                b.DORotate(new Vector3(UnityEngine.Random.Range(-360, 360), UnityEngine.Random.Range(-360, 360), UnityEngine.Random.Range(-360, 360)), 0.7f);
            }
        }
    }

    private void ResetWickets()
    {
        if (stumps != null && _stumpInitPos != null)
        {
            for (int i = 0; i < stumps.Length; i++)
            {
                if (stumps[i] == null) continue;
                stumps[i].DOKill();
                stumps[i].position = _stumpInitPos[i];
                stumps[i].rotation = _stumpInitRot[i];
            }
        }
        if (bails != null && _bailInitPos != null)
        {
            for (int i = 0; i < bails.Length; i++)
            {
                if (bails[i] == null) continue;
                bails[i].DOKill();
                bails[i].position = _bailInitPos[i];
                bails[i].rotation = _bailInitRot[i];
            }
        }
    }

    // --- Animation Event Callbacks (Must be public to be seen by Animator) ---

    public void OnBatsmanContact()
    {
        if (_isContactMade || ballTransform == null) return;
        
        _isContactMade = true;
        Vector3 batsmanTarget = (batsmanContactPoint != null) ? batsmanContactPoint.position : ballTransform.position;
        ballTransform.position = batsmanTarget;

        bool isWicket = (_currentBatVal == _currentBowlVal);

        if (!isWicket)
        {
            PlaySFX(batHitClip);
            
            // CINEMATIC: Impact Zoom Out (Expansion) 
            // Expanding from tension-zoom (28) to wide action (42)
            if (_mainCam != null)
            {
                _mainCam.DOKill();
                _mainCam.DOFieldOfView(42.0f, 0.4f / simSpeedMultiplier).SetEase(Ease.OutExpo);
            }

            // Visual Feedback: Shake Camera (Scaled by run value)
            float shakeDuration = 0.15f;
            float shakeStrength = 0.3f;
            if (_currentBatVal >= 6) { shakeDuration = 0.35f; shakeStrength = 0.7f; }
            else if (_currentBatVal >= 4) { shakeDuration = 0.25f; shakeStrength = 0.5f; }

            ShakeCamera(shakeDuration, shakeStrength);
        }
    }

    private void ShakeCamera(float duration, float strength)
    {
        if (Camera.main != null)
        {
            Camera.main.transform.DOKill(true); // Stop existing shakes
            Camera.main.transform.DOShakePosition(duration, strength);
        }
    }
}
