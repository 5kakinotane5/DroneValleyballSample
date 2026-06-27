// ドローン衝突時にボール速度をベクトル倍増するトス処理
/*
【BallToss2】
概要:
  ドローンがボールに衝突したとき、ドローンの速度ベクトルを tossBoost 倍にして
  ボールに適用するトス処理スクリプト（BallToss の改良版）。
  ボールとドローン両方の速度を一度ゼロにしてから上書きするため、
  物理的なはね返りの計算を完全に制御できる。

他スクリプトとの関係:
  ・AdvancedDroneSpiker, DroneSpikeMultiple（旧スパイカー）
    ← 旧系統のスパイカーがボールに衝突する際の速度変換として利用
  ・SpikerAllyEnemyV2, SpikerAllyEnemy（現行スパイカー）
    ← これら現行スパイカーは OnCollisionEnter 内で ballRb.linearVelocity を
       自前で直接上書きするため、BallToss2 は不要（アタッチ不要）

【注意 ─ 削除候補】
  現行の SpikerAllyEnemyV2 はボール速度を自己完結して設定するため、
  BallToss2 を同じドローンにアタッチすると速度が二重に適用される。
  旧系統スクリプトを整理した後は削除可能。
  BallToss（旧版）と完全に機能が重複するため、BallToss 側は削除推奨。
*/
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
                BallInfo.Register(ballRb); // 確実な実体を登録

                //一度完全停止
                BallInfo.SetVelocity(Vector3.zero);
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
                BallInfo.SetVelocity(boostedVelocity);

                //Debug.Log($"トス成功! 合力速度: {boostedVelocity} (倍率: {tossBoost})");
            }
        }
    }
}