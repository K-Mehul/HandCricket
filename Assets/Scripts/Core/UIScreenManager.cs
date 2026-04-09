using UnityEngine;
using System.Collections.Generic;

public class UIScreenManager : MonoBehaviour
{
    public static UIScreenManager Instance;

    Dictionary<string, UIScreen> screens = new Dictionary<string, UIScreen>();

    UIScreen currentScreen;


    void Awake()
    {
        Instance = this;


        var allScreens =  GetComponentsInChildren<UIScreen>(true);

        foreach (var screen in allScreens)
        {
            screens.Add(
                screen.name,
                screen);

            screen.Hide();
        }
    }



    public void Show(string screenName)
    {
        if (currentScreen != null)
            currentScreen.Hide();

        if (screens.ContainsKey(screenName))
        {
            currentScreen = screens[screenName];
            currentScreen.Show();
        }
        else
        {
            Debug.LogError($"Screen {screenName} not found!");
        }
    }

    public string GetCurrentScreenName()
    {
        return currentScreen != null ? currentScreen.name : "";
    }

    public T GetCurrentScreen<T>() where T : UIScreen
    {
        return currentScreen as T;
    }

    public T GetScreen<T>(string screenName) where T : UIScreen
    {
        if (screens.ContainsKey(screenName))
        {
            return screens[screenName] as T;
        }
        return null;
    }
}
