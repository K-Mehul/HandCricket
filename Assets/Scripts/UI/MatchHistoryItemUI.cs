using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MatchHistoryItemUI : MonoBehaviour
{
    public TextMeshProUGUI opponentText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI earningsText;
    public TextMeshProUGUI dateText;
    public Image resultBg;

    public Color winColor = Color.green;
    public Color lossColor = Color.red;
    public Color drawColor = Color.gray;

    public void SetData(SocialService.MatchHistoryEntry entry)
    {
        if (opponentText != null) opponentText.text = entry.opponent;
        if (resultText != null) resultText.text = entry.result;
        if (dateText != null) dateText.text = entry.GetFormattedDate();

        if (earningsText != null)
        {
            earningsText.text = (entry.earnings > 0 ? "+" : "") + entry.earnings;
            // earningsText.color = entry.earnings > 0 ? winColor : lossColor;
        }

        if (resultBg != null)
        {
            if (entry.result == "WON") resultBg.color = winColor;
            else if (entry.result == "LOST") resultBg.color = lossColor;
            else resultBg.color = drawColor;
        }
    }
}