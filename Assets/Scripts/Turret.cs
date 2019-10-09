using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Turret : Weapon
{
    [SerializeField]
    GameObject XAxis, YAxis, Beam, Barrel;
    [SerializeField]
    Tank Tank;
    [SerializeField]
    float TurnSpeed = 90, TurretRange = 20, Damage = 40;
    public Missile MissilePrefab;
    public string ControlAxis = "Fire1";
    public bool IsMissileFiring;
    private bool missileCooldown;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void Awake()
    {
        if (Beam != null)
            Beam.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        // trigger Beams active state
        if (Beam != null && IsFiring != Beam.activeSelf)
        {
            Beam.SetActive(IsFiring);
        }
        if(IsFiring)
        {
            RaycastHit hit;
            float distance = TurretRange;
            if (Physics.Raycast(Beam.transform.parent.position, Beam.transform.forward, out hit, TurretRange))
            {
                distance = hit.distance;
                Destructable temp;
                if((temp = hit.collider.transform.root.GetComponent<Destructable>()) != null || (temp = hit.collider.transform.GetComponent<Destructable>()) != null)
                {
                    temp.Hit(Damage * Time.deltaTime, Tank);

                }
            }
            Beam.transform.localScale = new Vector3(Beam.transform.localScale.x, Beam.transform.localScale.y, distance);
            Beam.transform.localPosition = new Vector3(Beam.transform.localPosition.x, Beam.transform.localPosition.y, distance / 2);
        }
        if(IsMissileFiring && !missileCooldown)
        {
            Instantiate(MissilePrefab).Fire(Barrel, Tank);
            missileCooldown = true;
            if(Tank.Player)
                Invoke("ResetMissileCoolDown", 5);
            else
                Invoke("ResetMissileCoolDown", 3);
        }
    }
    public void ResetMissileCoolDown()
    {
        missileCooldown = false;
        IsMissileFiring = false;
    }
    public void ProcessInputs()
    {
        float horizontalTurret = Input.GetAxis("Mouse X") * TurnSpeed, verticalTurret = Input.GetAxis("Mouse Y") * TurnSpeed;
        YAxis.transform.Rotate(Vector3.up * horizontalTurret * Time.deltaTime);
        XAxis.transform.Rotate(Vector3.right * -verticalTurret * Time.deltaTime);
        if (Input.GetButtonDown(ControlAxis))
        {
            if (!IsFiring)
            {
                IsFiring = true;
            }
        }
        if (Input.GetButtonUp(ControlAxis))
        {
            if (IsFiring)
            {
                IsFiring = false;
            }
        }
        if (Input.GetButtonUp("Fire2"))
        {
            if (!IsMissileFiring)
            {
                IsMissileFiring = true;
            }
        }
    }
}
