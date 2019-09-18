using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Turret : Weapon
{
    [SerializeField]
    GameObject XAxis, YAxis, Beam;
    [SerializeField]
    Tank Tank;
    [SerializeField]
    float TurnSpeed = 90, TurretRange = 20, Damage = 40;
    public string ControlAxis = "Fire1";
    public void SetFiring(bool isFiring)
    {
        IsFiring = isFiring;
    }
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
                if((temp = hit.collider.transform.root.GetComponent<Destructable>()) != null)
                {
                    temp.Hit(Damage * Time.deltaTime);
                }
            }
            Beam.transform.localScale = new Vector3(Beam.transform.localScale.x, Beam.transform.localScale.y, distance);
            Beam.transform.localPosition = new Vector3(Beam.transform.localPosition.x, Beam.transform.localPosition.y, distance / 2);
        }
    }
    
    public void ProcessInputs()
    {
        float horizontalTurret = Input.GetAxis("Mouse X") * 900;
        if (Mathf.Abs(horizontalTurret) > TurnSpeed)
            horizontalTurret = horizontalTurret < 0 ? -TurnSpeed : TurnSpeed;
        transform.Rotate(Vector3.up * horizontalTurret * Time.deltaTime);
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
        if(Input.GetButtonUp("Fire2"))
        {
            IsFiring = !IsFiring;
        }
    }
}
