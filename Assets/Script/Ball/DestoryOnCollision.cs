// 指定タグへの衝突でボールを即削除（ServeDroneのサーブ弾用）
/*
【DestoryOnCollision】（クラス名にスペルミスあり: Destroy の誤り）
概要:
  指定タグのオブジェクトに衝突したとき自分自身を削除するスクリプト。
  衝突地点の座標をデバッグログに出力する。

他スクリプトとの関係:
  ・ServeDrone が生成するサーブボールの Prefab にコンポーネントとしてアタッチされており、
    コート（Court タグ等）に着弾したタイミングでボールを消去する役割を担う。
  ・BallDestruction と役割が重複しているため、同一 Prefab に両方をアタッチしないこと。

注意:
  BallResetOnCollision も衝突時にボールを削除するが、あちらは得点判定・リセット処理まで
  行う高機能版。サーブボールに得点判定が不要な場合はこのスクリプトを使用する。
  クラス名のスペルミスは、他スクリプトから型名で参照していない限り動作に影響しない。
*/
using UnityEngine;

public class DestroyOnCollision : MonoBehaviour
{
    [Header("このタグを持つ物体とぶつかると消去する")]
    public string targetTag = "Ball";

    [Header("自分を消す場合はチェックを入れる")]
    public bool destroySelf = false;

    // 衝突した瞬間に呼ばれる
    private void OnCollisionEnter(Collision collision)
    {
        // ぶつかった相手のタグをチェック
        if (collision.gameObject.CompareTag(targetTag))
        {
            Vector3 hitPoint=collision.contacts[0].point;
            Debug.Log($"{targetTag} にぶつかったので自分を消去しました。消失地点: {hitPoint}");
            Destroy(gameObject);
            //Debug.Log($"{targetTag}にぶつかったので自分を消去しました。");
            
        }
    }
}