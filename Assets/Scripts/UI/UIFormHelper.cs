using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// A reusable helper for UI forms. Handles Tabbing between fields, 
/// Enter-to-Submit, and Password visibility toggling.
/// </summary>
public class UIFormHelper : MonoBehaviour
{
    [Header("Navigation")]
    [Tooltip("The order of input fields when pressing Tab.")]
    [SerializeField] private List<TMP_InputField> fields = new List<TMP_InputField>();
    
    [Header("Submission")]
    [Tooltip("The button to trigger when Enter/Return is pressed.")]
    [SerializeField] private Button submitButton;

    [Header("Password Visibility")]
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Image toggleIcon;
    [SerializeField] private Sprite showIcon;
    [SerializeField] private Sprite hideIcon;

    private void OnEnable()
    {
        // Always reset password to hidden whenever the screen is enabled
        if (passwordField != null)
        {
            passwordField.contentType = TMP_InputField.ContentType.Password;
            passwordField.ForceLabelUpdate();
            UpdateIcon(false);
        }
    }

    private void Update()
    {
        // 1. Handle Tab Navigation
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectNextField();
        }

        // 2. Handle Enter Submission
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (submitButton != null && submitButton.interactable && submitButton.gameObject.activeInHierarchy)
            {
                submitButton.onClick.Invoke();
            }
        }
    }

    private void SelectNextField()
    {
        if (fields.Count == 0) return;

        // Find which field is currently selected
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;

        TMP_InputField currentField = current.GetComponent<TMP_InputField>();
        if (currentField == null) return;

        int index = fields.IndexOf(currentField);
        if (index == -1) return;

        // Calculate next index (loops back to 0)
        int nextIndex = (index + 1) % fields.Count;
        
        // Select and activate
        fields[nextIndex].ActivateInputField();
    }

    /// <summary>
    /// Swaps the assigned passwordField between Password and Standard text.
    /// Also updates the toggleIcon if assigned.
    /// </summary>
    public void TogglePasswordVisibility()
    {
        if (passwordField == null) return;

        bool isCurrentlyPassword = passwordField.contentType == TMP_InputField.ContentType.Password;
        
        if (isCurrentlyPassword)
        {
            passwordField.contentType = TMP_InputField.ContentType.Standard;
            UpdateIcon(true);
        }
        else
        {
            passwordField.contentType = TMP_InputField.ContentType.Password;
            UpdateIcon(false);
        }

        // Force refresh the text logic
        passwordField.ForceLabelUpdate();
    }

    private void UpdateIcon(bool isVisible)
    {
        if (toggleIcon != null)
        {
            toggleIcon.sprite = isVisible ? showIcon : hideIcon;
        }
    }

    /// <summary>
    /// Swaps a specific input field between Password and Standard text.
    /// (Manual version for multiple fields)
    /// </summary>
    public void TogglePasswordVisibility(TMP_InputField field)
    {
        if (field == null) return;

        field.contentType = (field.contentType == TMP_InputField.ContentType.Password) 
            ? TMP_InputField.ContentType.Standard 
            : TMP_InputField.ContentType.Password;

        field.ForceLabelUpdate();
    }
}
