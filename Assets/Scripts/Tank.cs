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
            return temp >= 0 && temp <= HoverHeight;
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
    public float jumpAcceleration = 8.0f;
    public float gravity = 20.0f;
    public float BrakingForce = 0.8f;
    public float torque = 1f;
    public Tank LastHit;
    public int Kills = 0;
    public static List<Tank> tanks = new List<Tank>();
    bool dead;
    public string NickName
    {
        get => photonView.Owner.NickName;

    }
    #endregion
    private Vector3 moveDirection = Vector3.zero;
    private float HoverHeight = .4f, maxAngularVelocity = 5, maxVelocity = 90;
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
        if (_cameraWork != null)
        {
            if (photonView.IsMine)
            {
                //_cameraWork.OnStartFollowing();
                Camera.main.transform.parent = CameraParent.transform;
                Camera.main.transform.localPosition = CameraOffset;
                Camera.main.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }
        }
        else
        {
            Debug.LogError("<Color=Red><a>Missing</a></Color> CameraWork Component on playerPrefab.", this);
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
        RaycastHit hit;
        if (Physics.BoxCast(transform.position + Vector3.up * 0.2f, new Vector3(0.5f, 0.1f, 1), -transform.up, out hit, Quaternion.identity, 20f))
            Height = hit.distance - 0.2f;
        else
            Height = -1;
        if (photonView.IsMine && PhotonNetwork.IsConnected)
        {
            if (Turret)
                (MainGun as Turret).ProcessInputs();
            float h = Input.GetAxis("HorizontalAlt"), v = Input.GetAxis("Vertical"), turn = Input.GetAxis("Horizontal");
            if (turn != 0)
                rb.AddTorque(transform.up * torque * rb.mass * turn * Time.deltaTime);
            
            if (Input.GetButton("Jump") && Jump.x > 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpAcceleration * Time.deltaTime, rb.velocity.z);
                rb.useGravity = false;
            }
            else
                rb.useGravity = true;

            rb.AddForce((transform.forward * v + transform.right * h) * rb.mass * Time.deltaTime * speed);
            if(rb.velocity.magnitude > maxVelocity)
            {
                rb.velocity = rb.velocity.normalized * maxVelocity;
            }

        }
        
        if (transform.position.y <= GameManager.KillingFloor)
            Die();
    }
    private void LateUpdate()
    {
        //freeze rotation
        transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
        if (Height > -.1f && Height < HoverHeight)
        {
            if (rb.velocity.y < 0)
                rb.velocity -= new Vector3(0, rb.velocity.y, 0);

            transform.position += new Vector3(0, HoverHeight - Height);
        }

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
    public override void Die()
    {
        if (dead)
            return;
        dead = true;
        tanks.Remove(this);
        if(LastHit != null)
            LastHit.Kills++;
        GameManager.Instance.graveyard.Add(new KeyValuePair<string, int>(photonView.Owner.NickName, Kills));
        if (photonView.IsMine)
        {
            GameManager.Instance.LeaveRoom(true);
        }
        else
        {
            base.Die();
        }
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
            stream.SendNext(IsFiring);
            if (Turret)
                stream.SendNext((MainGun as Turret).IsMissileFiring);
            stream.SendNext(HP);
        }
        else
        {
            // Network player, receive data
            MainGun.IsFiring = (bool)stream.ReceiveNext();
            if(Turret)
                (MainGun as Turret).IsMissileFiring = (bool)stream.ReceiveNext();
            this.HP = (float)stream.ReceiveNext();
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
}
