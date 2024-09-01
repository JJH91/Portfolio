using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System;
using Random = UnityEngine.Random;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    public bool IsMasterClient { get => PhotonNetwork.IsMasterClient; }
    public bool IsConnected { get => PhotonNetwork.IsConnected; }
    public bool IsInRoom { get => PhotonNetwork.InRoom; }
    public bool IsScenLoadCompleted { get => playSceneLoadingCompleteCount >= 2; }

    int playSceneLoadingCompleteCount = 0;

    [Header("Test")]
    [SerializeField] bool isTestSinglePlayMode;

    [Header("Room List")]
    [SerializeField] List<RoomInfo> totalRoomInfoList = new List<RoomInfo>();
    Action<string> onCreateRoomAction;
    Action onJoinRoomFailAction;
    Action onJoinLobbyAction;

    [Header("Secret Room Code")]
    [SerializeField] string secretRoomCode;

    string tempNetworkStateMsg;

    private void Awake()
    {
        // 싱글턴.
        if (Instance == null)
            Instance = FindObjectOfType(typeof(NetworkManager)) as NetworkManager;
        else if (Instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(Instance);

        // 싱크.
        // PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Update()
    {
        if (tempNetworkStateMsg != PhotonNetwork.NetworkClientState.ToString())
        {
            tempNetworkStateMsg = PhotonNetwork.NetworkClientState.ToString();
            Debug.Log($"네트워크 상태: {PhotonNetwork.NetworkClientState}");
        }

        // PrintNetworkInfo(PhotonNetwork.NetworkClientState.ToString());
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void Test_LogPlayerCount()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            Debug.Log($"PhotonNetwork.CurrentRoom.Name: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"PhotonNetwork.CurrentRoom.PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}");
        }

        Debug.Log($"PhotonNetwork.CountOfPlayers: {PhotonNetwork.CountOfPlayers}");
        Debug.Log($"PhotonNetwork.CountOfPlayersInRooms: {PhotonNetwork.CountOfPlayersInRooms}");
        Debug.Log($"PhotonNetwork.CountOfRooms: {PhotonNetwork.CountOfRooms}");

        Debug.Log($"PhotonNetwork.RoomList: {totalRoomInfoList.Count}");
        foreach (var info in totalRoomInfoList)
            Debug.Log($"Room Info: {info.Name} / {info.PlayerCount}");
    }

    public void Test_CreateSinglePlayRoom()
    {
        PhotonNetwork.CreateRoom("Test", new RoomOptions { MaxPlayers = 2, CleanupCacheOnLeave = false });

        isTestSinglePlayMode = true;
        playSceneLoadingCompleteCount = 2;
        GameManager.Instance.PlayerData.IsRoomMaster = true;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * On Complete Scene Loading *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void CompleteSceneLoading()
    {
        playSceneLoadingCompleteCount++;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable() { { NameManager.READY, true } });
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        if (!targetPlayer.IsLocal)
            playSceneLoadingCompleteCount++;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Connect To Master *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ConnectToMaster(Action joinedLobbyAction)
    {
        PhotonNetwork.GameVersion = NameManager.GAME_VERSION;
        PhotonNetwork.ConnectUsingSettings();

        onJoinLobbyAction += joinedLobbyAction;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * On Master Connected *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void OnConnectedToMaster()
    {
        Debug.Log($"Network Manager: OnConnectedToMaster executed.");
        base.OnConnectedToMaster();

        PhotonNetwork.JoinLobby(new TypedLobby(NameManager.LOBBY, LobbyType.Default));
    }

    public override void OnJoinedLobby()
    {
        Debug.Log($"Network Manager: OnJoinedLobby executed.");
        base.OnJoinedLobby();

        onJoinLobbyAction?.Invoke();
        onJoinLobbyAction = null;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Disconnect *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Network Manager: OnDisconnected executed.");
        base.OnDisconnected(cause);

        if (GameManager.Instance.CurSceneName == NameManager.SceneName.Lobby)
        {
            SceneAssistManager.Instance.OpenEntranceModule();
            Debug.Log($"포톤 서버와 연결이 끊겨 로그인 화면으로 이동.");
        }
        else
        {
#if !UNITY_EDITOR
            ObjectManager.Instance.ClearAllPrefabAssets();
            GameManager.Instance.LoadScene(NameManager.SceneName.Lobby);
            Debug.Log($"포톤 서버와 연결이 끊겨 로비로 이동.");
# endif
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Create Room *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void CreateRoom(string roomName)
    {
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 2, CleanupCacheOnLeave = false });

        GameManager.Instance.PlayerData.IsRoomMaster = true;
    }

    public void CreateSecretRoom(string roomName, Action<string> callback)
    {
        if (callback != null)
            onCreateRoomAction += callback;

        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 2, IsVisible = false, CleanupCacheOnLeave = false });

        GameManager.Instance.PlayerData.IsRoomMaster = true;
        secretRoomCode = roomName;
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Network Manager: OnCreateRoom executed.");

        base.OnCreatedRoom();

        if (onCreateRoomAction != null)
        {
            onCreateRoomAction.Invoke(secretRoomCode);
            onCreateRoomAction = null;
        }

        if (isTestSinglePlayMode)
            GameManager.Instance.LoadScene(NameManager.SceneName.Play);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"Network Manager: OnCreateRoomFailed executed.");
        // ? 룸 생성 실패 =>> 어떤 경우?
        // RoomInput.text = ""; CreateRoom();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Join Room *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void OnJoinedRoom()
    {
        Debug.Log($"Network Manager: OnJoinedRoom executed.");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            GameManager.Instance.LoadScene(NameManager.SceneName.Play);
        else if (PhotonNetwork.CurrentRoom.PlayerCount > 2)
            LeaveRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);

        if (onJoinRoomFailAction != null)
        {
            onJoinRoomFailAction.Invoke();
            onJoinRoomFailAction = null;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Network Manager: OnPlayerEnteredRoom executed.");

        GameManager.Instance.LoadScene(NameManager.SceneName.Play);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Together Play *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void CreatePlayTogetherRoom(Action<string> callback)
    {
        string randomRoomNumberStr;
        while (true)
        {
            bool isPossibleRoomName = true;
            randomRoomNumberStr = $"{Random.Range(0, 10000):D4}";

            foreach (var roomInfo in totalRoomInfoList)
                if (roomInfo.Name == randomRoomNumberStr)
                {
                    isPossibleRoomName = false;
                    break;
                }

            if (isPossibleRoomName)
            {
                // TODO: 방 생성이 실패할 수도 있음. 해당 경우의 예외처리 필요.
                Debug.Log($"같이하기 방 생성: {randomRoomNumberStr}");
                CreateSecretRoom(randomRoomNumberStr, callback);
                break;
            }
        }
    }

    public void JoinRoomByCode(string roomName, Action callback)
    {
        onJoinRoomFailAction += callback;

        PhotonNetwork.JoinRoom(roomName);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Quick Play *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"Network Manager: OnJoinRandomFailed executed.");
        Debug.Log($"returnCode: {returnCode}, message: {message}");

        CreateRoom(string.Empty);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Leave Room *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void LeaveRoom()
    {
        ObjectManager.Instance.ClearAllPrefabAssets();

        isTestSinglePlayMode = false;
        playSceneLoadingCompleteCount = 0;
        secretRoomCode = string.Empty;

        if (PhotonNetwork.CurrentRoom != null)
            PhotonNetwork.LeaveRoom();

        GameManager.Instance.PlayerData.IsRoomMaster = false;
    }

    public override void OnLeftRoom()
    {
        Debug.Log($"Network Manager: OnLeftRoom executed.");
        base.OnLeftRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Network Manager: OnPlayerLeftRoom executed.");
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Room List *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"Network Manager: OnRoomListUpdate executed.");
        base.OnRoomListUpdate(roomList);

        if (totalRoomInfoList.Count == 0)
            totalRoomInfoList.AddRange(roomList);
        else
        {
            foreach (var roomInfo in roomList)
                if (!roomInfo.RemovedFromList)
                {
                    if (!totalRoomInfoList.Contains(roomInfo))
                        totalRoomInfoList.Add(roomInfo);
                    else
                    {
                        var index = totalRoomInfoList.IndexOf(roomInfo);

                        totalRoomInfoList[index] = roomInfo;
                    }
                }
                else
                {
                    var index = totalRoomInfoList.IndexOf(roomInfo);

                    if (index != -1)
                        totalRoomInfoList.RemoveAt(index);
                }
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Print Network Info *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void PrintNetworkInfo(string value)
    {
        TextMeshProUGUI infoText;

        switch (GameManager.Instance.CurSceneName)
        {
            case NameManager.SceneName.Title:
                infoText = SceneAssistManager.Instance.Title_NetworkInfoText;
                break;

            case NameManager.SceneName.Lobby:
                infoText = SceneAssistManager.Instance.Lobby_NetworkInfoText;
                break;

            case NameManager.SceneName.Play:
                infoText = SceneAssistManager.Instance.Play_NetworkInfoText;
                break;

            default:
                Debug.LogError("네트워크 정보를 출력할 수 없습니다.");
                return;
        }

        infoText.text = value;
    }
}