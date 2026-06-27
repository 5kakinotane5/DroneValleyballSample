// Enemy AI 専用スパイカードローン
// コントローラー不要・完全自動・StaminaSystem 対応
// SpikeDrone.cs のAIモード（isPlayerControlled=false）を独立させたもの
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpikeDrone : MonoBehaviour
{
    [SerializeField] private Team myTeam = Team.Enemy;
    public Team MyTeam => myTeam;

    [Header("ドローン速度設定")]
    public float tossBoost = 2f;
    public string ballTag = "injectionball";
    public float spikeHeight = 10f;
    public Vector3 initialPos = new Vector3(-10.5f, 6.0f, 0f);
    public float vMaxDrone = 40f;
    public float vMax => vMaxDrone * tossBoost;

    [Header("弾道パラメータ")]
    [SerializeField] private float spikeFlightTime = 0.6f;
    [SerializeField] private float runupTime = 0.2f;

    [Header("ネット安全設定")]
    public float netX = 0f;
    public float netHeightSafe = 4.9f;

    [Header("ターゲットボール軌道回避")]
    [SerializeField] private float trajectoryCheckRadius = 3f;
    [SerializeField] private float trajectoryAvoidSpeed = 25f;
    [SerializeField] private int trajectorySamples = 30;

    [Header("非ターゲットボール回避")]
    [SerializeField] private float dodgeRadius = 3f;
    [SerializeField] private float dodgeSpeed = 15f;

    [Header("トス判定しきい値")]
    public float highTossApexThreshold = 13f;
    public float medTossApexThreshold = 8f;

    [Header("速度比率（トス品質別）")]
    [Range(0.1f, 1.0f)] public float weakVelocityRatio = 0.35f;
    [Range(0.1f, 1.0f)] public float medVelocityRatio = 0.65f;

    [Header("着弾範囲（相手コート）")]
    public float targetShallowX = -3f;
    public float targetDeepX = -20f;
    public float targetZHalf = 9f;

    [Header("AI 配球設定")]
    [Tooltip("ブレをクランプするコート境界からの安全マージン（m）")]
    [SerializeField] private float courtMargin = 1.2f;
    [Tooltip("Ally SpikeDrone の参照（未設定時は Start() で自動検索）")]
    [SerializeField] private SpikeDrone allyDrone;
    [Tooltip("高トスで短距離フェイントを選ぶ確率（0=常に深い、1=常に短い）")]
    [Range(0f, 1f)]
    [SerializeField] private float highTossShortRate = 0.25f;

    [Header("スタミナ")]
    [SerializeField] private StaminaSystem staminaSystem;
    public StaminaSystem Stamina => staminaSystem;

    // ── 公開プロパティ ─────────────────────────────────────────────
    public bool isReady { get; private set; }
    public TossQuality CurrentTossQuality => tossQuality;
    public bool IsHighToss => tossQuality == TossQuality.High;

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
            float mult = staminaSystem != null ? staminaSystem.SpeedMultiplier : 1f;
            return tossBase * mult;
        }
    }

    // ── 内部状態 ───────────────────────────────────────────────────
    private Rigidbody rb;
    private Rigidbody targetRb;
    private GameObject targetBall;
    private Vector3 requiredDroneVel;
    private Vector3 pointA;
    private Vector3 standbyPoint;
    private float timeUntilImpact;
    private GameObject lastSpikedBall;
    private TossQuality tossQuality = TossQuality.Low;
    private float pendingCourse;
    private float pendingVelocity;
    private bool isAvoidingTrajectory;
    private float lastCourse = 1f;
    private readonly float g = BallInfo.Gravity;

    enum State { Waiting, Hovering, MovingToTrajectory, Striking, Returning }
    [SerializeField] private State currentState = State.Waiting;

    // ── Unity ─────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        transform.position = initialPos;

        if (allyDrone == null)
            allyDrone = FindObjectOfType<SpikeDrone>();
    }

    void FixedUpdate()
    {
        bool isBetweenPoints = MatchManager.Instance != null &&
            MatchManager.Instance.currentPhase == MatchManager.GamePhase.Waiting;
        if (!isBetweenPoints)
            staminaSystem?.RecoverTick(currentState == State.Waiting);

        if (currentState == State.MovingToTrajectory || currentState == State.Striking)
            timeUntilImpact -= Time.fixedDeltaTime;

        SetNonTargetBallIgnore(currentState == State.Striking || currentState == State.Waiting);

        switch (currentState)
        {
            case State.Waiting:
                isReady = false;
                rb.linearVelocity = Vector3.zero;
                if (MatchManager.Instance.currentPhase == MatchManager.GamePhase.Spiking &&
                    MatchManager.Instance.currentPossesion == myTeam)
                    currentState = State.Hovering;
                break;

            case State.Hovering:
                isReady = true;
                DetectTossType();
                isAvoidingTrajectory = TryGetTrajectoryAvoidVector(out Vector3 hoverAvoid);
                Hover(initialPos);
                if (isAvoidingTrajectory) rb.linearVelocity += hoverAvoid;
                ApplyDodgeVelocity();
                FindAndCalculateBall();
                break;

            case State.MovingToTrajectory:
                isReady = false;
                if (targetBall == null) { currentState = State.Returning; break; }
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
                MatchManager.Instance.ChangePossesion(myTeam == Team.Ally ? Team.Enemy : Team.Ally);
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

    // ── 内部処理 ───────────────────────────────────────────────────

    void FindAndCalculateBall()
    {
        GameObject ball = GameObject.FindGameObjectWithTag(ballTag);
        if (ball == null || ball == lastSpikedBall) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;
        BallInfo.Register(ballRb); // 追跡対象としてメインボールを登録
        if (!IsBallOnMySide(BallInfo.GetPosition())) return;

        if (BallInfo.GetVelocity().y > 0 &&
            BallInfo.GetPosition().y < spikeHeight &&
            MatchManager.Instance.currentPhase == MatchManager.GamePhase.Spiking)
        {
            ChooseStrategy();

            targetRb = ballRb;
            targetBall = ball;
            if (CalculateTrajectory())
                currentState = State.MovingToTrajectory;
            else
            {
                targetRb = null;
                targetBall = null;
            }
        }
    }

    /// <summary>
    /// Ally ドローンの現在位置を読み、コース（Z）と速度（深度）を戦術的に決定する。
    ///
    /// コース: Ally が Z 正側 → 負側へ、Ally が Z 負側 → 正側へ（いない方を狙う）。
    ///         Ally が中央付近のときは前回と逆のコーナーへ交互に配球。
    /// 深度 : Ally が浅い位置（ネット寄り） → 深い球。
    ///         Ally が深い位置              → 浅い球（フェイント）。
    /// </summary>
    void ChooseStrategy()
    {
        float maxV = CurrentMaxVelocity;

        float allyZ = allyDrone != null ? allyDrone.transform.position.z : 0f;
        float allyX = allyDrone != null ? allyDrone.transform.position.x : Mathf.Abs(targetDeepX) * 0.5f;

        // ── コース選択（Z 軸） ──────────────────────────────────────
        const float centerThreshold = 2f;
        if (Mathf.Abs(allyZ) < centerThreshold)
            pendingCourse = lastCourse >= 0f ? -1f : 1f;   // 中央→交互コーナー
        else
            pendingCourse = allyZ > 0f ? -1f : 1f;         // Ally の逆サイド
        lastCourse = pendingCourse;

        // ── 速度選択（X 深度） ──────────────────────────────────────
        // Ally コート内の前後位置を 0（ネット際）〜1（最深部）で正規化
        float courtDepth = Mathf.Abs(targetDeepX);
        float allyNormX = Mathf.Clamp01(allyX / courtDepth);
        // Ally の逆の深度を狙う
        float depthNorm = 1f - allyNormX;

        switch (tossQuality)
        {
            case TossQuality.High:
                // 高トス: 基本は Ally 逆深度への強打、一定確率で短距離フェイント
                pendingVelocity = Random.value < highTossShortRate
                    ? maxV * medVelocityRatio
                    : Mathf.Lerp(maxV * medVelocityRatio, maxV, depthNorm);
                break;
            case TossQuality.Medium:
                // 中トス: 深度戦術（速度範囲は weakVelocityRatio〜medVelocityRatio）
                pendingVelocity = Mathf.Lerp(maxV * weakVelocityRatio, maxV, depthNorm);
                break;
            default:
                // 低トス: 既に weakVelocityRatio で上限が低いのでフル速度
                pendingVelocity = maxV;
                break;
        }
    }

    void DetectTossType()
    {
        if (!BallInfo.TryGetState(out Vector3 ballPos, out Vector3 ballVel)) return;

        float vy = ballVel.y;
        float apex = vy > 0f
            ? ballPos.y + (vy * vy) / (2f * Mathf.Abs(g))
            : ballPos.y;

        if (apex > highTossApexThreshold) tossQuality = TossQuality.High;
        else if (apex > medTossApexThreshold) tossQuality = TossQuality.Medium;
        else tossQuality = TossQuality.Low;
    }

    bool CalculateTrajectory()
    {
        float t = CalculateFalling(spikeHeight);
        if (t < 0) return false;
        timeUntilImpact = t;

        float norm = Mathf.Clamp01(pendingVelocity / vMaxDrone);
        float targetX = myTeam == Team.Ally
            ? Mathf.Lerp(targetShallowX, targetDeepX, norm)
            : Mathf.Lerp(-targetShallowX, -targetDeepX, norm);
        float blur = staminaSystem != null ? staminaSystem.GetBlur() : 0f;

        Vector3 pointB = ClampLandingToCourt(new Vector3(
            targetX + Random.Range(-blur, blur),
            0f,
            pendingCourse * targetZHalf + Random.Range(-blur, blur)));

        Vector3 ballPos = BallInfo.GetPosition();
        Vector3 ballVel = BallInfo.GetVelocity();
        pointA = new Vector3(
            ballPos.x + ballVel.x * t,
            spikeHeight,
            ballPos.z + ballVel.z * t);

        float BAx = pointB.x - pointA.x;
        float BAz = pointB.z - pointA.z;

        float vBallX = BAx / spikeFlightTime;
        float vBallZ = BAz / spikeFlightTime;
        float vBallY = (pointB.y - pointA.y - 0.5f * g * spikeFlightTime * spikeFlightTime) / spikeFlightTime;
        Vector3 vBall = new Vector3(vBallX, vBallY, vBallZ);

        if (vBall.magnitude > vMax)
        {
            float a = 0.25f * g * g;
            float b = g * spikeHeight - vMax * vMax;
            float c = spikeHeight * spikeHeight + BAx * BAx + BAz * BAz;
            float det = b * b - 4f * a * c;
            if (det < 0f) return false;
            float tb = Mathf.Sqrt(Mathf.Max((-b + Mathf.Sqrt(det)) / (2f * a),
                                             (-b - Mathf.Sqrt(det)) / (2f * a)));
            vBallX = BAx / tb;
            vBallZ = BAz / tb;
            vBallY = (pointA.y - pointB.y + 0.5f * g * tb * tb) / tb;
            vBall = new Vector3(vBallX, vBallY, vBallZ);
        }

        // ネット安全チェック
        if (Mathf.Abs(vBallX) > 0.001f)
        {
            float alpha = (netX - pointA.x) / BAx;
            if (alpha > 0f && alpha < 1f)
            {
                float tbCur = BAx / vBallX;
                float tNet = alpha * tbCur;
                float yNet = pointA.y + vBallY * tNet + 0.5f * g * tNet * tNet;
                if (yNet < netHeightSafe + 0.5f)
                {
                    float linY = pointA.y + alpha * (pointB.y - pointA.y);
                    float curveFactor = 0.5f * g * alpha * (alpha - 1f);
                    if (curveFactor > 0.0001f)
                    {
                        float tb2Min = (netHeightSafe + 0.5f - linY) / curveFactor;
                        if (tb2Min > tbCur * tbCur)
                        {
                            float tbNew = Mathf.Sqrt(tb2Min);
                            vBallX = BAx / tbNew;
                            vBallZ = BAz / tbNew;
                            vBallY = (pointB.y - pointA.y - 0.5f * g * tbNew * tbNew) / tbNew;
                            vBall = new Vector3(vBallX, vBallY, vBallZ);
                        }
                    }
                }
            }
        }

        requiredDroneVel = vBall / tossBoost;
        standbyPoint = pointA - requiredDroneVel * Mathf.Min(runupTime, t);
        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != State.Striking) return;
        if (targetBall == null || collision.gameObject != targetBall) return;
        if (!collision.gameObject.CompareTag(ballTag)) return;

        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb == null) return;
        BallInfo.Register(ballRb); // 確実な実体を登録

        if (MatchManager.Instance != null)
            MatchManager.Instance.lastTeamToHit = myTeam;

        if (staminaSystem != null) staminaSystem.RecoveryBlocked = false;
        staminaSystem?.ConsumeCharge(pendingVelocity);

        // 打球速度を計算してボールに与える
        float norm = Mathf.Clamp01(pendingVelocity / vMaxDrone);
        float targetX = myTeam == Team.Ally
            ? Mathf.Lerp(targetShallowX, targetDeepX, norm)
            : Mathf.Lerp(-targetShallowX, -targetDeepX, norm);
        float blur = staminaSystem != null ? staminaSystem.GetBlur() : 0f;

        Vector3 landing = ClampLandingToCourt(new Vector3(
            targetX + Random.Range(-blur, blur),
            0f,
            pendingCourse * targetZHalf + Random.Range(-blur, blur)));

        Vector3 hitPos = collision.transform.position;
        float dx = landing.x - hitPos.x;
        float dz = landing.z - hitPos.z;
        float horizontalDist = Mathf.Sqrt(dx * dx + dz * dz);

        // vy ≤ 0 を保証する最低水平速度: T_max = sqrt(2h/|g|) → minSpeed = dist/T_max
        // スタミナ枯渇など pendingVelocity が小さすぎると T 過大になり vy > 0（山なり）になる
        float heightDiff = Mathf.Max(hitPos.y - landing.y, 0.1f);
        float minFlatSpeed = horizontalDist * Mathf.Sqrt(Mathf.Abs(g) / (2f * heightDiff));
        float speed = Mathf.Max(pendingVelocity, minFlatSpeed, 0.1f);

        float T = horizontalDist / speed;
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
                if (yNet < netHeightSafe) { T *= 1.4f; vx = dx / T; vz = dz / T; vy = (landing.y - hitPos.y - 0.5f * g * T * T) / T; }
            }
        }

        BallInfo.SetVelocity(new Vector3(vx, vy, vz));
        rb.linearVelocity = Vector3.zero;
        lastSpikedBall = collision.gameObject;
        currentState = State.Returning;
    }

    /// <summary>
    /// 着弾点を相手コート内にクランプしてアウトを防ぐ。
    /// ブレで境界を超えた場合は courtMargin だけ内側の位置に止める。
    /// </summary>
    Vector3 ClampLandingToCourt(Vector3 landing)
    {
        float zRange = targetZHalf - courtMargin;

        if (myTeam == Team.Enemy)
        {
            // Enemy → Ally コート（X 正値側）
            float xMin = netX + courtMargin;
            float xMax = Mathf.Abs(targetDeepX) - courtMargin;
            landing.x = Mathf.Clamp(landing.x, xMin, xMax);
        }
        else
        {
            // Ally → Enemy コート（X 負値側）
            float xMin = -(Mathf.Abs(targetDeepX) - courtMargin);
            float xMax = -(netX + courtMargin);
            landing.x = Mathf.Clamp(landing.x, xMin, xMax);
        }
        landing.z = Mathf.Clamp(landing.z, -zRange, zRange);
        return landing;
    }

    // ── ユーティリティ（SpikeDrone と同一） ───────────────────────

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

        float duration = timeUntilImpact > 0.05f ? timeUntilImpact : 3f;
        float minDist = float.MaxValue;
        Vector3 closestBallPos = Vector3.zero;
        float closestT = 0f;

        for (int i = 0; i <= trajectorySamples; i++)
        {
            float t = duration * i / trajectorySamples;
            Vector3 bp = PredictBallPosition(checkRb, t);
            float d = Vector3.Distance(transform.position, bp);
            if (d < minDist) { minDist = d; closestBallPos = bp; closestT = t; }
        }

        if (minDist >= trajectoryCheckRadius) return false;

        Vector3 awayDir = transform.position - closestBallPos;
        if (awayDir.magnitude < 0.01f)
        {
            Vector3 ballVelAtT = new Vector3(checkRb.linearVelocity.x,
                                              checkRb.linearVelocity.y + g * closestT,
                                              checkRb.linearVelocity.z);
            awayDir = Vector3.Cross(ballVelAtT.normalized, Vector3.up);
            if (awayDir.magnitude < 0.01f) awayDir = Vector3.forward;
        }

        avoidVector = awayDir.normalized * (1f - minDist / trajectoryCheckRadius) * trajectoryAvoidSpeed;
        return true;
    }

    Vector3 PredictBallPosition(Rigidbody ballRb, float t) =>
        new Vector3(ballRb.position.x + ballRb.linearVelocity.x * t,
                    ballRb.position.y + ballRb.linearVelocity.y * t + 0.5f * g * t * t,
                    ballRb.position.z + ballRb.linearVelocity.z * t);

    void SetNonTargetBallIgnore(bool ignore)
    {
        Collider myCol = GetComponent<Collider>();
        if (myCol == null) return;
        foreach (var ball in GameObject.FindGameObjectsWithTag(ballTag))
        {
            if (ball == targetBall) continue;
            Collider bc = ball.GetComponent<Collider>();
            if (bc != null) Physics.IgnoreCollision(myCol, bc, ignore);
        }
    }

    void ApplyDodgeVelocity()
    {
        Vector3 dodge = Vector3.zero;
        foreach (var col in Physics.OverlapSphere(transform.position, dodgeRadius))
        {
            if (!col.CompareTag(ballTag) || col.gameObject == targetBall) continue;
            Vector3 away = transform.position - col.transform.position;
            float dist = away.magnitude;
            if (dist < 0.001f) continue;
            dodge += away.normalized * (1f - Mathf.Clamp01(dist / dodgeRadius)) * dodgeSpeed;
        }
        if (dodge.sqrMagnitude > 0.001f)
        {
            rb.linearVelocity += dodge;
            if (rb.linearVelocity.magnitude > vMaxDrone)
                rb.linearVelocity = rb.linearVelocity.normalized * vMaxDrone;
        }
    }

    bool IsBallOnMySide(Vector3 ballPos) =>
        myTeam == Team.Ally ? ballPos.x > netX : ballPos.x < netX;

    float CalculateFalling(float h)
    {
        float y0 = BallInfo.GetPosition().y;
        float vy0 = BallInfo.GetVelocity().y;
        float a = 0.5f * g;
        float b = vy0;
        float c = y0 - h;
        float det = b * b - 4 * a * c;
        if (det < 0) return -1;
        return Mathf.Max((-b + Mathf.Sqrt(det)) / (2 * a),
                         (-b - Mathf.Sqrt(det)) / (2 * a));
    }

    void MoveToPoint(Vector3 target)
    {
        rb.linearVelocity = (target - transform.position) / 0.8f;
        if (rb.linearVelocity.magnitude > vMax)
            rb.linearVelocity = rb.linearVelocity.normalized * vMax;
    }

    void Hover(Vector3 target)
    {
        Vector3 diff = target - transform.position;
        if (diff.magnitude < 0.3f) { rb.linearVelocity = Vector3.zero; transform.position = target; return; }
        rb.linearVelocity = diff.normalized * vMaxDrone / 8f;
    }
}
