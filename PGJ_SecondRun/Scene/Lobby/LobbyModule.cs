using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyModule : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] TextMeshProUGUI topBarText;
    [SerializeField] Button backButton;

    [Header("Center")]
    [SerializeField] Button togetherPlayButton;
    [SerializeField] Button quickPlayButton;

    [Header("Quick Play Modal Window")]
    [SerializeField] ModalWindow togetherPlayModalWindow;
    [SerializeField] ModalWindow quickPlayModalWindow;

    private void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClick);
    }

    private void Start()
    {
        togetherPlayButton.onClick.AddListener(() => togetherPlayModalWindow.OpenModalWindow(ModalWindow.ModalWindowButtonType.Cancel, null, () => NetworkManager.Instance.LeaveRoom()));

        quickPlayButton.onClick.AddListener(NetworkManager.Instance.JoinRandomRoom);
        quickPlayButton.onClick.AddListener(() => quickPlayModalWindow.OpenModalWindow(ModalWindow.ModalWindowButtonType.Cancel, null, () => NetworkManager.Instance.LeaveRoom()));
    }

    private void OnEnable()
    {
        // TODO: 서버 연결은 엔트란스에서 처리함(아이디 입력도), 그냥 여기에 뭔가 UI 같은게 추가되면 작업을 해주면 될듯?
        topBarText.text = $"{GameManager.Instance.PlayerData.NickName}님 접속을 환영합니다!";
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void Test(string roomName)
    {
        CreateRoom(roomName);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Create Room *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void CreateRoom(string roomName)
    {
        // TODO: 중복되는 방 이름이 생성되면 다시 번호를 재발급 받아야함
        if (roomName.IsNullOrEmpty())
            roomName = Random.Range(0, 9999).ToString("D4");

        Debug.Log($"{roomName}");

        NetworkManager.Instance.CreateRoom(roomName);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Leave Room *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void LeaceRoom()
    {
        // TODO: 빠른 시작 혹은 같이 하기에서 방을 만든 후 방에서 나가기(삭제) 하기 위한 버튼 이벤트 용
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * On Back Button Click *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OnBackButtonClick()
    {
        NetworkManager.Instance.Disconnect();

        SceneAssistManager.Instance.OpenEntranceModule(true);
    }
}
