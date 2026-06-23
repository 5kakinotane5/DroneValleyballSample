// サーブ権を確認してEnterキーでボールを手動発射するランチャー
/*
【AllyEnemyballlauncher】
概要:
  MatchManager ベースの手動サーブ発射器。
  MatchManager でサーブ権が自チームかつ Waiting フェーズのとき、
  Enter キーで自コート内のランダム地点にボールを発射する。
  発射後に MatchManager.StartPlay() を呼んでフェーズを Receiving に変える。

他スクリプトとの関係:
  ・MatchManager  ← serveRight / currentPhase を参照して発動判断、発射後 StartPlay() を呼ぶ

注意:
  ServeDrone（自動サーブ）と役割が重複する。ServeDrone はコート外に移動してから
  発射する高機能版のため、自動化する場合は ServeDrone を優先。
  手動テスト用途のみであればこちらを残し、ServeDrone を外すこと。
  RandomBallLauncher（VolleyballManager 系の旧ランチャー）とも目的が類似している。
*/
using UnityEngine;
public class AllyEnemyballlauncher : MonoBehaviour
{
    [Header("発射するボールのプレハブ")]
    public GameObject ballPrefab;

    [Header("着弾までの時間（秒）")]
    public float flightTime = 3f;

    [Header("サーブするチーム")]
    public Team serveTeam = Team.Ally;

    [Header("自動サーブ遅延（秒）")]
    public float autoServeDelay = 2.0f;

    private float serveTimer = 0f;

    void Update()
    {
        bool isMyServe = MatchManager.Instance != null &&
            MatchManager.Instance.serveRight == serveTeam &&
            MatchManager.Instance.currentPhase == MatchManager.GamePhase.Waiting;

        if (isMyServe)
        {
            serveTimer += Time.deltaTime;
            if (serveTimer >= autoServeDelay)
            {
                serveTimer = 0f;
                ShootBall();
            }
        }
        else
        {
            serveTimer = 0f;
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
        if(MatchManager.Instance != null){
            MatchManager.Instance.StartPlay();
        }
        
    }
}