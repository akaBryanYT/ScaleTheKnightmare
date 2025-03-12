using UnityEngine;

public class VerticalPlatform : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public PlatformEffector2D effector;
    public float waitTime;

    void Start()
    {
        effector = GetComponent<PlatformEffector2D>();
    }

    void Update(){

        if(Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.S)){
            waitTime = 0.5f;
            effector.rotationalOffset = 0;
        }

        if(Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))    
        {
            if(waitTime  <= 0){
                effector.rotationalOffset = 180f;
                waitTime = 0.5f;
            }else{
                waitTime -= Time.deltaTime;
            }
        }

        if(Input.GetButtonDown("Jump")){
            effector.rotationalOffset = 0;
        }
    }
}
