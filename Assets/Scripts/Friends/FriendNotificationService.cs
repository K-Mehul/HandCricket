using Nakama;
using UnityEngine;

public class FriendNotificationService
{
    public void Initialize()
    {
        NakamaService.Socket
            .ReceivedNotification += OnNotification;
    }

    void OnNotification(
        IApiNotification notification)
    {
        Debug.Log(
            "Friend Notification: "
            + notification.Subject);

        NotificationUI.Show(
            notification.Subject);
    }
}
