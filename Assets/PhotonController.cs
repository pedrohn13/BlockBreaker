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
        this.roomCustomProperties = new PhotonHashtable();
        this.prefabs = new List<GameObject>();
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
        }
        else
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

        
        if (!String.IsNullOrEmpty(myName))
        {
            SetScore();
        }

        if (!GameStarted && (int)this.roomCustomProperties[PLAYERS_READY] == 2)
        {
            this.Clock.text = GameTime.ToString();
            this.ClockLabel.SetActive(true);
            this.PlacarLabel.SetActive(true);
            this.StartButtonGO.SetActive(false);
            this.WaitReady.SetActive(false);

            blockSpawnCoroutine = InstantateBlock(SpawnTime);
            StartCoroutine(blockSpawnCoroutine);

            PhotonView photonView = PhotonView.Get(this);
            photonView.RPC("StartClockCountDown", PhotonTargets.All);

            GameStarted = true;
        }
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        StopCoroutine(blockSpawnCoroutine);
        StopCoroutine(clockCoroutine);
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        foreach (GameObject go in this.prefabs)
        {
            Destroy(go);
        }
        this.StartScreen.SetActive(true);
        this.GameScreen.SetActive(false);

        this.WaitingLabel.enabled = true;
        this.StartButton.interactable = false;
        this.StartButtonGO.SetActive(false);

        this.ClockLabel.SetActive(false);
        this.PlacarLabel.SetActive(false);
        this.StartButtonGO.SetActive(false);
        this.WaitReady.SetActive(false);
    }

    #endregion

    #region Button Methods
    public void FindRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void ImReady()
    {
        this.WaitReady.SetActive(true);
        this.StartButtonGO.SetActive(false);
        this.roomCustomProperties[PLAYERS_READY] = (int)this.roomCustomProperties[PLAYERS_READY] + 1;
        PhotonNetwork.room.SetCustomProperties(this.roomCustomProperties);
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
        Debug.Log(myName + "-" + otherName);
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
        customProperties[PLAYERS_READY] = 0;
        options.CustomRoomProperties = customProperties;
        PhotonNetwork.CreateRoom("", options, TypedLobby.Default);
    }

    private void FinishGame()
    {
        this.Finish.SetActive(true);
        StopCoroutine(blockSpawnCoroutine);
        GameStarted = false;

        if ((int) this.roomCustomProperties[myName] > (int) this.roomCustomProperties[otherName])
        {
            this.ResultLabel.text = "YOU WIN!";
        } else if ((int)this.roomCustomProperties[myName] < (int)this.roomCustomProperties[otherName])
        {
            this.ResultLabel.text = "YOU LOSE!";
        } else
        {
            this.ResultLabel.text = "DRAW GAME!";
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
            this.Clock.text = count.ToString();
            count--;
        }
        FinishGame();
    }
    #endregion



}
