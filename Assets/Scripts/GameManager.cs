using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region Public Fields
    public static GameManager Instance;
    public GameObject playerPrefab;
    public static float KillingFloor = -10;
    public List<KeyValuePair<string, int>> graveyard = new List<KeyValuePair<string, int>>();
    public static string Rankings;
 
    #endregion

    #region Private Fields
    bool Lost = false;
    [SerializeField]
    Text RankingText;
    bool update = true;
    #endregion

    #region Photon Callbacks


    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(1);
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", newPlayer.NickName); // not seen if you're the player connecting
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom

            //LoadArena();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", otherPlayer.NickName); // seen when other disconnects
        

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            //LoadArena();
        }
    }
    #endregion


    #region Public Methods

    public static void ResetRanking()
    {
        Rankings = "";
    }
    public void LeaveRoom(bool lost)
    {
        this.Lost = lost;
        LeaveRoom();
    }
    public void LeaveRoom()
    {
        GetRankings();
        update = false;
        PhotonNetwork.LeaveRoom();
    }

    public string GetRankings()
    {
        List<KeyValuePair<string, int>> pairs = new List<KeyValuePair<string, int>>();

        foreach (Tank tank in Tank.tanks)
        {
            if(tank != null)
            pairs.Add(new KeyValuePair<string, int>(tank.NickName, tank.Kills));
        }
        foreach(KeyValuePair<string, int> tank in graveyard)
        {
            pairs.Add(tank);
        }
        pairs.Sort((x, y) => (y.Value.CompareTo(x.Value)));
        string rankingString = "";
        int i = 0;
        foreach(KeyValuePair<string, int> pair in pairs)
        {
            rankingString += ++i + ": " + pair.Key + ", Kills: " + pair.Value;
            rankingString += "\n";
        }
        Rankings = rankingString;
        RankingText.text = "Score:\n" + Rankings;
        return Rankings;
    }

    #endregion
    #region Private Methods


    void LoadArena()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
        }
        Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
        PhotonNetwork.LoadLevel("Room for " + PhotonNetwork.CurrentRoom.PlayerCount);
    }

    private void Start()
    {
        Instance = this;
        ResetRanking();
        if (playerPrefab == null)
        {
            Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
        }
        else
        {
            if (Tank.LocalPlayerInstance == null)
            {
                Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
                // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                PhotonNetwork.LocalPlayer.TagObject = PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
            }
            else
            {
                Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
            }
        }
    }
    private void Update()
    {
        if (!update)
            return;
        GetRankings();
        RankingText.text = Rankings;
    }
    #endregion
}
