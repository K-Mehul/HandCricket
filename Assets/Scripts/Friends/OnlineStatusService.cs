using Nakama;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class OnlineStatusService
{
    public static Dictionary<string, bool>
        OnlineUsers =
        new Dictionary<string, bool>();


    /// <summary>
    /// Initializes the service and subscribes to presence events.
    /// </summary>
    public static void Initialize()
    {
        NakamaService.Socket.ReceivedStatusPresence += OnPresenceChanged;
    }

    /// <summary>
    /// Subscribes to status updates for a list of users (e.g. friends).
    /// </summary>
    /// <param name="userIds">List of User IDs to follow</param>
    public static async void FollowUsers(IEnumerable<string> userIds)
    {
        if (userIds == null || !userIds.Any()) return;

        try 
        {
            await NakamaService.Socket.FollowUsersAsync(userIds);
            Debug.Log($"Following {userIds.Count()} users for status updates.");
        }
        catch (System.Exception e) 
        {
            Debug.LogWarning("Error following users: " + e.Message);
        }
    }


    static void OnPresenceChanged(
        IStatusPresenceEvent presence)
    {
        // USER ONLINE

        foreach (var user in presence.Joins)
        {
            OnlineUsers[user.UserId] = true;

            Debug.Log(
                user.Username +
                " ONLINE");
        }


        // USER OFFLINE

        foreach (var user in presence.Leaves)
        {
            OnlineUsers[user.UserId] = false;

            Debug.Log(
                user.Username +
                " OFFLINE");
        }
    }
}
