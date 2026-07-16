// カメラからボールの位置を推定するためのモジュール. 
// Cameraから視線を取得し, 視線からボールの位置を計算して推定する処理を共通化するため.
// 既存のBallクラスを残す理由は, ドローンの自律飛行に関係のない箇所にはBallクラスを使うため. 

using UnityEngine;

public class BallFromCamera
{
    private static CameraOnDrone myCamera;
    private static CameraOnDrone otherCamera;

    public BallFromCamera(CameraOnDrone myCamera, CameraOnDrone otherCamera)
    {
        BallFromCamera.myCamera = myCamera;
        BallFromCamera.otherCamera = otherCamera;
    }

    // ボールの現在位置を返す.
    // 戻り値がNullableである理由は, ボールの位置を推定できない幾何学的条件が存在するため. 
    public Vector3? GetPosition()
    {
        if (myCamera == null || otherCamera == null)
        {
            throw new System.Exception("BallFromCamera: One or both cameras are null. Make sure to register the cameras before calling GetPosition.");
        }

        Ray gazeA = myCamera.GetGaze();
        Ray gazeB = otherCamera.GetGaze();

        return Ryougan.GetPosition(gazeA, gazeB);
    }
}
