using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class StadiumCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rulesText;
    public TextMeshProUGUI stakeText;
    public Button selectButton;
    public GameObject lockOverlay;
    public TextMeshProUGUI unlockText;
    public Image cardBgImage;
    [Header("Profile Data")]
    public UserData userData;

    public StadiumData Data { get; private set; }
    private Action<StadiumData> onSelected;

    public void Setup(StadiumData data, Action<StadiumData> onSelected)
    {
        this.Data = data;
        this.onSelected = onSelected;

        string finalName = !string.IsNullOrEmpty(data.displayName) ? data.displayName : data.name;
        Debug.Log($"StadiumCardUI: Setting name for '{data.name}' to '{finalName}' (Color: {data.themeColor})");
        nameText.text = finalName;
        rulesText.text = $"{data.overs} Overs | {data.wickets} Wickets";
        stakeText.text = $"{data.stake} Coins";
        
        //if (data.icon != null) stadiumImage.sprite = data.icon;
        if (data.cardBackground != null && cardBgImage != null) cardBgImage.sprite = data.cardBackground;
        
        // Apply theme color if not transparent
        if (nameText != null && data.themeColor.a > 0.1f) 
        {
            nameText.color = data.themeColor;
        }

        // Unlock Logic: Check user level against stadium requirement
        bool isLocked = (userData != null) && (data.minLevel > userData.level);
        bool canAfford = (userData != null) && (userData.coins >= data.stake);
        
        if (lockOverlay != null) lockOverlay.SetActive(isLocked);
        if (unlockText != null) unlockText.text = isLocked ? $"Unlock at Lvl {data.minLevel}" : (!canAfford ? "Insufficient Coins" : "");

        if (selectButton != null)
        {
            selectButton.interactable = (!isLocked && canAfford);
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelected?.Invoke(data));
        }
    }
}
