using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModalWindow : MonoBehaviour
{
    public enum ModalWindowButtonType { Confirm, Cancel, Both }

    [Header("Content UI")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descriptionText;
    public TextMeshProUGUI TitleText { get => titleText; }
    public TextMeshProUGUI DescriptionText { get => descriptionText; }

    [SerializeField] Button confirmButton, cancelButton;
    // public Button ConfirmButton { get => confirmButton; }
    // public Button CancelButton { get => cancelButton; }

    protected Action confirmButtonAction, cancelButtonAction;

    protected virtual void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClick);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Open/Close Modal Window *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OpenModalWindow(ModalWindowButtonType modalWindowButtonType = ModalWindowButtonType.Both, string title = null, string description = null)
    {
        OpenModalWindow(modalWindowButtonType, title, description, null, null);
    }

    public void OpenModalWindow(ModalWindowButtonType modalWindowButtonType = ModalWindowButtonType.Both, Action confirmAction = null, Action cancelAction = null)
    {
        OpenModalWindow(modalWindowButtonType, string.Empty, string.Empty, confirmAction, cancelAction);
    }

    public void OpenModalWindow(ModalWindowButtonType modalWindowButtonType = ModalWindowButtonType.Both, string title = null, string description = null, Action confirmAction = null, Action cancelAction = null)
    {
        switch (modalWindowButtonType)
        {
            case ModalWindowButtonType.Confirm:
                if (confirmButton != null)
                    confirmButton.gameObject.SetActive(true);
                if (cancelButton != null)
                    cancelButton.gameObject.SetActive(false);
                break;

            case ModalWindowButtonType.Cancel:
                if (confirmButton != null)
                    confirmButton.gameObject.SetActive(false);
                if (cancelButton != null)
                    cancelButton.gameObject.SetActive(true);
                break;

            case ModalWindowButtonType.Both:
                if (confirmButton != null)
                    confirmButton.gameObject.SetActive(false);
                if (cancelButton != null)
                    cancelButton.gameObject.SetActive(false);
                break;
        }

        if (!title.IsNullOrEmpty())
            titleText.text = title;

        if (!description.IsNullOrEmpty())
            descriptionText.text = description;

        if (confirmAction != null)
            confirmButtonAction += confirmAction;

        if (cancelAction != null)
            cancelButtonAction += cancelAction;

        gameObject.SetActive(true);
    }

    public void CloseModalWindow()
    {
        confirmButtonAction = null;
        cancelButtonAction = null;

        gameObject.SetActive(false);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * On Button Click *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void OnConfirmButtonClick()
    {
        confirmButtonAction?.Invoke();

        CloseModalWindow();
    }

    void OnCancelButtonClick()
    {
        cancelButtonAction?.Invoke();

        CloseModalWindow();
    }
}
