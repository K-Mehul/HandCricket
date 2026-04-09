using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class SocialView : MonoBehaviour
{
    [Header("Tab System")]
    public Transform tabContainer;
    public GameObject tabPrefab;
    public Color activeTabColor = Color.white;
    public Color idleTabColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("List Container")]
    public Transform listContainer;
    public GameObject itemPrefab;
    public TextMeshProUGUI statusText;

    [Header("Form Elements")]
    public TMP_InputField usernameInput;
    public Button addFriendButton;
    public TMP_InputField searchInput;
    public Button backButton;

    // Events for the Presenter
    public event Action<string> OnTabSelected;
    public event Action<string> OnAddFriend;
    public event Action<string> OnSearchChanged;
    public event Action OnBack;

    private Dictionary<string, SocialTabView> _tabs = new Dictionary<string, SocialTabView>();

    void Start()
    {
        if (addFriendButton != null) addFriendButton.onClick.AddListener(() => OnAddFriend?.Invoke(usernameInput.text));
        if (backButton != null) backButton.onClick.AddListener(() => OnBack?.Invoke());
        if (searchInput != null) searchInput.onValueChanged.AddListener((val) => OnSearchChanged?.Invoke(val));
    }

    public void CreateTabs(List<(string id, string name)> tabConfigs)
    {
        // Clear existing tabs
        foreach (Transform child in tabContainer) Destroy(child.gameObject);
        _tabs.Clear();

        foreach (var config in tabConfigs)
        {
            GameObject go = Instantiate(tabPrefab, tabContainer);
            SocialTabView tabView = go.GetComponent<SocialTabView>();
            tabView.Setup(config.id, config.name, (id) => OnTabSelected?.Invoke(id));
            _tabs.Add(config.id, tabView);
        }
    }

    public void UpdateTabVisuals(string activeTabId)
    {
        foreach (var kv in _tabs)
        {
            kv.Value.SetSelected(kv.Key == activeTabId, activeTabColor, idleTabColor);
        }
    }

    public void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    public void ClearContainer()
    {
        foreach (Transform child in listContainer) Destroy(child.gameObject);
    }

    public void ClearInputs()
    {
        if (usernameInput != null) usernameInput.text = "";
    }
}
