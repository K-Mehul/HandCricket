using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Screen that shows "VS" info and match details before transitioning to GameScene.
/// </summary>
public class MatchIntroScreen : UIScreen
{
    [Header("UI References")]
    public TextMeshProUGUI p1NameText;
    public TextMeshProUGUI p2NameText;
    public TextMeshProUGUI vsText;
    public TextMeshProUGUI matchInfoText; // "Stake: 50 | 2 Overs"
    public float displayDuration = 0f;

    protected override void OnShow()
    {
        base.OnShow();
        if(p1NameText != null) p1NameText.text = "...";
        if(p2NameText != null) p2NameText.text = "...";
        if(matchInfoText != null) matchInfoText.text = "Loading Match...";
    }

    public void Init(string p1, string p2, string info)
    {
        if(p1NameText != null) p1NameText.text = p1;
        if(p2NameText != null) p2NameText.text = p2;
        if(matchInfoText != null) matchInfoText.text = info;
        
        StopAllCoroutines();
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        
        Debug.Log("Match Intro Complete. Transitioning to GameScene...");
        UIScreenManager.Instance.Show("GameScene");
    }
}
