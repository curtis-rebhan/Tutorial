using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Animations;

public class Tank : Destructable
{
    #region Public Fields
    [Tooltip("The Player's UI GameObject Prefab")]
    [SerializeField]
    public GameObject PlayerUiPrefab;
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;
    public bool Player
    {
        get => photonView.IsMine;
    }
    public float Height;
    public bool IsGrounded
    {
        get
        {
            float temp = Height;
            return temp >= 0 && temp <= HoverHeight +0.1f;
        }
    }
    [SerializeField]
    Weapon MainGun;
    [SerializeField]
    public Vector3 CameraOffset;
    [SerializeField]
    public GameObject CameraParent;
    [SerializeField]
    public Rigidbody rb;
    public Vector2 Jump = new Vector2(5, 5);
    public float speed = 6.0f;
    public float jumpVelocity = 8.0f;
    public float gravity = 20.0f;
    public float BrakingForce = 0.8f;
    public float torque = 1f;
    public GameManager.RankingStruct RankingStruct;
    public Tank LastHit;
    public static List<Tank> tanks = new List<Tank>();
    public bool dead;
    public string NickName
    {
        get => photonView.Owner.NickName;

    }
    public int Honor
    {
        get => RankingStruct.Honor;
        set
        {
            honor = value;
            RankingStruct.Honor = value;
        }
    }
    #endregion
    int honor;
    private Vector3 moveDirection = Vector3.zero;
    private float HoverHeight = .4f, maxAngularVelocity = 5, maxVelocity = 10;
    Vector3 velocity;
    float drag = 0.9f;
    Vector3 LastFrame;
    ConstraintSource ConstraintSource;
    bool Turret
    { 
        get => MainGun is Turret;
    }
    bool IsFiring
    {
        get => MainGun == null ? false : MainGun.IsFiring;
        set => MainGun.IsFiring = value;
    }
    #region Monobehavior Callbacks
    // Start is called before the first frame update
    void Start()
    {
        CameraWork _cameraWork = this.gameObject.GetComponent<CameraWork>();
        rb.maxAngularVelocity = maxAngularVelocity;
        tanks.Add(this);
        RankingStruct = new GameManager.RankingStruct();
        if (photonView.IsMine)
        {
            GameManager.Instance.localTank = this;
            GameManager.ResetRanking();
            Camera.main.transform.parent = CameraParent.transform;
            Camera.main.transform.localPosition = CameraOffset;
            Camera.main.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            RankingStruct.Nickname = PhotonNetwork.LocalPlayer.NickName;
            //if (_cameraWork != null)
            //{
            //    _cameraWork.OnStartFollowing();
            //}
            //else
            //{
            //    Debug.LogError("<Color=Red><a>Missing</a></Color> CameraWork Component on playerPrefab.", this);
            //}

        }
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        if (PlayerUiPrefab != null)
        {
            GameObject _uiGo = Instantiate(PlayerUiPrefab);
            _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
        }
        else
        {
            Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if (dead)
        //    return;
        RaycastHit hit;
        if (Physics.BoxCast(transform.position + Vector3.up * 0.2f, new Vector3(0.5f, 0.1f, 1), -transform.up, out hit, Quaternion.identity, 20f))
            Height = hit.distance - 0.2f;
        else
            Height = -1;
        if (photonView.IsMine && PhotonNetwork.IsConnected)
        {
            velocity *= drag;
            if (Turret)
                (MainGun as Turret).ProcessInputs();
            float h = Input.GetAxis("HorizontalAlt"), v = Input.GetAxis("Vertical"), turn = Input.GetAxis("Horizontal");
            if (turn != 0)
                transform.Rotate(new Vector3(0, turn));

            if (Input.GetButton("Jump") && Jump.x > 0)
            {
                velocity.y = jumpVelocity;
                Jump.x -= Time.deltaTime;
            }
            else if (!IsGrounded)
                velocity.y -= gravity * Time.deltaTime;
            else
                Jump.x += Time.deltaTime; 

            velocity += (transform.forward * v + transform.right * h) * rb.mass * Time.deltaTime * speed;
            if (Height > -.1f && Height <= HoverHeight)
            {
                if (velocity.y < 0)
                    velocity.y = 0;

                transform.position += new Vector3(0, HoverHeight - Height);
            }
            if (h == 0 && v == 0 && velocity.magnitude <= 0.1f)
                velocity = Vector3.zero;
            if (velocity.magnitude > maxVelocity)
            {
                velocity = velocity.normalized * maxVelocity;
            }
            transform.position += velocity * Time.deltaTime;
        }
        else
        {
            velocity = transform.position - LastFrame;
            velocity /= Time.deltaTime;
            LastFrame = transform.position;
        }

        if (transform.position.y <= GameManager.KillingFloor)
            Die();
    }
    private void LateUpdate()
    {
        //freeze rotation
        //transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);

    }
    private void Awake()
    {
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            Tank.LocalPlayerInstance = this.gameObject;
            //Camera.main.transform.parent = CameraParent.transform;
            //Camera.main.transform.rotation = Quaternion.Euler(Vector3.zero);
            //Camera.main.transform.localPosition = CameraOffset;
            //Camera.main.nearClipPlane = 0.18f;
        }
        // #Critical #NotSoMuch
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        //DontDestroyOnLoad(this.gameObject);
    }
    #endregion
    [PunRPC]
    public void DestroyTank()
    {
        Destroy(gameObject);
    }
    public override void Die()
    {
        if (dead)
            return;
        dead = true;
        GameManager.Instance.deaths++;
        RankingStruct.Death = GameManager.Instance.deaths;
        if(LastHit != null && LastHit.photonView.IsMine)//addKill to tank who LastHit me
        {
            LastHit.RankingStruct.Kills++;
        }

        if (photonView.IsMine)
        {
            GameManager.Lost = true;
            Camera.main.transform.parent = null;
            Camera.main.transform.position = Vector3.up * 10 - Vector3.forward * 20;
            Camera.main.transform.rotation = Quaternion.identity;
            GameManager.Instance.PlayerDeath();
        }
        GameManager.Instance.graveyard.Add(RankingStruct);
        transform.position = -Vector3.up * 300;
    }

    #region Scene
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
    {
        this.CalledOnLevelWasLoaded(scene.buildIndex);
    }
    #endregion
    #region IPunObservable implementation


    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(honor);
            stream.SendNext(velocity);
            stream.SendNext(IsFiring);
            if (Turret)
                stream.SendNext((MainGun as Turret).IsMissileFiring);
            stream.SendNext(HP);
            stream.SendNext(RankingStruct.Death);
            stream.SendNext(RankingStruct.Nickname);
            stream.SendNext(RankingStruct.Kills);
        }
        else if(stream.IsReading)
        {
            // Network player, receive data
            Honor = (int)stream.ReceiveNext();
            this.velocity = (Vector3)stream.ReceiveNext();
            MainGun.IsFiring = (bool)stream.ReceiveNext();
            if(Turret)
                (MainGun as Turret).IsMissileFiring = (bool)stream.ReceiveNext();
            this.HP = (float)stream.ReceiveNext();
            RankingStruct.Death = (int)stream.ReceiveNext();
            RankingStruct.Nickname = (string)stream.ReceiveNext();
            RankingStruct.Kills = (int)stream.ReceiveNext();

        }
    }

    
    #endregion
    #region IPunMonoBehaviorCallbacks
    /// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
    void OnLevelWasLoaded(int level)
    {
        this.CalledOnLevelWasLoaded(level);
    }


    void CalledOnLevelWasLoaded(int level)
    {
        GameObject _uiGo = Instantiate(this.PlayerUiPrefab);
        _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
        // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
        if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
        {
            transform.position = new Vector3(0f, 5f, 0f);
        }
    }
    public override void OnDisable()
    {
        // Always call the base to remove callbacks
        base.OnDisable();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion

    public override float Hit(float damage, Tank origin)
    {
        LastHit = origin;
        HP -= damage;
        if (HP <= 0 || rb.mass <= 0)
            Die();
        return HP;
    }
    Vector3 RelativeVelocity(Vector3 otherVelocity)
    {
        return velocity - otherVelocity;
    }
    private void OnCollisionEnter(Collision collision)
    {
        Tank temp;
        if ((temp = collision.transform.root.GetComponent<Tank>()) != null || (temp = collision.transform.GetComponent<Tank>()) != null)
        {
            if (Player)
            {
                velocity = (transform.position - collision.transform.position).normalized * RelativeVelocity(temp.velocity).magnitude;
            }

        }
        else
            velocity = (transform.position - collision.transform.position).normalized * RelativeVelocity(Vector3.zero).magnitude;
    }
    private void OnCollisionStay(Collision collision)
    {
        if(velocity.magnitude < maxVelocity / 2 && collision.relativeVelocity.magnitude < maxVelocity)
            velocity = (transform.position - collision.transform.position).normalized * (maxVelocity / 2);

        velocity = (transform.position - collision.transform.position).normalized * (velocity.magnitude);

    }
}
