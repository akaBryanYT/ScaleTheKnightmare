using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController2D controller;

    public float runSpeed = 40f;
    public int augSpeed = 0;

    float horizonatlMove = 0f;
    bool jump = false;


    // Update is called once per frame
    void Update()
    {
        horizonatlMove = Input.GetAxisRaw("Horizontal") * runSpeed;

        if(Input.GetButtonDown("Jump"))
        {
            jump = true;
        }    
    }

    void FixedUpdate()
    {
        controller.Move(horizonatlMove * Time.fixedDeltaTime, false, jump);

        jump = false;
    }
}