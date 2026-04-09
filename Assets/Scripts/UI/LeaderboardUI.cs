using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nakama;
using System.Threading.Tasks;
using System.Linq; // Added for Count()

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;
    public GameObject itemPrefab;
    public Button backButton;
    public Button dailyButton;
    public Button weeklyButton;
    public Button globalButton;

    [Header("Config")]
    public int limit = 20;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        if (dailyButton != null) dailyButton.onClick.AddListener(() => LoadLeaderboard("daily_wins"));
        if (weeklyButton != null) weeklyButton.onClick.AddListener(() => LoadLeaderboard("weekly_wins"));
        if (globalButton != null) globalButton.onClick.AddListener(() => LoadLeaderboard("global_wins"));
    }

    private void OnEnable()
    {
        // Default to Global on open
        LoadLeaderboard("global_wins");
    }

    [ContextMenu("GLOBAL_BUTTON_CLICKED")]
    private void GLOBAL_BUTTON_CLICKED()
    {
        LoadLeaderboard("global_wins");
    }

    [ContextMenu("WEEKLY_BUTTON_CLICKED")]
    private void WEEKLY_BUTTON_CLICKED()
    {
        LoadLeaderboard("weekly_wins");
    }

    [ContextMenu("DAILY_BUTTON_CLICKED")]
    private void DAILY_BUTTON_CLICKED()
    {
        LoadLeaderboard("daily_wins");
    }

    private async void LoadLeaderboard(string id)
    {
        // Clear old items
        // foreach (Transform child in contentParent)
        // {
        //     Destroy(child.gameObject);
        // }

        try
        {
            var result = await NakamaService.Client.ListLeaderboardRecordsAsync(
                NakamaSessionManager.Session,
                id,
                ownerIds: null,
                expiry: null,
                limit: limit
            );

            Debug.Log($"LEADERBOARD");

            foreach (var record in result.Records)
            {
                // var obj = Instantiate(itemPrefab, contentParent);
                // var item = obj.GetComponent<LeaderboardItemUI>();
                // if (item != null)
                // {
                //     // Rank and Score are strings in Nakama SDK to support BigInts
                //     long rank = long.Parse(record.Rank);
                //     long score = long.Parse(record.Score);
                //     item.SetData(rank, record.Username, score);
                // }
                // Debug.Log($"Loaded {record.Username} {record.Score} {record.Rank}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load leaderboard {id}: {e.Message}");
        }
    }

    private void OnBackClicked()
    {
        UIScreenManager.Instance.Show("MainMenuScreen");
    }
}
