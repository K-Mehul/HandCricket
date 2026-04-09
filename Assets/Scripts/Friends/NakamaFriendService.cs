using Nakama;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class NakamaFriendService
{
    IClient client => NakamaService.Client;

    ISession session =>
        NakamaSessionManager.Session;

    ISocket socket =>
        NakamaService.Socket;


    // GET FRIEND LIST

    /// <summary>
    /// Fetches the current user's friends details and updates their online status subscription.
    /// </summary>
    /// <returns>List of Friend Models</returns>
    public async Task<List<FriendModel>> GetFriends()
    {
        try 
        {
            var result = await client.ListFriendsAsync(session);

            // Convert to internal model
            var friends = result.Friends
                .Select(f => new FriendModel(f))
                .ToList();

            // Auto-follow these friends to get their Online/Offline status
            var friendIds = friends.Select(f => f.UserId);
            OnlineStatusService.FollowUsers(friendIds);

            return friends;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to load friends: " + e.Message);
            return new List<FriendModel>();
        }
    }


    // SEND FRIEND REQUEST USING USERNAME

    public async Task SendFriendRequest(
        string username)
    {
        await client.AddFriendsAsync(
            session,
            new string[] { },
            new string[] { username });
    }


    // ACCEPT REQUEST

    public async Task AcceptFriend(
        string userId)
    {
        await client.AddFriendsAsync(
            session,
            new string[] { userId });
    }


    // REJECT REQUEST

    public async Task RejectFriend(
        string userId)
    {
        await client.DeleteFriendsAsync(
            session,
            new string[] { userId });
    }


    // REMOVE FRIEND

    public async Task RemoveFriend(
        string userId)
    {
        await client.DeleteFriendsAsync(
            session,
            new string[] { userId });
    }
}

/*0 = Mutual friend
1 = Incoming request
2 = Sent request
3 = Blocked*/