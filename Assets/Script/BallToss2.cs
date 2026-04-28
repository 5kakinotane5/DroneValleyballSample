using UnityEngine;

public class BallToss2 : MonoBehaviour
{
    [Header("判定するターゲットのタグ")]    
    public string targetTag = "injectionball"; // タグ名を統一

    [Header("ドローンの速度の何倍で飛ばすか")]
    public float tossBoost = 2f;

    [Header("最低限の跳ね上がり速度 (m/s)")]
    public float minTossSpeed = 5f;

    private Rigidbody droneRb;

    void Start()
    {
        droneRb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();

            if (ballRb != null)
            {
                //一度完全停止
                ballRb.linearVelocity=Vector3.zero;
                ballRb.angularVelocity=Vector3.zero;

                // 1. ドローンの現在の速度ベクトル（XYZの合力）を取得
                Vector3 droneVelocityVector = droneRb.linearVelocity;

                //Debug.Log($"ドローンの衝突前のスピード:{droneVelocityVector}");
                // 2. ドローンの速度ベクトルを tossBoost 倍にする
                Vector3 boostedVelocity = droneVelocityVector * tossBoost;

                // 3. 最低限の跳ね上がり（上方向への保障）を追加
                // ドローンが止まっていても、ボールが当たれば少し上に跳ねるようにする
                

                // 4. ボールの速度を完全に上書き
                droneRb.linearVelocity=Vector3.zero;
                ballRb.linearVelocity = boostedVelocity;

                //Debug.Log($"トス成功! 合力速度: {boostedVelocity} (倍率: {tossBoost})");
            }
        }
    }
}