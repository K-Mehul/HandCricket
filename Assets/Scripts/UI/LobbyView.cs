using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LobbyView : MonoBehaviour
{
    [Header("Matchmaking UI")]
    public Button findMatchButton;
    public Button cancelMatchButton;
    public Button createPrivateButton;
    public TextMeshProUGUI statusText;
    public TMP_InputField joinMatchInput;
    public Button joinMatchButton;

    [Header("Stadium Info UI")]
    public TextMeshProUGUI selectedStadiumText;
    public TextMeshProUGUI stadiumRulesText;
    public Button changeStadiumButton;

    [Header("Navigation")]
    public GameObject previewPanel;
    public TextMeshProUGUI previewDetailsText;
    public Button confirmJoinButton;
    public Button cancelJoinButton;

    [Header("Navigation")]
    public Button backButton;

    // Events for the Presenter to listen to
    public event Action OnFindMatch;
    public event Action OnCancelMatch;
    public event Action OnCreatePrivate;
    public event Action<string> OnJoinMatch; // Code
    public event Action OnConfirmJoin;
    public event Action OnCancelJoin;
    public event Action OnOpenStadiumSelect;
    public event Action OnBack;
    public event Action OnOpenProfile;
    public event Action OnOpenLeaderboard;

    void Start()
    {
        findMatchButton.onClick.AddListener(() => OnFindMatch?.Invoke());
        cancelMatchButton.onClick.AddListener(() => OnCancelMatch?.Invoke());
        createPrivateButton.onClick.AddListener(() => OnCreatePrivate?.Invoke());
        joinMatchButton.onClick.AddListener(() => OnJoinMatch?.Invoke(joinMatchInput.text));
        confirmJoinButton.onClick.AddListener(() => OnConfirmJoin?.Invoke());
        cancelJoinButton.onClick.AddListener(() => OnCancelJoin?.Invoke());
        // changeStadiumButton.onClick.AddListener(() => OnOpenStadiumSelect?.Invoke());
        backButton.onClick.AddListener(() => OnBack?.Invoke());
    }

    public void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    // Redundant profile update removed (now on Main Menu)
    public void UpdateProfile(string username, int level, int coins, int xp) {}

    public void UpdateStadiumInfo(string displayName, string rules)
    {
        if (selectedStadiumText != null) selectedStadiumText.text = displayName;
        if (stadiumRulesText != null) stadiumRulesText.text = rules;
    }

    public void ShowJoinPreview(bool visible, string details = "")
    {
        if (previewPanel != null)
        {
            previewPanel.SetActive(visible);
            if (visible && previewDetailsText != null) previewDetailsText.text = details;
        }
    }

    public void SetMatchmakingState(bool isSearching)
    {
        findMatchButton.gameObject.SetActive(!isSearching);
        createPrivateButton.gameObject.SetActive(!isSearching);
        cancelMatchButton.gameObject.SetActive(isSearching);
    }

    public void ClearJoinInput()
    {
        if (joinMatchInput != null) joinMatchInput.text = "";
    }
}
