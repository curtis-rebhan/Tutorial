using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Voting : MonoBehaviour
{
    Player player;
    public Text playerNameText;
    string id;
    public void Setup(Player p, string ID)
    {
        player = p;
        playerNameText.text = p.NickName;
        this.id = ID;
    }

    public void VoteDown()
    {
        GameManager.Instance.Vote(id, -1);
    }
    public void VoteUp()
    {
        GameManager.Instance.Vote(id, 1);

    }
}
