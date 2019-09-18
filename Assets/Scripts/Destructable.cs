using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Destructable : MonoBehaviourPunCallbacks, IPunObservable
{
    public float HP = 100;
    public virtual float Hit(float Damage)
    {
        HP -= Damage;
        if (HP <= 0)
            Die();
        return HP;
    }
    public virtual void Die()
    {
        Destroy(gameObject);
    }

    #region IPunObservable implementation


    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(HP);
        }
        else
        {
            this.HP = (float)stream.ReceiveNext();
        }
    }


    #endregion
}
