using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class M_Player : MonoBehaviour
{
    [SerializeField]
	GameObject AimingReticule, LookingReticule;
    Tank Tank;
    CharacterController characterController;

    private Vector3 moveDirection = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        Tank = GetComponent<Tank>();
        characterController = Tank.GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {

        
    }

    private void FixedUpdate()
    {
        
    }
}
