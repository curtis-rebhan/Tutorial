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
    public Tank localTank;
    public static float KillingFloor = -10;

    /// <summary>
    /// Fancy memory management, so long as i keep the rankingstruct in graveyard on dead tanks the code magically works
    /// </summary>
    public List<RankingStruct> graveyard = new List<RankingStruct>();
    public static string Rankings;
    public static int votes;
    public GameObject votePrefab;
    public GameObject canvas;
    public static bool Lost = false;
    public int deaths = 0;
    public struct RankingStruct : IEquatable<RankingStruct>
    {
        public static bool operator ==(RankingStruct left, RankingStruct right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(RankingStruct left, RankingStruct right)
        {
            return !left.Equals(right);
        }
        public int Kills;
        public int Death;
        public string Nickname;
        public int Honor;
        public RankingStruct(int kills = 0, string nickname = "", int death = 0, int honor = 0)
        {
            Kills = kills;
            Death = death;
            Nickname = nickname;
            Honor = honor;
        }

        public override bool Equals(object obj)
        {
            return obj is RankingStruct && Equals((RankingStruct)obj);
        }

        public bool Equals(RankingStruct other)
        {
            return Kills == other.Kills &&
                   Death == other.Death &&
                   Nickname == other.Nickname &&
                   Honor == other.Honor;
        }

        public override int GetHashCode()
        {
            var hashCode = -386364561;
            hashCode = hashCode * -1521134295 + Kills.GetHashCode();
            hashCode = hashCode * -1521134295 + Death.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Nickname);
            hashCode = hashCode * -1521134295 + Honor.GetHashCode();
            return hashCode;
        }
    }
    #endregion

    #region Private Fields
    [SerializeField]
    Text RankingText;
    int playerGraveyardIndex = -1;
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
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            LeaveRoom(false);

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            //LoadArena();
        }
    }
    #endregion


    #region Public Methods
    public void Vote(string id, int vote)
    {
        photonView.RPC("RemoteVote", RpcTarget.AllBuffered, id, vote);
    }
    [PunRPC]
    public void RemoteVote(string id, int vote)
    {
        if (PhotonNetwork.LocalPlayer.UserId == id)
        {
            votes += vote;
            if(localTank == null)
            {
                Debug.Log("Fail");
                return;
            }
            localTank.RankingStruct.Honor = votes;
            //if (localTank.dead)//proc serialize view
            //    localTank.HP--;
        }
    }
    public void PlayerDeath()
    {
        int i = 0;
        foreach(KeyValuePair<int, Player> P in PhotonNetwork.CurrentRoom.Players)
        {
            GameObject prefab = Instantiate(votePrefab);
            prefab.transform.SetParent(canvas.transform);
            prefab.transform.localPosition = new Vector3(0, 20 + (-i++) * 30);
            prefab.GetComponent<Voting>().Setup(P.Value, P.Value.UserId);
        }
    }
    public static void ResetRanking()
    {
        Debug.Log("Resetting Rankings");
        Rankings = "";
    }
    public void LeaveRoom(bool lost)
    {
        Lost = lost;
        LeaveRoom();
    }
    public void LeaveRoom()
    {
        update = false;
        if (localTank != null)
            localTank.photonView.RPC("DestroyTank", RpcTarget.AllBuffered);
        PhotonNetwork.LeaveRoom();
    }

    public string GetRankings()
    {
        List<RankingStruct> pairs = new List<RankingStruct>();
        //graveyard.Sort((x, y) => (y.Death.CompareTo(x.Death)));
        foreach (Tank tank in Tank.tanks)
        {
            if(tank != null)
                pairs.Add(tank.RankingStruct);
        }
        foreach(RankingStruct tank in graveyard)
        {
            pairs.Add(tank);
        }
        pairs.Sort((x, y) => (y.Kills.CompareTo(x.Kills))); // Living tanks Compare by kills
        //pairs.AddRange(graveyard); //add dead players
        string rankingString = "";
        int i = 0;
        foreach(RankingStruct pair in pairs)
        {
            rankingString += ++i + ": " + pair.Nickname + ", Kills: " + pair.Kills + " Honor: " + pair.Honor;
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
        RankingText.text = Rankings;
        if (!update)
            return;
        GetRankings();
    }
    
    #endregion
}
