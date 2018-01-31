using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class PhotonController : Photon.PunBehaviour
{
    private const string PLAYER_1 = "player1";
    private const string PLAYER_2 = "player2";
    private const string PLAYERS_READY = "ready";

    public GameObject StartScreen;
    public GameObject GameScreen;
    public GameObject StartButtonGO;
    public GameObject PlacarLabel;
    public GameObject ClockLabel;
    public GameObject Prefab;
    public GameObject Finish;
    public GameObject WaitReady;

    public Button JoinRoomButton;
    public Button StartButton;

    public Text WaitingLabel;
    public Text MyScoreLabel;
    public Text OtherScoreLabel;
    public Text Clock;
    public Text ResultLabel;

    public float SpawnTime;
    public float LifeTime;
    public int GameTime;

    public string myName;
    public string otherName;

    public bool GameStarted;

    private IEnumerator blockSpawnCoroutine;
    private IEnumerator clockCoroutine;

    private PhotonHashtable roomCustomProperties;

    public List<GameObject> prefabs;



    void Start()
    {
        PhotonNetwork.ConnectUsingSettings("v1.0");
        roomCustomProperties = new PhotonHashtable();
        prefabs = new List<GameObject>();
    }

    void Update()
    {
    }

    void OnGUI()
    {
        GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
    }

    #region Photon Methods
    public override void OnJoinedLobby()
    {
        JoinRoomButton.interactable = true;
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        SetupRoom();
    }

    public override void OnJoinedRoom()
    {
        roomCustomProperties = PhotonNetwork.room.CustomProperties;
        StartScreen.SetActive(false);
        GameScreen.SetActive(true);
        if (PhotonNetwork.room.PlayerCount == 2)
        {
            WaitingLabel.enabled = false;
            StartButton.interactable = true;
            StartButtonGO.SetActive(true);
            myName = PLAYER_2;
            otherName = PLAYER_1;
        }
        else
        {
            myName = PLAYER_1;
            otherName = PLAYER_2;
        }
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        WaitingLabel.enabled = false;
        StartButton.interactable = true;
        StartButtonGO.SetActive(true);
    }

    public override void OnPhotonCustomRoomPropertiesChanged(PhotonHashtable propertiesThatChanged)
    {
        foreach (DictionaryEntry pair in propertiesThatChanged)
        {
            roomCustomProperties[pair.Key] = pair.Value;
        }


        if (!String.IsNullOrEmpty(myName))
        {
            SetScore();
        }

        if (!GameStarted && (int)roomCustomProperties[PLAYERS_READY] == 2)
        {
            Clock.text = GameTime.ToString();
            ClockLabel.SetActive(true);
            PlacarLabel.SetActive(true);
            StartButtonGO.SetActive(false);
            WaitReady.SetActive(false);

            blockSpawnCoroutine = InstantateBlock(SpawnTime);
            StartCoroutine(blockSpawnCoroutine);

            if (PhotonNetwork.player.ID == 1)
            {
                PhotonView photonView = PhotonView.Get(this);
                photonView.RPC("StartClockCountDown", PhotonTargets.All);
            }

            GameStarted = true;
        }
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        foreach (GameObject go in prefabs)
        {
            Destroy(go);
        }
        ExitGame();
    }

    public override void OnLeftRoom()
    {
        GameStarted = false;
        StartScreen.SetActive(true);
        GameScreen.SetActive(false);

        WaitingLabel.enabled = true;
        StartButton.interactable = false;
        JoinRoomButton.interactable = false;
        StartButtonGO.SetActive(false);

        ClockLabel.SetActive(false);
        PlacarLabel.SetActive(false);
        StartButtonGO.SetActive(false);
        WaitReady.SetActive(false);
        Finish.SetActive(false);
    }

    #endregion

    #region Button Methods
    public void FindRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void ImReady()
    {
        WaitReady.SetActive(true);
        StartButtonGO.SetActive(false);
        roomCustomProperties[PLAYERS_READY] = (int)roomCustomProperties[PLAYERS_READY] + 1;
        PhotonNetwork.room.SetCustomProperties(roomCustomProperties);
    }

    public void ExitGame()
    {
        StopCoroutine(blockSpawnCoroutine);
        StopCoroutine(clockCoroutine);
        PhotonNetwork.LeaveRoom();
    }
    #endregion

    #region Game Flow Methods
    private void SetScore()
    {
        MyScoreLabel.text = roomCustomProperties[myName].ToString();
        OtherScoreLabel.text = roomCustomProperties[otherName].ToString();
    }

    public void Point()
    {
        roomCustomProperties[myName] = (int)roomCustomProperties[myName] + 1;
        PhotonNetwork.room.SetCustomProperties(roomCustomProperties);
    }

    private void SetupRoom()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        PhotonHashtable customProperties = new PhotonHashtable();
        customProperties[PLAYER_1] = 0;
        customProperties[PLAYER_2] = 0;
        customProperties[PLAYERS_READY] = 0;
        options.CustomRoomProperties = customProperties;
        PhotonNetwork.CreateRoom("", options, TypedLobby.Default);
    }

    private void FinishGame()
    {
        foreach (GameObject go in prefabs)
        {
            Destroy(go);
        }

        Finish.SetActive(true);
        StopCoroutine(blockSpawnCoroutine);
        GameStarted = false;

        if ((int)roomCustomProperties[myName] > (int)roomCustomProperties[otherName])
        {
            ResultLabel.text = "YOU WIN!";
        }
        else if ((int)roomCustomProperties[myName] < (int)roomCustomProperties[otherName])
        {
            ResultLabel.text = "YOU LOSE!";
        }
        else
        {
            ResultLabel.text = "DRAW GAME!";
        }

    }
    #endregion

    #region Events
    [PunRPC]
    void StartClockCountDown()
    {
        clockCoroutine = StartClock();
        StartCoroutine(clockCoroutine);
    }
    #endregion

    #region Coroutines
    private IEnumerator InstantateBlock(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            GameObject block = Instantiate(Prefab, new Vector3(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-4f, 4f), -1), transform.rotation);
            prefabs.Add(block);
            StartCoroutine(DestroyBlock(block));
        }
    }

    private IEnumerator DestroyBlock(GameObject block)
    {
        yield return new WaitForSeconds(LifeTime);
        prefabs.Remove(block);
        Destroy(block);
    }

    private IEnumerator StartClock()
    {
        int count = GameTime - 1;
        while (count > -1)
        {
            yield return new WaitForSeconds(1f);
            Clock.text = count.ToString();
            count--;
        }
        FinishGame();
    }
    #endregion



}
