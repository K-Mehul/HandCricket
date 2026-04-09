using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationUI : MonoBehaviour
{
    public GameObject notificationPanel;
    public TextMeshProUGUI text;
    public Button joinLobbyButton;

    public static NotificationUI instance;

    void Awake()
    {
        instance = this;
        if (joinLobbyButton != null)
        {
            joinLobbyButton.onClick.AddListener(OnJoinClicked);
            joinLobbyButton.gameObject.SetActive(false);
        }
    }

    public static void Show(
        string message, bool showJoinButton = false)
    {
        instance.text.text = message;

        if (instance.joinLobbyButton != null)
        {
            instance.joinLobbyButton.gameObject.SetActive(showJoinButton);
        }

        instance.notificationPanel.gameObject.SetActive(true);

        // Auto-hide after 10 seconds ALWAYS (prevents sticking)
        instance.CancelInvoke("Hide");
        instance.Invoke("Hide", 10.0f);
    }

    private void Hide()
    {
        notificationPanel.gameObject.SetActive(false);
    }

    private void OnJoinClicked()
    {
        Hide();
    }
}
