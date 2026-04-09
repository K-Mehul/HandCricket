using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SplashView : MonoBehaviour
{
    [Header("UI Elements")]
    public Image logoImage;
    public TextMeshProUGUI loadingText;

    public void SetLoadingText(string text)
    {
        if (loadingText != null) loadingText.text = text;
    }
}
