/*
スパイクを打つ高さは簡易的にするために固定する。
取り敢えずは高さh=6にする。
打つ球の速度vd(vxd,vyd,vzd)と位置情報(xd,yd,zd)を取得する
打撃点を２点調べる(球が上がる時と下がる時)
打撃地点を(x,y,z),時刻をthとする。
y-zd=vyd*th+0.5f*g*th^2
->thを求める
狙う位置をきめる(xa,ya,za)

*/
using UnityEngine;

public class AiDroneSpiker : MonoBehaviour
{
    [Header("基本設定")]
    public string ballTag = "injectionball";
    public float vMax = 15f;           // 最大速度
    public float spikeHeight = 6.0f;    // 叩く高さ(h)
    public float netZ = 0f;            // ネットのZ座標
    public bool isTeamA = true;        // チーム判定（コート制限用）

    [Header("スパイク設定")]
    public float spikeForce = 20f;     // 叩きつける力
    public float interceptTolerance = 0.5f; // どのくらい近づいたら打つか

    private Rigidbody rb;
    private Rigidbody targetRb;
    private Vector3 targetSpikePos;    // 相手コートの狙い目
    private bool isBallInMyCourt = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 暫定的な狙い目（本来は相手の位置から動的に計算）
        targetSpikePos = isTeamA ? new Vector3(0, 0, 5) : new Vector3(0, 0, -5);
    }

    void FixedUpdate()
    {
        GameObject ball = GameObject.FindGameObjectWithTag(ballTag);
        if (ball == null) { Hover(); return; }

        targetRb = ball.GetComponent<Rigidbody>();
        
        // 1. ボールが自分のコートにあるかチェック
        CheckBallCourt();

        if (isBallInMyCourt)
        {
            ExecuteSpikeLogic();
        }
        else
        {
            Hover(); // 相手陣地にあるときは待機
        }
    }

    void CheckBallCourt()
    {
        // チームAはZマイナス側、チームBはZプラス側と仮定
        float ballZ = targetRb.position.z;
        isBallInMyCourt = isTeamA ? (ballZ < netZ) : (ballZ > netZ);
    }

    void ExecuteSpikeLogic()
    {
        // 2. 打撃点(th)の計算：二次方程式 0.5gt^2 + Vy*t + (y0 - h) = 0
        float g = Physics.gravity.y;
        float v_yd = targetRb.linearVelocity.y;
        float y0 = targetRb.position.y;

        float a = 0.5f * g;
        float b = v_yd;
        float c = y0 - spikeHeight;

        float det = b * b - 4 * a * c;

        if (det < 0) { Hover(); return; } // 指定の高さまで上がらない

        // 2つの解（上昇時と下降時）
        float t1 = (-b + Mathf.Sqrt(det)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(det)) / (2 * a);

        // 未来の時刻（正の数）で、より適切な方を選択
        float th = (t1 > 0) ? t1 : t2;
        if (th < 0) { Hover(); return; }

        // 3. 打撃地点の座標予測
        Vector3 ballPos = targetRb.position;
        Vector3 ballVel = targetRb.linearVelocity;
        // XとZは等速直線運動と仮定
        Vector3 interceptPoint = new Vector3(
            ballPos.x + ballVel.x * th,
            spikeHeight,
            ballPos.z + ballVel.z * th
        );

        // 4. 移動と対数加速度 a = log(vmax - v)
        Vector3 diff = interceptPoint - transform.position;
        Vector3 dir = diff.normalized;
        
        float currentV = rb.linearVelocity.magnitude;
        // 速度制限ロジック: vmaxに近づくほど加速を絞る
        float speedDiff = Mathf.Max(0.1f, vMax - currentV);
        float logFactor = Mathf.Log10(speedDiff + 1.0f); 

        // 基本加速度（距離に比例）にlog係数をかける
        Vector3 accel = dir * logFactor * 10f; 
        
        rb.AddForce(accel, ForceMode.Acceleration);

        // 5. スパイク実行
        if (diff.magnitude < interceptTolerance)
        {
            PerformSpike();
        }

        Debug.DrawLine(transform.position, interceptPoint, Color.yellow);
    }

    void PerformSpike()
    {
        // 目標地点に向かう速度ベクトルを計算してボールに上書き
        Vector3 spikeDir = (targetSpikePos - targetRb.position).normalized;
        // 少し下向き成分を強調
        spikeDir.y = -1.0f;
        
        targetRb.linearVelocity = Vector3.zero; // 一旦リセット
        targetRb.AddForce(spikeDir.normalized * spikeForce, ForceMode.Impulse);
        
        Debug.Log("Spike!");
    }

    void Hover()
    {
        // 重力相殺 + ブレーキ
        Vector3 hoverAccel = -Physics.gravity - rb.linearVelocity * 0.5f;
        rb.AddForce(hoverAccel, ForceMode.Acceleration);
    }
}