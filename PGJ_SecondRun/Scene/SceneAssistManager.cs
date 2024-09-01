using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneAssistManager : MonoSingleton<SceneAssistManager>
{
    [Header("Title Scene")]
    [SerializeField] TextMeshProUGUI title_NetworkInfoText;
    public TextMeshProUGUI Title_NetworkInfoText { get => title_NetworkInfoText; }
    [SerializeField] Button title_TestButton;

    [Header("Lobby Scene")]
    [SerializeField] TextMeshProUGUI lobby_NetworkInfoText;
    public TextMeshProUGUI Lobby_NetworkInfoText { get => lobby_NetworkInfoText; }
    [SerializeField] TextMeshProUGUI lobby_TestMatchingText;

    [SerializeField] EntranceModule entranceModule;
    public EntranceModule EntranceModule { get => entranceModule; }
    [SerializeField] LobbyModule lobbyModule;
    public LobbyModule LobbyModule { get => lobbyModule; }

    [SerializeField] Button lobby_TestPlayButton;

    [Header("Play Scene")]
    [SerializeField] TextMeshProUGUI play_NetworkInfoText;
    public TextMeshProUGUI Play_NetworkInfoText { get => play_NetworkInfoText; }
    [SerializeField] TextMeshProUGUI play_GameInfoText;
    public TextMeshProUGUI Play_GameInfoText { get => play_GameInfoText; }
    [SerializeField] Button play_TestButton;
    [SerializeField] Button play_ExitButton;
    [SerializeField] Button play_ExitButton_Temp;

    [Header("Modal Window")]
    [SerializeField] ModalWindow smallModalWindow;
    public ModalWindow SmallModalWindow { get => smallModalWindow; }

    // [Header("Chat")]
    // [SerializeField] Chat chat;

    private void Awake()
    {
        MakeDestroyableInstance();
    }

    private void Start()
    {
        switch (GameManager.Instance.CurSceneName)
        {
            case NameManager.SceneName.None:
                break;

            case NameManager.SceneName.Title:
                break;

            case NameManager.SceneName.Lobby:
                lobby_TestPlayButton.onClick.AddListener(() => NetworkManager.Instance.Test_CreateSinglePlayRoom());

                OpenEntranceModule();
                break;

            case NameManager.SceneName.Play:
                play_ExitButton.onClick.AddListener(() => NetworkManager.Instance.LeaveRoom());
                play_ExitButton.onClick.AddListener(() => GameManager.Instance.LoadScene(NameManager.SceneName.Lobby));

                play_ExitButton_Temp.onClick.AddListener(() => NetworkManager.Instance.LeaveRoom());
                play_ExitButton_Temp.onClick.AddListener(() => GameManager.Instance.LoadScene(NameManager.SceneName.Lobby));
                break;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Lobby Scene *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Entrance Module *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OpenEntranceModule(bool isForce = false)
    {
        Debug.Log($"OpenEntranceModule 실행");
        EntranceModule.gameObject.SetActive(true);
        LobbyModule.gameObject.SetActive(false);

        if (isForce)
            return;

        if (NetworkManager.Instance.IsConnected)
        {
            Debug.Log($"로그인된 상태, 로비 모듈 실행");
            Instance.OpenLobbyModule();
            return;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Lobby Module *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OpenLobbyModule()
    {
        Debug.Log($"OpenLobbyModule 실행");
        EntranceModule.gameObject.SetActive(false);
        LobbyModule.gameObject.SetActive(true);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Play Scene *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Show Game Message *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ShowGameMessage(string value)
    {
        CancelInvoke();

        Play_GameInfoText.gameObject.SetActive(true);
        play_GameInfoText.text = value;

        Invoke(nameof(DeactiveGameMessage), 1f);
    }

    void DeactiveGameMessage()
    {
        Play_GameInfoText.gameObject.SetActive(false);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Exit Play Scene *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void ExitPlayScene()
    {

    }
}
