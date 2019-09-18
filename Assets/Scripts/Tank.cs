using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Animations;

public class Tank : Destructable
{
    [Tooltip("The Player's UI GameObject Prefab")]
    [SerializeField]
    public GameObject PlayerUiPrefab;
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;
    bool Turret
    {
        get => MainGun is Turret;
    }
    public bool Player;
    [SerializeField]
    Weapon MainGun;
    [SerializeField]
    public Vector3 CameraOffset;
    [SerializeField]
    public GameObject CameraParent;
    [SerializeField]
    public CharacterController characterController;
    public Vector2 Jump = new Vector2(5, 5);
    public float speed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    private Vector3 moveDirection = Vector3.zero;
    ConstraintSource ConstraintSource;
    bool IsFiring
    {
        get => MainGun.IsFiring;
        set => MainGun.IsFiring = value;
    }
    // Start is called before the first frame update
    void Start()
    {
        CameraWork _cameraWork = this.gameObject.GetComponent<CameraWork>();


        if (_cameraWork != null)
        {
            if (photonView.IsMine)
            {
                _cameraWork.OnStartFollowing();
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
        if(photonView.IsMine && PhotonNetwork.IsConnected)
        {
            float Rotation = Input.GetAxis("Horizontal"), temp = moveDirection.y;
            transform.Rotate(Vector3.up * Rotation * 45 * Time.deltaTime);
            moveDirection = new Vector3(Input.GetAxis("HorizontalAlt"), 0.0f, Input.GetAxis("Vertical"));
            moveDirection *= speed;
            moveDirection = transform.rotation * moveDirection;
            // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
            // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
            // as an acceleration (ms^-2)
            moveDirection.y = temp;
            moveDirection.y -= gravity * Time.deltaTime;

            if (Input.GetButton("Jump") && Jump.x > 0)
            {
                moveDirection.y = jumpSpeed;
                Jump.x -= Time.deltaTime;
            }
            else if (Jump.x < Jump.y && characterController.isGrounded)
                Jump.x += Time.deltaTime;
            // Move the controller
            characterController.Move(moveDirection * Time.deltaTime);
            if (Turret)
                (MainGun as Turret).ProcessInputs();
        }
    }
    public override void Die()
    {
        Debug.Log("Blarg I am become " + HP + " HP");
        if (photonView.IsMine)
            GameManager.Instance.LeaveRoom();
        else
            base.Die();
    }

    private void Awake()
    {
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            Player = true;
            Tank.LocalPlayerInstance = this.gameObject;
            //Camera.main.transform.parent = CameraParent.transform;
            //Camera.main.transform.rotation = Quaternion.Euler(Vector3.zero);
            //Camera.main.transform.localPosition = CameraOffset;
            //Camera.main.nearClipPlane = 0.18f;
        }
        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        DontDestroyOnLoad(this.gameObject);
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
            stream.SendNext(HP);
        }
        else
        {
            // Network player, receive data
            MainGun.IsFiring = (bool)stream.ReceiveNext();
            this.HP = (float)stream.ReceiveNext();
        }
    }


    #endregion
    #region MonoBehaviorCallbacks
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
}
