using UnityEngine;
using UnityEngine.UI;
using Nakama;
using TMPro;

public class FriendsUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform friendsListContainer;
    public Transform requestsListContainer;
    public GameObject friendItemPrefab; // Attach FriendItemUI prefab
    public TMP_InputField addFriendInput;
    public Button addFriendButton;
    public Button refreshButton;

    NakamaFriendService service = new NakamaFriendService();

    void Start()
    {
        addFriendButton.onClick.AddListener(SendRequest);
        refreshButton.onClick.AddListener(LoadFriends);
        
        // Load on start
        LoadFriends();
    }

    public async void SendRequest()
    {
        string username = addFriendInput.text;
        if (string.IsNullOrEmpty(username)) return;

        await service.SendFriendRequest(username);
        Debug.Log("Friend request sent to " + username);
        addFriendInput.text = "";
        LoadFriends();
    }

    public async void LoadFriends()
    {
        // Clear old list
        foreach(Transform child in friendsListContainer) Destroy(child.gameObject);
        foreach(Transform child in requestsListContainer) Destroy(child.gameObject);

        var friends = await service.GetFriends();

        foreach (var friend in friends)
        {
            // Instantiate Item
            GameObject itemObj = Instantiate(friendItemPrefab);
            FriendItemUI itemUI = itemObj.GetComponent<FriendItemUI>();

            if (friend.State == 0) // Mutual Friend
            {
                itemObj.transform.SetParent(friendsListContainer, false);
                itemUI.Setup(friend.Raw, () => OnInviteClicked(friend.UserId));
            }
            else if (friend.State == 1) // Incoming Request
            {
                itemObj.transform.SetParent(requestsListContainer, false);
                itemUI.Setup(friend.Raw, () => OnAcceptClicked(friend.UserId));
            }
            // We can also show Sent requests if needed
        }
    }

    private async void OnAcceptClicked(string userId)
    {
        await service.AcceptFriend(userId);
        LoadFriends(); // Refresh list
    }

    private void OnInviteClicked(string userId)
    {
        Debug.Log("Invite clicked for " + userId);
        // Invite logic to be implemented
    }
}