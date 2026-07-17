// 2つのカメラからBallFromCameraインスタンスを生成し, データを保持する処理を分離するため. 
// このモジュールを使う側は, このインスタンスを持つだけでボールの位置を推定できる. 
// そうしないと, カメラからBallFromCameraを生成してそれをprivate変数に保持するというコードを
// 何か所にも書くことになる. 

using UnityEngine;

public class BallEstimator : MonoBehaviour
{
    [SerializeField] private CameraOnDrone myCamera;
    [SerializeField] private CameraOnDrone otherCamera;

    void FixedUpdate()
    {
        UpdateVelocity();
    }

    public Vector3? GetPosition()
    {
        if (myCamera == null || otherCamera == null)
        {
            throw new System.Exception("BallEstimator: One or both cameras are null. Make sure to register the cameras before calling GetPosition.");
        }

        Ray gazeA = myCamera.GetGaze();
        Ray gazeB = otherCamera.GetGaze();

        return Ryougan.GetPosition(gazeA, gazeB);
    }

    public Vector3 GetVelocity()
    {
        return _ballVelocity;
    }

    private bool _hasLastBallPos;
    private Vector3 _ballVelocity;
    private Vector3 _lastBallPos;

    private void UpdateVelocity()
    {
        // Ballクラスに頼らないようにしたい.
        if (!Ball.Exists())
        {
            _hasLastBallPos = false;
            _ballVelocity = Vector3.zero;
            return;
        }

        Vector3? currentPos = GetPosition();
        if (!currentPos.HasValue)
        {
            _hasLastBallPos = false;
            _ballVelocity = Vector3.zero;
            return;
        }

        if (_hasLastBallPos)
            _ballVelocity = (currentPos.Value - _lastBallPos) / Time.fixedDeltaTime;

        _lastBallPos = currentPos.Value;
        _hasLastBallPos = true;
    }
}