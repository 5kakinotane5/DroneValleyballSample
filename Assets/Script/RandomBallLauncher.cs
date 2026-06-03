/*
【RandomBallLauncher】
概要:
  VolleyballManager ベースのボール発射器。Enter キーで自コート側の
  ランダム地点にボールを発射し、VolleyballManager.StartPlay() を呼ぶ。
  チーム・サーブ権の概念なし。

他スクリプトとの関係:
  ・VolleyballManager  ← 発射後に StartPlay() を呼んでフェーズを Receiving へ変える

【注意 ─ 削除候補】
  AllyEnemyballlauncher（MatchManager 系・サーブ権考慮）が現行の上位互換版。
  VolleyballManager 系移行後は削除推奨。
*/
using UnityEngine;
using UnityEngine.InputSystem;
public class RandomBallLauncher : MonoBehaviour
{
    [Header("発射するボールのプレハブ")]
    public GameObject ballPrefab;

    [Header("着弾までの時間（秒）")]
    public float flightTime = 3f;

    void Update()
    {
        // エンターキーで発射！
        if (Keyboard.current!=null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ShootBall();
            /*
            if (ValleyballManager.Instance != null){
            ValleyballManager.Instance.StartPlay}
            */
        }
    }

    void ShootBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("Ball Prefabがセットされていません！");
            return;
        }

        // 1. ボールを生成
        GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        // 2. 自分のコートのランダムな目標地点を決める
        // ※ここの数字を自分のコートの座標に合わせて微調整してください
        float randomX = Random.Range(1f, 21f);
        float randomZ = Random.Range(-10f, 10f);
        Vector3 targetPoint = new Vector3(randomX, 0f, randomZ);
        //Vector3 targetPoint=new Vector3(18.27f,0f,-5.7f);//この場合ドローンはスパイクできない
        //Debug.Log($"予想落下地点：{targetPoint}");
        // 3. 必要な初速を物理計算で出す
        Vector3 startPoint = transform.position;
        float vx = (targetPoint.x - startPoint.x) / flightTime;
        float vz = (targetPoint.z - startPoint.z) / flightTime;
        float gravity = Physics.gravity.y;
        float vy = (targetPoint.y - startPoint.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

        // 4. 速度をセット
        ballRb.linearVelocity = new Vector3(vx, vy, vz);
        
        //発射と同時にマネージャーのフェーズを変える
        if(VolleyballManager.Instance != null){
            VolleyballManager.Instance.StartPlay();
        }
        
    }
}