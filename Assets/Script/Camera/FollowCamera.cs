// ドローンにカメラを追従させるためのスクリプト。臨場感を出すために追従させる。

using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    // 追従する対象のドローンオブジェクト
    public Transform targetDrone;

    // ドローンに対するカメラの相対位置
    private Vector3 offset = new Vector3(8, 1.5f, 0);

    void LateUpdate()
    {
        transform.position = targetDrone.position + offset;
        transform.LookAt(targetDrone);
    }
}
