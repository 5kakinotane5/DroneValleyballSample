/*
【DroneTranslater】
概要:
  Transform.Translate を使う方式の手動操作ドローンコントローラー。
  WASD/矢印キーで前後左右、Space で上昇、LeftShift で下降、Q/E で旋回。
  Rigidbody を使わないため物理衝突の影響を受けない。

他スクリプトとの関係:
  ・なし（単体アタッチ用）

【注意 ─ 削除候補】
  DroneController（Rigidbody ベース）と操作キー・機能が完全に重複している。
  バレーボールゲームはボールとの物理衝突が必要なため、DroneController を使用し
  このスクリプトは削除推奨。テスト・デバッグ目的以外には使用しないこと。
*/
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
