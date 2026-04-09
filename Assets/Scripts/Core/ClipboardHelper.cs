using UnityEngine;
using System.Runtime.InteropServices;

public static class ClipboardHelper
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CopyTextToClipboard(string text);
#endif

    public static void Copy(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        try {
            CopyTextToClipboard(text);
        } catch (System.Exception e) {
            Debug.LogError("ClipboardHelper: WebGL Copy failed: " + e.Message);
            GUIUtility.systemCopyBuffer = text; // Fallback
        }
#else
        GUIUtility.systemCopyBuffer = text;
#endif
        Debug.Log($"ClipboardHelper: Copied '{text}' to clipboard.");
    }
}
