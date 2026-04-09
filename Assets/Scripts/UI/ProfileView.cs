using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ProfileView : MonoBehaviour
{
    [Header("Top Bar / Info")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinsText;
    public Button backButton;
    public Button matchHistoryButton;

    [Header("Tabs")]
    public Button battingTabButton;
    public Button bowlingTabButton;
    public GameObject battingPanel;
    public GameObject bowlingPanel;

    [Header("Batting Stats")]
    public TextMeshProUGUI totalRunsText;
    public TextMeshProUGUI highestScoreText;
    public TextMeshProUGUI battingAvgText;
    public TextMeshProUGUI strikeRateText;
    public TextMeshProUGUI fourCountText;
    public TextMeshProUGUI sixCountText;
    public TextMeshProUGUI battingMatchesText;

    [Header("Bowling Stats")]
    public TextMeshProUGUI totalWicketsText;
    public TextMeshProUGUI bestBowlingText; // e.g. "3/15"
    public TextMeshProUGUI economyText;
    public TextMeshProUGUI bowlingAvgText;
    public TextMeshProUGUI bowlingStrikeRateText;
    public TextMeshProUGUI bowlingMatchesText;

    [Header("General Stats")]
    public TextMeshProUGUI winLossRatioText;
    public TextMeshProUGUI recordText; // e.g. "10W / 5L / 2D"

    public event Action OnBackClicked;
    public event Action OnBattingTabClicked;
    public event Action OnBowlingTabClicked;
    public event Action OnMatchHistoryClicked;

    private void Awake()
    {
        backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        battingTabButton.onClick.AddListener(() => OnBattingTabClicked?.Invoke());
        bowlingTabButton.onClick.AddListener(() => OnBowlingTabClicked?.Invoke());
        if (matchHistoryButton != null) matchHistoryButton.onClick.AddListener(() => OnMatchHistoryClicked?.Invoke());
    }

    public void SetUserInfo(string username, int level, int coins)
    {
        if (usernameText != null) usernameText.text =  username;
        if (levelText != null) levelText.text = "LEVEL: " + level;
        if (coinsText != null) coinsText.text = "COINS: " + coins.ToString();
    }

    public void SetBattingStats(int runs, int hs, float avg, float sr, int fours, int sixes, int matches)
    {
        if (totalRunsText != null) totalRunsText.text = "TOTAL RUNS: " + runs.ToString();
        if (highestScoreText != null) highestScoreText.text = "HIGHEST SCORE: " + hs.ToString();
        if (battingAvgText != null) battingAvgText.text = "AVG: " + avg.ToString("F2");
        if (strikeRateText != null) strikeRateText.text = "SR: " + sr.ToString("F1");
        if (fourCountText != null) fourCountText.text = "4's: " + fours.ToString();
        if (sixCountText != null) sixCountText.text = "6's: " + sixes.ToString();
        if (battingMatchesText != null) battingMatchesText.text = "Matches: " + matches;
    }

    public void SetBowlingStats(int wkts, string bestBowling, float econ, float avg, float sr, int matches)
    {
        if (totalWicketsText != null) totalWicketsText.text = "TOTAL WICKETS: " + wkts.ToString();
        if (bestBowlingText != null) bestBowlingText.text = "BEST BOWLING: " + bestBowling;
        if (economyText != null) economyText.text = "ECONOMY: " + econ.ToString("F2");
        if (bowlingAvgText != null) bowlingAvgText.text = "AVG: " + avg.ToString("F2");
        if (bowlingStrikeRateText != null) bowlingStrikeRateText.text = "SR: " + sr.ToString("F1");
        if (bowlingMatchesText != null) bowlingMatchesText.text = "Matches: " + matches;
    }

    public void SetGeneralStats(string record, string winLossRatio)
    {
        if (recordText != null) recordText.text = "RECORD: " + record;
        if (winLossRatioText != null) winLossRatioText.text = winLossRatio;
    }

    public void ShowTab(bool isBatting)
    {
        if (battingPanel != null) battingPanel.SetActive(isBatting);
        if (bowlingPanel != null) bowlingPanel.SetActive(!isBatting);
        
        // Visual feedback for tab buttons (optional, can be done with colors/sprites)
        if (battingTabButton != null) battingTabButton.GetComponent<Image>().color = isBatting ? Color.white : Color.gray;
        if (bowlingTabButton != null) bowlingTabButton.GetComponent<Image>().color = !isBatting ? Color.white : Color.gray;
    }
}
