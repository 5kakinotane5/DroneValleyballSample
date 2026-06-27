// ボール弾道予測と回避機能付きAIスパイカー（Ally/Enemy対応）
/*
【SpikerAllyEnemyV2】
概要:
  現行システムの主力スパイカー（最新版）。
  MatchManager のフェーズ・チームを監視し、スパイクフェーズになると
  ボールの弾道を予測してスパイクを実行する。
  SpikerAllyEnemy（V1）に加え、ボール軌道回避と非ターゲットボール回避機能を備える。
  Ally/Enemy 両チームに対応（myTeam で切り替え）。

動作フロー: Waiting → Hovering → MovingToTrajectory → Striking → Returning

他スクリプトとの関係:
  ・MatchManager          ← フェーズ/チームを参照・更新
  ・BallResetOnCollision  ← ラリー終了時に ResetToInitialState() を呼ばれる
  ・ServeDrone            ← Start() 時にこのスクリプトの initialPos と myTeam を参照する
  ・BallToss2             ← ボールに衝突したとき、ballRb.linearVelocity を直接上書きするため
                            BallToss2 は不要（OnCollisionEnter 内でボール速度を設定）

注意:
  SpikerAllyEnemy（V1）より高機能なため、通常はこちらを使用する。
*/
using UnityEngine;
using Random = UnityEngine.Random;

public class Spike : MonoBehaviour
{
    [SerializeField] private Team myTeam;
    public Team MyTeam => myTeam;

    [Header("ドローンの速度の何倍で飛ばすか")]
    public float tossBoost = 2f;

    public string ballTag = "injectionball";
    public float spikeHeight = 10f;
    public Vector3 initialPos = new Vector3(10.5f, 6.0f, 0f);
    public float vMaxDrone = 40f;
    public float vMax => vMaxDrone * tossBoost;

    [Header("弾道パラメータ")]
    [SerializeField] private float spikeFlightTime = 0.6f;
    [SerializeField] private float runupTime = 0.2f;

    [Header("ネット安全設定")]
    public float netX = 0f;
    public float netHeightSafe = 4.9f;

    [Header("ターゲットボール軌道回避設定")]
    [SerializeField] private float trajectoryCheckRadius = 3f;  // この距離以内で軌道回避を開始
    [SerializeField] private float trajectoryAvoidSpeed = 25f;  // 回避速度
    [SerializeField] private int trajectorySamples = 30;        // 軌道のサンプリング数

    [Header("非ターゲットボール回避設定")]
    [SerializeField] private float dodgeRadius = 3f;
    [SerializeField] private float dodgeSpeed = 15f;

    // デバッグ用（Inspector で確認可能）
    [SerializeField] private bool isAvoidingTrajectory = false;

    private Rigidbody rb;
    private Rigidbody targetRb;
    private GameObject targetBall;
    private Vector3 requiredDroneVel;
    private Vector3 pointA;
    private Vector3 standbyPoint;
    private float timeUntilImpact;
    private GameObject lastSpikedBall;
    private float g = BallInfo.Gravity;

    enum State { Waiting, Hovering, MovingToTrajectory, Striking, Returning }
    [SerializeField] private State currentState = State.Waiting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        transform.position = initialPos;
    }
    void FixedUpdate()
    {
        if (currentState == State.MovingToTrajectory || currentState == State.Striking)
            timeUntilImpact -= Time.fixedDeltaTime;

        // Striking/Waiting中は非ターゲットとの物理衝突を無効化
        bool shouldIgnorePhysics = (currentState == State.Striking || currentState == State.Waiting);
        SetNonTargetBallIgnore(shouldIgnorePhysics);

        switch (currentState)
        {
            case State.Waiting:
                rb.linearVelocity = Vector3.zero;
                if (MatchManager.Instance.currentPhase == MatchManager.GamePhase.Spiking &&
                    MatchManager.Instance.currentPossesion == myTeam)
                {
                    currentState = State.Hovering;
                }
                break;

            case State.Hovering:
                // ホバリング中もターゲットボールの軌道を監視し、被る場合は回避しながらホバー
                isAvoidingTrajectory = TryGetTrajectoryAvoidVector(out Vector3 hoverAvoid);
                Hover(initialPos);
                if (isAvoidingTrajectory)
                    rb.linearVelocity += hoverAvoid;
                ApplyDodgeVelocity();
                FindAndCalculateBall();
                break;

            case State.MovingToTrajectory:
                if (targetBall == null)
                {
                    currentState = State.Returning;
                    break;
                }
                // standbyPointへ移動しつつ、軌道上に被った場合は回避ベクトルを加算
                isAvoidingTrajectory = TryGetTrajectoryAvoidVector(out Vector3 moveAvoid);
                MoveToPoint(standbyPoint);
                if (isAvoidingTrajectory)
                {
                    rb.linearVelocity += moveAvoid;
                    if (rb.linearVelocity.magnitude > vMax)
                        rb.linearVelocity = rb.linearVelocity.normalized * vMax;
                }
                ApplyDodgeVelocity();
                if (timeUntilImpact <= runupTime)
                    currentState = State.Striking;
                break;

            case State.Striking:
                // 精密な軌道を維持するため毎フレーム強制上書き（回避は行わない）
                isAvoidingTrajectory = false;
                rb.linearVelocity = requiredDroneVel;
                if (myTeam == Team.Ally)
                    MatchManager.Instance.ChangePossesion(Team.Enemy);
                else
                    MatchManager.Instance.ChangePossesion(Team.Ally);
                // 打ち損ねたときのタイムアウト
                if (timeUntilImpact < -spikeFlightTime)
                    currentState = State.Returning;
                break;

            case State.Returning:
                isAvoidingTrajectory = false;
                Hover(initialPos);
                ApplyDodgeVelocity();
                if (Vector3.Distance(transform.position, initialPos) < 0.3f)
                {
                    lastSpikedBall = null;
                    targetBall = null;
                    targetRb = null;
                    timeUntilImpact = 0;
                    currentState = State.Waiting;
                }
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != State.Striking) return;
        // ターゲットにした特定のボールのみに反応
        if (targetBall == null || collision.gameObject != targetBall) return;
        if (!collision.gameObject.CompareTag(ballTag)) return;

        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb == null) return;
        BallInfo.Register(ballRb); // 確実な実体を登録

        if (MatchManager.Instance != null)
            MatchManager.Instance.lastTeamToHit = myTeam;

        BallInfo.SetVelocity(requiredDroneVel * tossBoost);
        rb.linearVelocity = Vector3.zero;

        lastSpikedBall = collision.gameObject;
        currentState = State.Returning;
    }

    // ターゲットボールの放物線軌道をサンプリングし、
    // ドローンと最接近する点が trajectoryCheckRadius 以内なら回避ベクトルを返す
    bool TryGetTrajectoryAvoidVector(out Vector3 avoidVector)
    {
        avoidVector = Vector3.zero;

        // targetRb がない場合（Hovering直後など）はシーン内ボールで代替
        Rigidbody checkRb = targetRb;
        if (checkRb == null)
        {
            GameObject anyBall = GameObject.FindGameObjectWithTag(ballTag);
            if (anyBall == null || anyBall == lastSpikedBall) return false;
            if (!IsBallOnMySide(anyBall.transform.position)) return false;
            checkRb = anyBall.GetComponent<Rigidbody>();
            if (checkRb == null) return false;
        }

        // Hovering中（timeUntilImpact≒0）は3秒先まで確認、それ以外は衝突予測時刻まで
        float duration = (timeUntilImpact > 0.05f) ? timeUntilImpact : 3f;

        float minDist = float.MaxValue;
        Vector3 closestBallPos = Vector3.zero;
        float closestT = 0f;

        for (int i = 0; i <= trajectorySamples; i++)
        {
            float t = duration * i / trajectorySamples;
            Vector3 bp = PredictBallPosition(checkRb, t);
            float d = Vector3.Distance(transform.position, bp);
            if (d < minDist)
            {
                minDist = d;
                closestBallPos = bp;
                closestT = t;
            }
        }

        if (minDist >= trajectoryCheckRadius) return false;

        // ドローンから最接近点への逆方向（軌道から離れる方向）
        Vector3 awayDir = transform.position - closestBallPos;

        if (awayDir.magnitude < 0.01f)
        {
            // ドローンが軌道上に乗っている：ボール速度に垂直な方向に回避
            Vector3 ballVelAtT = new Vector3(
                checkRb.linearVelocity.x,
                checkRb.linearVelocity.y + g * closestT,
                checkRb.linearVelocity.z
            );
            awayDir = Vector3.Cross(ballVelAtT.normalized, Vector3.up);
            if (awayDir.magnitude < 0.01f)
                awayDir = Vector3.forward;
        }

        // 近いほど強く回避（距離0でfull、trajectoryCheckRadiusで0）
        float strength = 1f - (minDist / trajectoryCheckRadius);
        avoidVector = awayDir.normalized * strength * trajectoryAvoidSpeed;
        return true;
    }

    // 時刻 t 秒後のボール位置を予測（重力考慮）
    Vector3 PredictBallPosition(Rigidbody ballRb, float t)
    {
        return new Vector3(
            ballRb.position.x + ballRb.linearVelocity.x * t,
            ballRb.position.y + ballRb.linearVelocity.y * t + 0.5f * g * t * t,
            ballRb.position.z + ballRb.linearVelocity.z * t
        );
    }

    // 非ターゲットボールとの物理衝突を有効/無効化
    void SetNonTargetBallIgnore(bool ignore)
    {
        Collider myCol = GetComponent<Collider>();
        if (myCol == null) return;
        GameObject[] balls = GameObject.FindGameObjectsWithTag(ballTag);
        foreach (var ball in balls)
        {
            if (ball == targetBall) continue;
            Collider bc = ball.GetComponent<Collider>();
            if (bc != null)
                Physics.IgnoreCollision(myCol, bc, ignore);
        }
    }

    // dodgeRadius内の非ターゲットボールを速度で回避
    void ApplyDodgeVelocity()
    {
        Vector3 dodge = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(transform.position, dodgeRadius);
        foreach (var col in nearby)
        {
            if (!col.CompareTag(ballTag)) continue;
            if (col.gameObject == targetBall) continue;

            Vector3 awayDir = transform.position - col.transform.position;
            float dist = awayDir.magnitude;
            if (dist < 0.001f) continue;

            float weight = 1f - Mathf.Clamp01(dist / dodgeRadius);
            dodge += awayDir.normalized * weight * dodgeSpeed;
        }

        if (dodge.sqrMagnitude > 0.001f)
        {
            rb.linearVelocity += dodge;
            if (rb.linearVelocity.magnitude > vMaxDrone)
                rb.linearVelocity = rb.linearVelocity.normalized * vMaxDrone;
        }
    }

    void FindAndCalculateBall()
    {
        if (currentState != State.Waiting && currentState != State.Hovering) return;

        GameObject ball = GameObject.FindGameObjectWithTag(ballTag);
        if (ball == null || ball == lastSpikedBall) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;
        BallInfo.Register(ballRb); // 追跡対象としてメインボールを登録

        if (!IsBallOnMySide(BallInfo.Position)) return;

        if (BallInfo.Velocity.y > 0 &&
            BallInfo.Position.y < spikeHeight &&
            MatchManager.Instance.currentPhase == MatchManager.GamePhase.Spiking)
        {
            targetRb = ballRb;
            targetBall = ball;
            if (CalculateTrajectory())
            {
                currentState = State.MovingToTrajectory;
            }
            else
            {
                targetRb = null;
                targetBall = null;
            }
        }
    }

    bool IsBallOnMySide(Vector3 ballPos)
    {
        if (myTeam == Team.Ally)
            return ballPos.x > netX;
        else
            return ballPos.x < netX;
    }

    bool CalculateTrajectory()
    {
        float t = CalculateFalling(spikeHeight);
        if (t == -1) return false;
        timeUntilImpact = t;

        Vector3 pointB;
        if (MatchManager.Instance.currentPossesion == Team.Ally)
            pointB = new Vector3(Random.Range(-21f, -10.5f), 0f, Random.Range(-10f, 10f));
        else
            pointB = new Vector3(Random.Range(21f, 10.5f), 0f, Random.Range(-10f, 10f));

        Vector3 ballPos = BallInfo.Position;
        Vector3 ballVel = BallInfo.Velocity;
        pointA = new Vector3(
            ballPos.x + (ballVel.x * t),
            spikeHeight,
            ballPos.z + (ballVel.z * t)
        );

        float BAx = pointB.x - pointA.x;
        float BAz = pointB.z - pointA.z;

        float vBallX = BAx / spikeFlightTime;
        float vBallZ = BAz / spikeFlightTime;
        float vBallY = (pointB.y - pointA.y - 0.5f * g * spikeFlightTime * spikeFlightTime) / spikeFlightTime;
        Vector3 vBallPost = new Vector3(vBallX, vBallY, vBallZ);

        Debug.Log($"pointB(狙う位置):{pointB}");

        if (vBallPost.magnitude > vMax)
        {
            float a = 0.25f * g * g;
            float b = g * spikeHeight - vMax * vMax;
            float c = spikeHeight * spikeHeight + BAx * BAx + BAz * BAz;
            float det = b * b - 4f * a * c;
            if (det < 0f)
            {
                Debug.Log("det<0");
                return false;
            }
            float t_rising = (-b + Mathf.Sqrt(det)) / (2f * a);
            float t_falling = (-b - Mathf.Sqrt(det)) / (2f * a);
            float tb = Mathf.Sqrt(Mathf.Max(t_rising, t_falling));

            vBallX = BAx / tb;
            vBallZ = BAz / tb;
            vBallY = (pointA.y - pointB.y + 0.5f * g * tb * tb) / tb;
            vBallPost = new Vector3(vBallX, vBallY, vBallZ);
        }

        // ネットクリアランス保証（閉形式）
        // yNet = pointA.y + alpha*(pointB.y-pointA.y) + 0.5*g*tb^2*alpha*(alpha-1)
        // alpha = (netX-pointA.x)/BAx (ネットが全行程のどの割合の位置か)
        if (Mathf.Abs(vBallX) > 0.001f)
        {
            float alpha = (netX - pointA.x) / BAx;
            if (alpha > 0f && alpha < 1f)
            {
                float currentTb = BAx / vBallX;
                float tNetCheck = alpha * currentTb;
                float yNet = pointA.y + vBallY * tNetCheck + 0.5f * g * tNetCheck * tNetCheck;
                float neededY = netHeightSafe + 0.5f; // 50cm余裕

                if (yNet < neededY)
                {
                    // neededY を満たす最小 tb を求める
                    float linY = pointA.y + alpha * (pointB.y - pointA.y);
                    float curveFactor = 0.5f * g * alpha * (alpha - 1f); // g<0, alpha*(alpha-1)<0 → 正の値
                    if (curveFactor > 0.0001f)
                    {
                        float tb2Min = (neededY - linY) / curveFactor;
                        if (tb2Min > currentTb * currentTb)
                        {
                            float tbNew = Mathf.Sqrt(tb2Min);
                            vBallX = BAx / tbNew;
                            vBallZ = BAz / tbNew;
                            vBallY = (pointB.y - pointA.y - 0.5f * g * tbNew * tbNew) / tbNew;
                            vBallPost = new Vector3(vBallX, vBallY, vBallZ);
                        }
                    }
                }
            }
        }

        requiredDroneVel = vBallPost / tossBoost;

        float actualRunup = Mathf.Min(runupTime, t);
        standbyPoint = pointA - (requiredDroneVel * actualRunup);
        return true;
    }

    float CalculateFalling(float h)
    {
        float y0 = BallInfo.Position.y;
        float vy0 = BallInfo.Velocity.y;

        float a = 0.5f * g;
        float b = vy0;
        float c = y0 - h;
        float det = b * b - 4 * a * c;

        if (det < 0) return -1;

        float t_rising = (-b + Mathf.Sqrt(det)) / (2 * a);
        float t_falling = (-b - Mathf.Sqrt(det)) / (2 * a);
        float tb = Mathf.Max(t_rising, t_falling);

        if (tb < 0) return -1;
        return tb;
    }

    void MoveToPoint(Vector3 target)
    {
        Vector3 diff = target - transform.position;
        rb.linearVelocity = diff / 0.8f;
        if (rb.linearVelocity.magnitude > vMax)
            rb.linearVelocity = rb.linearVelocity.normalized * vMax;
    }

    void Hover(Vector3 target)
    {
        Vector3 diff = target - transform.position;
        if (diff.magnitude < 0.3f)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = target;
            return;
        }
        rb.linearVelocity = diff.normalized * vMaxDrone / 8f;
    }

    public void ResetToInitialState()
    {
        currentState = State.Waiting;
        targetRb = null;
        targetBall = null;
        lastSpikedBall = null;
        SetNonTargetBallIgnore(false);
        transform.position = initialPos;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }
}
