using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EntranceModule : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField nickNameInputField;
    [SerializeField] Button connectButton;
    [SerializeField] TextMeshProUGUI connectButtonText;

    private void Start()
    {
        connectButton.onClick.AddListener(OnConnetButtonClick);
    }

    private void OnEnable()
    {
        // GameManager.Instance.PlayerData.NickName = string.Empty;

        connectButton.enabled = true;
        connectButtonText.text = "접속하기";
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * On Connect Button Click *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void OnConnetButtonClick()
    {
        if (nickNameInputField.text.Length == 0 || nickNameInputField.text.Length > 8)
        {
            SceneAssistManager.Instance.SmallModalWindow.OpenModalWindow(ModalWindow.ModalWindowButtonType.Confirm, "닉네임 설정", "닉네임은 1~8 글자로 해주세요!");
            return;
        }

        // ? 연속 클릭 방지.
        connectButton.enabled = false;
        connectButtonText.text = "접속중...";

        GameManager.Instance.PlayerData.NickName = nickNameInputField.text;
        nickNameInputField.text = string.Empty;

        NetworkManager.Instance.ConnectToMaster(() => SceneAssistManager.Instance.OpenLobbyModule());
    }
}
