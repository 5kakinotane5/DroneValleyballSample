// 2つのカメラからBallFromCameraインスタンスを生成し, データを保持する処理を分離するため. 
// このモジュールを使う側は, このインスタンスを持つだけでボールの位置を推定できる. 
// そうしないと, カメラからBallFromCameraを生成してそれをprivate変数に保持するというコードを
// 何か所にも書くことになる. 

using UnityEngine;

public class BallGetterOnDrone : MonoBehaviour
{
    [SerializeField] private CameraOnDrone myCamera;
    [SerializeField] private CameraOnDrone otherCamera;

    private BallFromCamera ball;

    public void Start()
    {
        ball = new BallFromCamera(myCamera, otherCamera);
    }

    public Vector3? GetPosition()
    {
        return ball.GetPosition();
    }
}