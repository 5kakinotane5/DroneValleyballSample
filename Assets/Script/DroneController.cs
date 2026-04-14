using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DroneController: MonoBehaviour
{
    public float moveSpeed=5f; //前後左右のスピード
    public float verticalSpeed=3f; //上昇河口スピード
    public float turnSpeed=100f; //旋回スピード

    private Rigidbody rb;

    void Start(){
        rb=GetComponent<Rigidbody>();

        rb.useGravity=false; //勝手に落ちないようにする
        rb.freezeRotation=true; //物理衝突でドローンが勝手に転がらないようにする
    }

    void FixedUpdate(){
        var keyboard=Keyboard.current;
        if(keyboard==null) return;

        float h=0;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h=-1;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h=1;
        
        float v=0;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v=1;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v=-1;

        float upDown=0;
        if(keyboard.spaceKey.isPressed) upDown=1;
        else if (keyboard.leftShiftKey.isPressed) upDown=-1;

        float turn=0;
        if(keyboard.qKey.isPressed) turn=-1;
        if(keyboard.eKey.isPressed) turn=1;

        transform.Rotate(Vector3.up*turn*turnSpeed*Time.deltaTime);

        Vector3 moveinput=new Vector3(h,upDown*(verticalSpeed/moveSpeed),v);
        Vector3 worldVelocity=transform.TransformDirection(moveinput)*moveSpeed;

        rb.linearVelocity=worldVelocity;

    }
}
