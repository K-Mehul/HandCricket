using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nakama;
using System;

public class FriendItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI stateText;
    public Button actionButton; // Add / Accept / Unfriend
    public TextMeshProUGUI actionButtonText;
    public Button rejectButton; // Reject / Cancel
    public Button inviteButton; // Challenge to match
    public Image statusDot; // Green/Gray online indicator

    private IApiFriend currentFriend;
    private Action onStateChanged;

    public void Setup(IApiFriend friend, Action onStateChangedCallback)
    {
        currentFriend = friend;
        onStateChanged = onStateChangedCallback;

        usernameText.text = friend.User.Username;
        
        // Default hidden
        if (rejectButton != null) rejectButton.gameObject.SetActive(false);

        // Relationship States: 0=Friend, 1=Invite Sent, 2=Invite Received, 3=Blocked
        switch (friend.State)
        {
            case 0:
                stateText.text = "Friend";
                actionButtonText.text = "Unfriend";
                break;
            case 1:
                stateText.text = "Sent";
                actionButtonText.text = "Cancel"; // Or use rejectButton for cancel
                break;
            case 2:
                stateText.text = "Received";
                actionButtonText.text = "Accept";
                if (rejectButton != null)
                {
                    rejectButton.gameObject.SetActive(true);
                    rejectButton.onClick.RemoveAllListeners();
                    rejectButton.onClick.AddListener(OnRejectClicked);
                }
                break;
            case 3:
                stateText.text = "Blocked";
                actionButtonText.text = "Unblock";
                break;
        }

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionClicked);
        
        // Presence & Invite
        if (statusDot != null)
        {
            statusDot.color = friend.User.Online ? Color.green : Color.gray;
        }

        if (inviteButton != null)
        {
            // Only show invite if they are a friend AND online
            inviteButton.gameObject.SetActive(friend.State == 0 && friend.User.Online);
            inviteButton.onClick.RemoveAllListeners();
            inviteButton.onClick.AddListener(OnInviteClicked);
        }
    }

    private void OnInviteClicked()
    {
        if (currentFriend == null) return;
        
        // 1. Open Stadium Selection
        var stadiumUI = UIScreenManager.Instance.GetScreen<StadiumSelectUI>("StadiumSelectScreen");
        if (stadiumUI != null)
        {
            stadiumUI.ShowWithCallback(stadium => {
                SendStadiumInvite(stadium);
            });
        }
    }

    private async void SendStadiumInvite(StadiumData stadium)
    {
        inviteButton.interactable = false;
        
        var mkService = MatchmakingService.Instance;
        string code = mkService.GenerateJoinCode();
        
        // 2. Register metadata so recipient can see it
        await mkService.RegisterPrivateRoom(code, stadium.displayName, stadium.stake, stadium.overs, stadium.wickets);

        // 3. Notify the friend with full details
        var myProfile = await NakamaService.Client.GetAccountAsync(NakamaSessionManager.Session);
        await SocialService.Instance.SendMatchInvite(
            currentFriend.User.Id, 
            code, 
            myProfile.User.Username,
            stadium.displayName,
            stadium.stake,
            stadium.overs,
            stadium.wickets
        );
        
        // 4. Host the match
        await mkService.FindPrivateMatch(code, stadium.stake, stadium.overs, stadium.wickets);
        
        Debug.Log($"Challenge sent! Stadium: {stadium.displayName}, Code: {code}");
    }

    private async void OnRejectClicked()
    {
        if (currentFriend == null) return;
        rejectButton.interactable = false;
        await SocialService.Instance.RemoveFriend(currentFriend.User.Id);
        onStateChanged?.Invoke();
    }

    private async void OnActionClicked()
    {
        if (currentFriend == null) return;

        actionButton.interactable = false;

        switch (currentFriend.State)
        {
            case 0: // Remove
            case 1: // Cancel
            case 2: // Accept (Nakama uses AddFriendsAsync to accept)
                if (currentFriend.State == 2)
                    await SocialService.Instance.AddFriendById(currentFriend.User.Id);
                else
                    await SocialService.Instance.RemoveFriend(currentFriend.User.Id);
                break;
            case 3: // Unblock is handled by DeleteFriendsAsync in Nakama
                await SocialService.Instance.RemoveFriend(currentFriend.User.Id);
                break;
        }

        onStateChanged?.Invoke();
    }
}
