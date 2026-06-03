/*
【BallDestruction】
概要:
  ボールの自動削除を担当する汎用スクリプト。
  ①一定時間（lifeTime）経過後、②指定高度（deathYThreshold）以下に落ちたとき、
  ③特定タグのオブジェクトに衝突したとき（destroyOnCollision=true の場合）
  の 3 条件でボールを削除する。

他スクリプトとの関係:
  ・ServeDrone が生成するサーブボールの Prefab にコンポーネントとしてアタッチされており、
    タイムアウト・場外落下・コート着弾のいずれかでボールを安全に消去する役割を担う。
  ・DestoryOnCollision と役割が重複するため、同一 Prefab に両方をアタッチしないこと。

注意:
  BallResetOnCollision も衝突時にボールを削除するが、あちらは得点判定・リセット処理まで
  行う高機能版。サーブボールに得点判定が不要な場合は destroyOnCollision=true にして
  このスクリプトを使用する。BallResetOnCollision と同じ Prefab に共存させる場合は
  destroyOnCollision=false に設定し、時間・高度による削除のみ有効にすること。
*/
using UnityEngine;

public class BallDestruction : MonoBehaviour
{
    [Header("消去設定")]
    [Tooltip("射出されてから自動で消えるまでの時間")]
    public float lifeTime = 120f;

    [Tooltip("この高さより下に落ちたら即座に消去する")]
    public float deathYThreshold = -2f;

    [Tooltip("何かに衝突した瞬間に消す場合はTrue（スパイク成功時など）")]
    public bool destroyOnCollision = false;

    [Tooltip("特定のタグ（例: Court）に触れたら消す場合。空なら何に触れても判定しない")]
    public string targetTag = "Court";

    void Start()
    {
        // 1. 指定時間後に自動削除を予約
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 2. 奈落判定：高さがしきい値を下回ったら即削除
        if (transform.position.y < deathYThreshold)
        {
            //Debug.Log($"{gameObject.name} が場外に落ちたため削除されました。");
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 3. 衝突判定（床やネットに触れたら消す設定用）
        if (destroyOnCollision)
        {
      // ターゲットタグの指定がない、もしくはタグが一致する場合
            if (string.IsNullOrEmpty(targetTag) || collision.gameObject.CompareTag(targetTag))
            {
                Debug.Log($"{gameObject.name} が {collision.gameObject.name} に接触したため削除されました。");
                Destroy(gameObject);
            }
        }
    }
}