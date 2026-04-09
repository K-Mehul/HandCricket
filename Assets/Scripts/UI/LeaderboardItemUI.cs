using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LeaderboardItemUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI scoreText;
    public Image background;

    public Color playerColor = Color.yellow;
    public Color normalColor = Color.white;

    public void SetData(SocialService.LeaderboardEntry entry)
    {
        if (rankText != null) rankText.text = entry.rank.ToString();
        if (usernameText != null) usernameText.text = entry.username;
        if (scoreText != null) scoreText.text = entry.score.ToString();

        // Highlight the current player
        if (background != null)
        {
            background.color = (entry.userId == SocialService.Instance.Session.UserId) ? playerColor : normalColor;
        }
    }
}
