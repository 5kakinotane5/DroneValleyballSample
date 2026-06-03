/*
【DroneController】
概要:
  Rigidbody.linearVelocity を直接設定する方式の手動操作ドローンコントローラー。
  WASD/矢印キーで前後左右、Space で上昇、LeftShift で下降、Q/E で旋回。
  useGravity=false・freezeRotation=true で安定したホバリングを実現。

他スクリプトとの関係:
  ・なし（単体アタッチ用、プレイヤーコントローラとして機能）

注意:
  DroneTranslater と操作キーが同一で機能が重複している。
  DroneController は Rigidbody ベース（物理あり）、
  DroneTranslater は Transform.Translate ベース（物理なし）という違いがある。
  物理衝突が必要な本番シーンでは DroneController を使用し、DroneTranslater を削除すること。
*/
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
