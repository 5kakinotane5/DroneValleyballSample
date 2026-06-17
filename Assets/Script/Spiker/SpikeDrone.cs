// スパイクモジュール：SpikerAllyEnemyV2 の動作をそのまま継承し公開 API を追加
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>レシーバーのトス品質（3段階）</summary>
public enum TossQuality { High, Medium, Low }

/// <summary>
/// SpikerAllyEnemyV2 の座標・速度制御をそのまま使用するスパイク専用ドローン。
///
/// SpikerAllyEnemyV2 からの変更点（最小限）:
///   1. isReady プロパティ追加（Hovering 状態のとき true）
///   2. inputCourse / inputVelocity をコントローラーから毎フレーム受け取る
///   3. FindAndCalculateBall で pendingCourse/pendingVelocity を設定
///   4. CalculateTrajectory の pointB を API 値から決定（ランダムから変更）
///   5. OnCollisionEnter でボール速度を CalcBallVelocity() で付与
/// </summary>
public class SpikeDrone : MonoBehaviour
{
    // ── SpikerAllyEnemyV2 と同一のフィールド ──────────────────────────
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
    [SerializeField] private float trajectoryCheckRadius = 3f;
    [SerializeField] private float trajectoryAvoidSpeed = 25f;
    [SerializeField] private int trajectorySamples = 30;

    [Header("非ターゲットボール回避設定")]
    [SerializeField] private float dodgeRadius = 3f;
    [SerializeField] private float dodgeSpeed = 15f;

    [SerializeField] private bool isAvoidingTrajectory = false;

    // ── スタミナ ────────────────────────────────────────────────────
    [Header("スタミナ")]
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private TimingWindowSystem timingWindow;

    /// <summary>外部からスタミナを参照する（SpikeController / ScoreManager 等）</summary>
    public StaminaSystem Stamina => staminaSystem;
    /// <summary>外部からタイミングウィンドウを参照する（SpikeController）</summary>
    public TimingWindowSystem TimingWindow => timingWindow;


    // ── 公開 API 用追加フィールド ──────────────────────────────────────
    [Header("操作・速度設定（API 用）")]
    [SerializeField] private bool isPlayerControlled = true;

    [Tooltip("入力がない場合の最低打球速度")]
    public float minVelocity = 5f;

    [Range(0.1f, 1.0f)]
    [Tooltip("低トス時の速度上限（vMaxDrone に対する割合）")]
    public float weakVelocityRatio = 0.35f;

    [Range(0.1f, 1.0f)]
    [Tooltip("中トス時の速度上限（vMaxDrone に対する割合）")]
    public float medVelocityRatio = 0.65f;

    [Header("トス判定")]
    public float highTossApexThreshold = 13f;
    public float medTossApexThreshold = 8f;

    [Header("相手コート着弾範囲")]
    [Tooltip("Ally は負値・Enemy は正値で設定")]
    public float targetShallowX = -3f;
    public float targetDeepX = -20f;
    public float targetZHalf = 9f;

    // ── 公開 API プロパティ ────────────────────────────────────────────

    /// <summary>Hovering 状態（スパイク待機中）のときのみ true</summary>
    public bool isReady { get; private set; }

    /// <summary>トス品質 × スタミナ倍率の速度上限（コントローラーのチャージ上限に使用）</summary>
    public float CurrentMaxVelocity
    {
        get
        {
            float tossBase;
            switch (tossQuality)
            {
                case TossQuality.High: tossBase = vMaxDrone; break;
                case TossQuality.Medium: tossBase = vMaxDrone * medVelocityRatio; break;
                default: tossBase = vMaxDrone * weakVelocityRatio; break;
            }
            float staminaMult = staminaSystem != null ? staminaSystem.SpeedMultiplier : 1f;
            return tossBase * staminaMult;
        }
    }

    /// <summary>現在のトス品質（コントローラー表示用）</summary>
    public TossQuality CurrentTossQuality => tossQuality;

    /// <summary>後方互換プロパティ（High のときのみ true）</summary>
    public bool IsHighToss => tossQuality == TossQuality.High;

    /// <summary>コントローラーが毎フレーム設定するコース値（-1〜1）</summary>
    public float inputCourse = 0f;

    /// <summary>コントローラーが毎フレーム設定する速度値（0 のとき minVelocity を使用）</summary>
    public float inputVelocity = 0f;

    // ── 内部状態（SpikerAllyEnemyV2 と同一） ─────────────────────────
    private Rigidbody rb;
    private Rigidbody targetRb;
    private GameObject targetBall;
    private Vector3 requiredDroneVel;
    private Vector3 pointA;
    private Vector3 standbyPoint;
    private float timeUntilImpact;
    private GameObject lastSpikedBall;
    private float g = Physics.gravity.y;

    // API 用追加
    private float pendingCourse;
    private float pendingVelocity;
    private TossQuality tossQuality = TossQuality.Low;

    enum State { Waiting, Hovering, MovingToTrajectory, Striking, Returning }
    [SerializeField] private State currentState = State.Waiting;

    // ── Unity（SpikerAllyEnemyV2 と同一） ────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        transform.position = initialPos;
    }

    void FixedUpdate()
    {
        staminaSystem?.RecoverTick(currentState == State.Waiting);

        if (currentState == State.MovingToTrajectory || currentState == State.Striking)
            timeUntilImpact -= Time.fixedDeltaTime;

        bool shouldIgnorePhysics = (currentState == State.Striking || currentState == State.Waiting);
        SetNonTargetBallIgnore(shouldIgnorePhysics);

        switch (currentState)
        {
            case State.Waiting:
                isReady = false;
                rb.linearVelocity = Vector3.zero;
                if (MatchManager.Instance.currentPhase == MatchManager.GamePhase.Spiking &&
                    MatchManager.Instance.currentPossesion == myTeam)
                {
                    currentState = State.Hovering;
                }
                break;

            case State.Hovering:
                isReady = true; // ← Hovering のときのみ true
                DetectTossType();
                isAvoidingTrajectory = TryGetTrajectoryAvoidVector(out Vector3 hoverAvoid);
                Hover(initialPos);
                if (isAvoidingTrajectory)
                    rb.linearVelocity += hoverAvoid;
                ApplyDodgeVelocity();
                FindAndCalculateBall();
                break;

            case State.MovingToTrajectory:
                isReady = false;
                if (targetBall == null)
                {
                    currentState = State.Returning;
                    break;
                }
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
                isReady = false;
                isAvoidingTrajectory = false;
                rb.linearVelocity = requiredDroneVel;
                if (myTeam == Team.Ally)
                    MatchManager.Instance.ChangePossesion(Team.Enemy);
                else
                    MatchManager.Instance.ChangePossesion(Team.Ally);
                if (timeUntilImpact < -spikeFlightTime)
                    currentState = State.Returning;
                break;

            case State.Returning:
                isReady = false;
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

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 外部から明示的にコース・速度を指定する。
    /// 呼ばなくても drone は自動でスパイクする（inputCourse/inputVelocity を使用）。
    /// </summary>
    public void Spike(float course, float velocity)
    {
        inputCourse = Mathf.Clamp(course, -1f, 1f);
        inputVelocity = velocity;
    }

    public void ResetToInitialState()
    {
        currentState = State.Waiting;
        targetRb = null;
        targetBall = null;
        lastSpikedBall = null;
        isReady = false;
        SetNonTargetBallIgnore(false);
        transform.position = initialPos;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }

    // ── 内部処理（SpikerAllyEnemyV2 と同一） ─────────────────────────

    // FindAndCalculateBall：pendingCourse/pendingVelocity を設定してから軌道計算
    void FindAndCalculateBall()
    {
        if (currentState != State.Waiting && currentState != State.Hovering) return;

        GameObject ball = GameObject.FindGameObjectWithTag(ballTag);
        if (ball == null || ball == lastSpikedBall) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        if (!IsBallOnMySide(ball.transform.position)) return;

        if (ballRb.linearVelocity.y > 0 &&
            ballRb.position.y < spikeHeight &&
            MatchManager.Instance.currentPhase == MatchManager.GamePhase.Spiking)
        {
            // Ally: コントローラーの入力値を使用（入力なしは minVelocity）
            if (isPlayerControlled)
            {
                pendingCourse = inputCourse;
                pendingVelocity = Mathf.Max(inputVelocity, minVelocity);
                pendingVelocity = Mathf.Min(pendingVelocity, CurrentMaxVelocity);
            }
            // Enemy: ランダム（トス制限あり）
            else
            {
                pendingCourse = Random.value > 0.5f ? 1f : -1f;
                switch (tossQuality)
                {
                    case TossQuality.High:
                        pendingVelocity = Random.value > 0.5f ? vMaxDrone : vMaxDrone * medVelocityRatio;
                        break;
                    case TossQuality.Medium:
                        pendingVelocity = vMaxDrone * medVelocityRatio;
                        break;
                    default:
                        pendingVelocity = vMaxDrone * weakVelocityRatio;
                        break;
                }
            }

            targetRb = ballRb;
            targetBall = ball;
            if (CalculateTrajectory())
            {
                currentState = State.MovingToTrajectory;
                timingWindow?.StartWindow(tossQuality, timeUntilImpact);
            }
            else
            {
                targetRb = null;
                targetBall = null;
            }
        }
    }

    // CalculateTrajectory：SpikerAllyEnemyV2 と同一だが pointB を API 値から決定
    bool CalculateTrajectory()
    {
        float t = CalculateFalling(spikeHeight);
        if (t == -1) return false;
        timeUntilImpact = t;

        // ★ random → pendingCourse/pendingVelocity から pointB を決定
        float norm = Mathf.Clamp01(pendingVelocity / vMaxDrone);
        float targetX = (myTeam == Team.Ally)
            ? Mathf.Lerp(targetShallowX, targetDeepX, norm)
            : Mathf.Lerp(-targetShallowX, -targetDeepX, norm);
        float blur = staminaSystem != null ? staminaSystem.GetBlur() : 0f;
        Vector3 pointB = new Vector3(
            targetX + Random.Range(-blur, blur),
            0f,
            pendingCourse * targetZHalf + Random.Range(-blur, blur));

        pointA = new Vector3(
            targetRb.position.x + (targetRb.linearVelocity.x * t),
            spikeHeight,
            targetRb.position.z + (targetRb.linearVelocity.z * t)
        );

        float BAx = pointB.x - pointA.x;
        float BAz = pointB.z - pointA.z;

        float vBallX = BAx / spikeFlightTime;
        float vBallZ = BAz / spikeFlightTime;
        float vBallY = (pointB.y - pointA.y - 0.5f * g * spikeFlightTime * spikeFlightTime) / spikeFlightTime;
        Vector3 vBallPost = new Vector3(vBallX, vBallY, vBallZ);

        if (vBallPost.magnitude > vMax)
        {
            float a = 0.25f * g * g;
            float b = g * spikeHeight - vMax * vMax;
            float c = spikeHeight * spikeHeight + BAx * BAx + BAz * BAz;
            float det = b * b - 4f * a * c;
            if (det < 0f) return false;

            float t_rising = (-b + Mathf.Sqrt(det)) / (2f * a);
            float t_falling = (-b - Mathf.Sqrt(det)) / (2f * a);
            float tb = Mathf.Sqrt(Mathf.Max(t_rising, t_falling));

            vBallX = BAx / tb;
            vBallZ = BAz / tb;
            vBallY = (pointA.y - pointB.y + 0.5f * g * tb * tb) / tb;
            vBallPost = new Vector3(vBallX, vBallY, vBallZ);
        }

        if (Mathf.Abs(vBallX) > 0.001f)
        {
            float alpha = (netX - pointA.x) / BAx;
            if (alpha > 0f && alpha < 1f)
            {
                float currentTb = BAx / vBallX;
                float tNetCheck = alpha * currentTb;
                float yNet = pointA.y + vBallY * tNetCheck + 0.5f * g * tNetCheck * tNetCheck;
                float neededY = netHeightSafe + 0.5f;

                if (yNet < neededY)
                {
                    float linY = pointA.y + alpha * (pointB.y - pointA.y);
                    float curveFactor = 0.5f * g * alpha * (alpha - 1f);
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

    // OnCollisionEnter：ボール速度を API 値（CalcBallVelocity）から付与
    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != State.Striking) return;
        if (targetBall == null || collision.gameObject != targetBall) return;
        if (!collision.gameObject.CompareTag(ballTag)) return;

        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        if (MatchManager.Instance != null)
            MatchManager.Instance.lastTeamToHit = myTeam;

        TimingResult timingResult = timingWindow != null ? timingWindow.LastResult : TimingResult.None;
        float speedMult = staminaSystem != null
            ? staminaSystem.ConsumeChargeWithTiming(pendingVelocity, timingResult, tossQuality)
            : 1f;
        timingWindow?.Reset();

        ballRb.linearVelocity = CalcBallVelocity(collision.transform.position) * speedMult;
        rb.linearVelocity = Vector3.zero;

        lastSpikedBall = collision.gameObject;
        currentState = State.Returning;
    }

    // ── 以下 SpikerAllyEnemyV2 と完全同一 ────────────────────────────

    bool TryGetTrajectoryAvoidVector(out Vector3 avoidVector)
    {
        avoidVector = Vector3.zero;

        Rigidbody checkRb = targetRb;
        if (checkRb == null)
        {
            GameObject anyBall = GameObject.FindGameObjectWithTag(ballTag);
            if (anyBall == null || anyBall == lastSpikedBall) return false;
            if (!IsBallOnMySide(anyBall.transform.position)) return false;
            checkRb = anyBall.GetComponent<Rigidbody>();
            if (checkRb == null) return false;
        }

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

        Vector3 awayDir = transform.position - closestBallPos;

        if (awayDir.magnitude < 0.01f)
        {
            Vector3 ballVelAtT = new Vector3(
                checkRb.linearVelocity.x,
                checkRb.linearVelocity.y + g * closestT,
                checkRb.linearVelocity.z
            );
            awayDir = Vector3.Cross(ballVelAtT.normalized, Vector3.up);
            if (awayDir.magnitude < 0.01f) awayDir = Vector3.forward;
        }

        float strength = 1f - (minDist / trajectoryCheckRadius);
        avoidVector = awayDir.normalized * strength * trajectoryAvoidSpeed;
        return true;
    }

    Vector3 PredictBallPosition(Rigidbody ballRb, float t)
    {
        return new Vector3(
            ballRb.position.x + ballRb.linearVelocity.x * t,
            ballRb.position.y + ballRb.linearVelocity.y * t + 0.5f * g * t * t,
            ballRb.position.z + ballRb.linearVelocity.z * t
        );
    }

    void SetNonTargetBallIgnore(bool ignore)
    {
        Collider myCol = GetComponent<Collider>();
        if (myCol == null) return;
        GameObject[] balls = GameObject.FindGameObjectsWithTag(ballTag);
        foreach (var ball in balls)
        {
            if (ball == targetBall) continue;
            Collider bc = ball.GetComponent<Collider>();
            if (bc != null) Physics.IgnoreCollision(myCol, bc, ignore);
        }
    }

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

    bool IsBallOnMySide(Vector3 ballPos)
    {
        return myTeam == Team.Ally ? ballPos.x > netX : ballPos.x < netX;
    }

    float CalculateFalling(float h)
    {
        float y0 = targetRb.position.y;
        float vy0 = targetRb.linearVelocity.y;

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

    // ── 追加メソッド ──────────────────────────────────────────────────

    void DetectTossType()
    {
        GameObject ball = GameObject.FindGameObjectWithTag(ballTag);
        if (ball == null) return;
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        float vy = ballRb.linearVelocity.y;
        float apex = (vy > 0f)
            ? ballRb.position.y + (vy * vy) / (2f * Mathf.Abs(g))
            : ballRb.position.y;

        if (apex > highTossApexThreshold)
            tossQuality = TossQuality.High;
        else if (apex > medTossApexThreshold)
            tossQuality = TossQuality.Medium;
        else
            tossQuality = TossQuality.Low;
    }

    /// API の pendingCourse/pendingVelocity から実際の打球速度ベクトルを計算する
    Vector3 CalcBallVelocity(Vector3 hitPos)
    {
        float norm = Mathf.Clamp01(pendingVelocity / vMaxDrone);
        float targetX = (myTeam == Team.Ally)
            ? Mathf.Lerp(targetShallowX, targetDeepX, norm)
            : Mathf.Lerp(-targetShallowX, -targetDeepX, norm);
        float blur = staminaSystem != null ? staminaSystem.GetBlur() : 0f;
        Vector3 landing = new Vector3(
            targetX + Random.Range(-blur, blur),
            0f,
            pendingCourse * targetZHalf + Random.Range(-blur, blur));

        float dx = landing.x - hitPos.x;
        float dz = landing.z - hitPos.z;
        float T = Mathf.Sqrt(dx * dx + dz * dz) / Mathf.Max(pendingVelocity, 0.1f);
        float vx = dx / T;
        float vz = dz / T;
        float vy = (landing.y - hitPos.y - 0.5f * g * T * T) / T;

        if (Mathf.Abs(dx) > 0.001f)
        {
            float alpha = (netX - hitPos.x) / dx;
            if (alpha > 0.01f && alpha < 0.99f)
            {
                float tNet = alpha * T;
                float yNet = hitPos.y + vy * tNet + 0.5f * g * tNet * tNet;
                if (yNet < netHeightSafe)
                {
                    T *= 1.4f;
                    vx = dx / T;
                    vz = dz / T;
                    vy = (landing.y - hitPos.y - 0.5f * g * T * T) / T;
                }
            }
        }

        return new Vector3(vx, vy, vz);
    }
}
