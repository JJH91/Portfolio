using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TogetherPlayModalWindow : ModalWindow
{
    enum ModalWindowState { SelectMethod, CreateSecretRoom, JoinSecretRoom }

    [Header("Content UI")]
    [SerializeField] GameObject selectMethodButtonsPanel;
    [SerializeField] GameObject createSecretRoomPanel;
    [SerializeField] GameObject joinSecretRoomPanel;
    [SerializeField] Button createSecretRoomMethodButton;
    [SerializeField] Button joinSecretRoomMethodButton;
    [SerializeField] Button joinSecretRoomButton;
    [SerializeField] TextMeshProUGUI roomCodeText;
    [SerializeField] TextMeshProUGUI joinSecretRoomButtonText;
    [SerializeField] TMP_InputField roomCodeInputField;

    protected override void Awake()
    {
        base.Awake();

        createSecretRoomMethodButton.onClick.AddListener(OnCreateSecretRoomMethodButtonClick);
        joinSecretRoomMethodButton.onClick.AddListener(OnJoinSecretRoomMethodButtonClick);
        joinSecretRoomButton.onClick.AddListener(OnJoinSecretRoomButtonClick);
    }

    private void OnEnable()
    {
        ChangeModalWindowContents(ModalWindowState.SelectMethod);

        roomCodeInputField.enabled = true;
        roomCodeInputField.text = string.Empty;

        joinSecretRoomButtonText.text = "입장";
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * Change Modal Window Contents *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void ChangeModalWindowContents(ModalWindowState modalWindowState)
    {
        switch (modalWindowState)
        {
            case ModalWindowState.SelectMethod:
                selectMethodButtonsPanel.gameObject.SetActive(true);
                createSecretRoomPanel.gameObject.SetActive(false);
                joinSecretRoomPanel.gameObject.SetActive(false);
                break;

            case ModalWindowState.CreateSecretRoom:
                selectMethodButtonsPanel.gameObject.SetActive(false);
                createSecretRoomPanel.gameObject.SetActive(true);
                joinSecretRoomPanel.gameObject.SetActive(false);
                break;

            case ModalWindowState.JoinSecretRoom:
                selectMethodButtonsPanel.gameObject.SetActive(false);
                createSecretRoomPanel.gameObject.SetActive(false);
                joinSecretRoomPanel.gameObject.SetActive(true);
                break;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * On Button Click *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void OnCreateSecretRoomMethodButtonClick()
    {
        ChangeModalWindowContents(ModalWindowState.CreateSecretRoom);

        roomCodeText.text = "방 입장 코드 생성중...";

        NetworkManager.Instance.CreatePlayTogetherRoom(OnCreateSecretRoomCallback);
    }

    void OnCreateSecretRoomCallback(string roomCode)
    {
        roomCodeText.text = $"방 입장 코드: {roomCode}";
    }

    void OnJoinSecretRoomMethodButtonClick()
    {
        ChangeModalWindowContents(ModalWindowState.JoinSecretRoom);

        roomCodeInputField.enabled = true;
        roomCodeInputField.text = string.Empty;
    }

    void OnJoinSecretRoomButtonClick()
    {
        if (roomCodeInputField.text.Length != 4)
            return;

        roomCodeInputField.enabled = false;
        joinSecretRoomButtonText.text = "입장중...";

        NetworkManager.Instance.JoinRoomByCode(roomCodeInputField.text, OnJoinSecretRoomCallback);
    }

    void OnJoinSecretRoomCallback()
    {
        roomCodeInputField.enabled = true;
        joinSecretRoomButtonText.text = "입장 재시도(실패)";
    }
}
