// 落下予測とアウト判定付きAIレシーバー（Ally/Enemy対応）
/*
【newReceiverAllyEnemy】
概要:
  現行システムの主力レシーバー。MatchManager のフェーズ・チームを監視し、
  Receiving フェーズかつ自チームの所持ターンになるとボールの落下地点を予測して移動・レシーブする。
  アウト判定機能（IsBallGoingOut）を持ち、コート外へ飛ぶボールはレシーブしない。
  Ally/Enemy 両チームに対応（myTeam で切り替え）。

  トス品質は「余裕時間 = ボール到達時間 - レシーバー移動時間」で3段階に決定する。
    高トス: 余裕時間 >= highMarginThreshold  → スパイカーが強打可能
    中トス: 余裕時間 >= medMarginThreshold   → スパイカーが中程度の球を打てる
    低トス: 余裕時間 <  medMarginThreshold   → スパイカーは弱い球のみ

動作フロー: Waiting → Hovering → MovingToTrajectory → (衝突) → Returning

他スクリプトとの関係:
  ・MatchManager          ← フェーズ/チームを参照、Returning 時に currentPhase を Spiking へ変更
  ・BallResetOnCollision  ← ラリー終了時に ResetToInitialState() を呼ばれる
  ・SpikeDrone            ← トスの頂点高さを検知して TossQuality を判定する
*/
using UnityEngine;

public class newReceiverAllyEnemy : MonoBehaviour
{
    [SerializeField] private Team myTeam;
    public Rigidbody targetBall;
    public float moveSpeed = 15f;

    public Vector3 initialPos = new Vector3(10f, 1f, 0f);

    [Header("トス品質の設定（余裕時間ベース）")]
    [Tooltip("余裕時間がこれ以上 → 高トス")]
    public float highMarginThreshold = 1.0f;
    [Tooltip("余裕時間がこれ以上（かつ高トス未満）→ 中トス")]
    public float medMarginThreshold  = 0.3f;

    [Header("各トスの滞空時間（秒）")]
    public float highTossFlightTime = 3.5f;
    public float medTossFlightTime  = 2.8f;
    public float lowTossFlightTime  = 2.0f;

    [Header("コート境界設定（アウト判定）")]
    [Tooltip("AllyはX:0〜21, EnemyはX:-21〜0 をそれぞれ設定する")]
    public float courtXMin = 0f;
    public float courtXMax = 21f;
    public float courtZMin = -10f;
    public float courtZMax = 10f;

    // 余裕時間（FindAndCalculateBall で計算し OnCollisionEnter で使用）
    private float storedMargin = 0f;

    // UI表示用
    private string tossLabel = "";
    private float  labelTimer = 0f;
    private const float labelDuration = 3f;

    enum State { Waiting, Hovering, MovingToTrajectory, Receiving, Returning }
    [SerializeField] private State currentState = State.Waiting;

    void Start()
    {
        if (myTeam == Team.Enemy)
        {
            courtXMin = -21f;
            courtXMax = 0f;
        }
        else
        {
            courtXMin = 0f;
            courtXMax = 21f;
        }
    }

    void Update()
    {
        if (labelTimer > 0f)
            labelTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (labelTimer <= 0f) return;

        float w = 340f;
        float h = 60f;
        float x = myTeam == Team.Ally ? 30f : Screen.width - w - 30f;
        float y = 80f;

        string teamLabel = myTeam == Team.Ally ? "Ally" : "Enemy";

        Color textColor;
        if (tossLabel.Contains("高"))
            textColor = Color.cyan;
        else if (tossLabel.Contains("中"))
            textColor = Color.yellow;
        else
            textColor = new Color(1f, 0.5f, 0f);

        var boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 24 };
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        labelStyle.normal.textColor = textColor;

        GUI.Box(new Rect(x - 10, y - 10, w + 20, h + 20), "", boxStyle);
        GUI.Label(new Rect(x, y, w, h), $"[{teamLabel}] {tossLabel}", labelStyle);
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Waiting:
                if (MatchManager.Instance.currentPhase == MatchManager.GamePhase.Receiving &&
                    MatchManager.Instance.currentPossesion == myTeam)
                {
                    currentState = State.Hovering;
                }
                Hover(initialPos);
                break;

            case State.Hovering:
                FindAndCalculateBall();
                Hover(initialPos);
                break;

            case State.MovingToTrajectory:
                if (targetBall == null || IsBallGoingOut(targetBall))
                {
                    targetBall = null;
                    currentState = State.Hovering;
                    break;
                }
                Vector3 landingPos = PredictLandingPoint(targetBall.position, targetBall.linearVelocity, transform.position.y);
                Vector3 targetPos  = new Vector3(landingPos.x, transform.position.y, landingPos.z);
                Hover(targetPos);
                break;

            case State.Returning:
                MatchManager.Instance.currentPhase = MatchManager.GamePhase.Spiking;
                Hover(initialPos);
                if (Vector3.Distance(transform.position, initialPos) < 0.3f)
                    currentState = State.Waiting;
                break;
        }
    }

    void FindAndCalculateBall()
    {
        GameObject ball = GameObject.Find("injectionball(Clone)");
        if (ball == null) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        if (IsBallGoingOut(ballRb)) return;

        // 余裕時間 = ボールが自分の高さに到達するまでの時間 − 自分が移動しきるのにかかる時間
        Vector3 landing   = PredictLandingPoint(ballRb.position, ballRb.linearVelocity, transform.position.y);
        float   arrive    = PredictTimeToHeight(ballRb.position, ballRb.linearVelocity, transform.position.y);
        float   travel    = Vector3.Distance(transform.position, new Vector3(landing.x, transform.position.y, landing.z)) / moveSpeed;
        storedMargin      = arrive - travel;

        targetBall   = ballRb;
        currentState = State.MovingToTrajectory;
    }

    bool IsBallGoingOut(Rigidbody ballRb)
    {
        Vector3 landing = PredictLandingPoint(ballRb.position, ballRb.linearVelocity, 0f);
        return landing.x < courtXMin || landing.x > courtXMax ||
               landing.z < courtZMin || landing.z > courtZMax;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (currentState != State.MovingToTrajectory && currentState != State.Hovering) return;

        if (!collision.gameObject.CompareTag("injectionball")) return;

        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        if (MatchManager.Instance != null)
            MatchManager.Instance.lastTeamToHit = myTeam;

        Vector3 startPos = collision.transform.position;
        float tossFlightTime;

        // 余裕時間で3段階を決定
        if (storedMargin >= highMarginThreshold)
        {
            tossFlightTime = highTossFlightTime;
            tossLabel      = "高いトス！";
            Debug.Log($"[{myTeam}] 余裕あり(margin:{storedMargin:F2}s) → 高トス");
        }
        else if (storedMargin >= medMarginThreshold)
        {
            tossFlightTime = medTossFlightTime;
            tossLabel      = "中くらいのトス";
            Debug.Log($"[{myTeam}] 少し余裕(margin:{storedMargin:F2}s) → 中トス");
        }
        else
        {
            tossFlightTime = lowTossFlightTime;
            tossLabel      = "低いトス…";
            Debug.Log($"[{myTeam}] ギリギリ(margin:{storedMargin:F2}s) → 低トス");
        }
        labelTimer = labelDuration;

        float gravity = Physics.gravity.y;
        float vx = (initialPos.x - startPos.x) / tossFlightTime;
        float vz = (initialPos.z - startPos.z) / tossFlightTime;
        float vy = (initialPos.y - startPos.y - 0.5f * gravity * tossFlightTime * tossFlightTime) / tossFlightTime;

        ballRb.linearVelocity = new Vector3(vx, vy, vz);
        targetBall   = null;
        currentState = State.Returning;
    }

    void Hover(Vector3 target)
    {
        Rigidbody rb  = GetComponent<Rigidbody>();
        Vector3   diff = target - transform.position;
        float distance = diff.magnitude;
        if (distance < 0.1f)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = target;
            return;
        }
        rb.linearVelocity = diff.normalized * moveSpeed;
    }

    // ボールが targetY の高さに到達するまでの時間（放物線の解の最大値）
    float PredictTimeToHeight(Vector3 startPos, Vector3 velocity, float targetY)
    {
        float gravity      = Physics.gravity.y;
        float a            = 0.5f * gravity;
        float b            = velocity.y;
        float c            = startPos.y - targetY;
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return 0f;
        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        return Mathf.Max(t1, t2);
    }

    Vector3 PredictLandingPoint(Vector3 startPos, Vector3 velocity, float targetY)
    {
        float gravity      = Physics.gravity.y;
        float a            = 0.5f * gravity;
        float b            = velocity.y;
        float c            = startPos.y - targetY;
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return startPos;
        float t = Mathf.Max((-b + Mathf.Sqrt(discriminant)) / (2 * a),
                            (-b - Mathf.Sqrt(discriminant)) / (2 * a));
        return new Vector3(startPos.x + velocity.x * t, targetY, startPos.z + velocity.z * t);
    }

    public void ResetToInitialState()
    {
        currentState = State.Waiting;
        targetBall   = null;
        storedMargin = 0f;
        transform.position = initialPos;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }
}
