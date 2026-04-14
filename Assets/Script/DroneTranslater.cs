using UnityEngine;
using UnityEngine.InputSystem;

public class DroneTranslater: MonoBehaviour
{
    public float moveSpeed=15f; //前後左右のスピード
    public float verticalSpeed=3f; //上昇河口スピード
    public float turnSpeed=100f; //旋回スピード

    void Update(){
        var keyboard=Keyboard.current;
        if(keyboard==null) return;

        float h=0;
        float v=0;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h=-1;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h=1;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v=1;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v=-1;

        float upDown=0;
        if(keyboard.spaceKey.isPressed) upDown=1;
        else if (keyboard.leftShiftKey.isPressed) upDown=-1;

        float turn=0;
        if(keyboard.qKey.isPressed) turn=-1;
        if(keyboard.eKey.isPressed) turn=1;

        Vector3 moveDir=new Vector3(h,upDown*(verticalSpeed/moveSpeed),v);

        transform.Translate(moveDir*moveSpeed*Time.deltaTime);

        transform.Rotate(Vector3.up*turn*turnSpeed*Time.deltaTime);
    }
}
