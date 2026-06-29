// 2台のカメラから取得した視線から、対象の位置を計算して描画する.
// 描画するのは対象の位置を正しく計算出来ているかどうかを検証するため.

using UnityEngine;

public class DrawPosition : MonoBehaviour
{
    // 2台のカメラから視線を取得するため.
    [SerializeField] private CameraOnDrone myCamera;
    [SerializeField] private CameraOnDrone otherCamera;

    void Update()
    {
        if (myCamera == null || otherCamera == null || !Ball.Exists())
        {
            return;
        }

        Ray gazeA = myCamera.GetGaze();
        Ray gazeB = otherCamera.GetGaze();

        if (!Ryougan.CanGetPosition(gazeA, gazeB))
        {
            return;
        }

        Vector3 ballPosition = Ryougan.GetPosition(gazeA, gazeB);
        DrawMarker(ballPosition);
    }

    // 描画するマーカーのサイズ.
    private const float markerSize = 0.5f;

    private void DrawMarker(Vector3 position)
    {
        Debug.DrawLine(position - Vector3.right * markerSize, position + Vector3.right * markerSize, Color.green);
        Debug.DrawLine(position - Vector3.up * markerSize, position + Vector3.up * markerSize, Color.green);
        Debug.DrawLine(position - Vector3.forward * markerSize, position + Vector3.forward * markerSize, Color.green);
    }
}
