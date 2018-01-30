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

    public GameObject StartScreen;
    public GameObject GameScreen;
    public GameObject StartButtonGO;
    public GameObject PlacarLabel;
    public GameObject ClockLabel;
    public GameObject Prefab;
    public GameObject Finish;

    public Button JoinRoomButton;
    public Button StartButton;

    public Text WaitingLabel;
    public Text MyScoreLabel;
    public Text OtherScoreLabel;
    public Text Clock;

    public float SpawnTime;
    public float LifeTime;
    public int GameTime;

    private IEnumerator blockSpawnCoroutine;

    private PhotonHashtable roomCustomProperties;

    private string myName;
    private string otherName;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings("v1.0");
        this.roomCustomProperties = new PhotonHashtable();
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
        this.JoinRoomButton.interactable = true;
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        SetupRoom();
    }

    public override void OnJoinedRoom()
    {
        this.roomCustomProperties = PhotonNetwork.room.CustomProperties;
        this.StartScreen.SetActive(false);
        this.GameScreen.SetActive(true);
        if (PhotonNetwork.room.PlayerCount == 2)
        {
            this.WaitingLabel.enabled = false;
            this.StartButton.interactable = true;
            this.StartButtonGO.SetActive(true);
            myName = PLAYER_2;
            otherName = PLAYER_1;
        } else
        {
            myName = PLAYER_1;
            otherName = PLAYER_2;
        }
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        this.WaitingLabel.enabled = false;
        this.StartButton.interactable = true;
        this.StartButtonGO.SetActive(true);
    }

    public override void OnPhotonCustomRoomPropertiesChanged(PhotonHashtable propertiesThatChanged)
    {
        foreach (DictionaryEntry pair in propertiesThatChanged)
        {
            this.roomCustomProperties[pair.Key] = pair.Value;
        }
        if (myName != null)
        {
            SetScore();
        }
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        StopCoroutine(blockSpawnCoroutine);
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        this.StartScreen.SetActive(true);
        this.GameScreen.SetActive(false);

        this.WaitingLabel.enabled = true;
        this.StartButton.interactable = false;
        this.StartButtonGO.SetActive(false);

        this.ClockLabel.SetActive(false);
        this.PlacarLabel.SetActive(false);
        this.StartButtonGO.SetActive(true);
    }

    #endregion

    #region Button Methods
    public void FindRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void ImReady()
    {
        SetScore();

        this.Clock.text = GameTime.ToString();
        this.ClockLabel.SetActive(true);
        this.PlacarLabel.SetActive(true);
        this.StartButtonGO.SetActive(false);

        blockSpawnCoroutine = InstantateBlock(SpawnTime);
        StartCoroutine(blockSpawnCoroutine);
        StartCoroutine(StartClock());
    }
    #endregion

    #region Game Flow Methods
    private void SetScore()
    {
        this.MyScoreLabel.text = this.roomCustomProperties[myName].ToString();
        this.OtherScoreLabel.text = this.roomCustomProperties[otherName].ToString();
    }

    public void Point()
    {
        this.roomCustomProperties[myName] = (int)this.roomCustomProperties[myName] + 1;
        PhotonNetwork.room.SetCustomProperties(this.roomCustomProperties);
    }

    private void SetupRoom()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        PhotonHashtable customProperties = new PhotonHashtable();
        customProperties[PLAYER_1] = 0;
        customProperties[PLAYER_2] = 0;
        options.CustomRoomProperties = customProperties;
        PhotonNetwork.CreateRoom("", options, TypedLobby.Default);
    }

    private void FinishGame()
    {
        this.Finish.SetActive(true);
        StopCoroutine(blockSpawnCoroutine);
    }
    #endregion

    #region Coroutines
    private IEnumerator InstantateBlock(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            GameObject block = Instantiate(Prefab, new Vector3(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-4f, 4f), -1), transform.rotation);
            StartCoroutine(DestroyBlock(block));
        }
    }

    private IEnumerator DestroyBlock(GameObject block)
    {
        yield return new WaitForSeconds(LifeTime);
        Destroy(block);
    }

    private IEnumerator StartClock()
    {
        int count = GameTime - 1;
        while (count > -1)
        {
            yield return new WaitForSeconds(1f);
            this.Clock.text = count.ToString();
            count--;
        }
        FinishGame();
    }
    #endregion



}
