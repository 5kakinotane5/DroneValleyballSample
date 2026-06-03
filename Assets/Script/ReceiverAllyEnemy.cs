/*
【ReceiverAllyEnemy】
概要:
  現行システムの主力レシーバー。MatchManager のフェーズ・チームを監視し、
  Receiving フェーズかつ自チームの所持ターンになるとボールの落下地点を予測して移動・レシーブする。
  アウト判定機能（IsBallGoingOut）を持ち、コート外へ飛ぶボールはレシーブしない。
  Ally/Enemy 両チームに対応（myTeam で切り替え）。

動作フロー: Waiting → Hovering → MovingToTrajectory → (衝突) → Returning

他スクリプトとの関係:
  ・MatchManager          ← フェーズ/チームを参照、Returning 時に currentPhase を Spiking へ変更
  ・BallResetOnCollision  ← ラリー終了時に ResetToInitialState() を呼ばれる
*/
using UnityEngine;

public class ReceiverAllyEnemy : MonoBehaviour
{
    [SerializeField] private Team myTeam;
    public Rigidbody targetBall;
    public float moveSpeed = 10f;

    public Vector3 initialPos = new Vector3(10f, 1f, 0f);
    public float returnFlightTime = 3f;

    [Header("コート境界設定（アウト判定）")]
    [Tooltip("AllyはX:0〜21, EnemyはX:-21〜0 をそれぞれ設定する")]
    public float courtXMin = 0f;
    public float courtXMax = 21f;
    public float courtZMin = -10f;
    public float courtZMax = 10f;

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
                // ボールが消えた、またはアウトコースと再判定された場合はHoveringに戻る
                if (targetBall == null || IsBallGoingOut(targetBall))
                {
                    targetBall = null;
                    currentState = State.Hovering;
                    break;
                }
                Vector3 landingPos = PredictLandingPoint(targetBall.position, targetBall.linearVelocity, transform.position.y);
                Vector3 targetPos = new Vector3(landingPos.x, transform.position.y, landingPos.z);
                Hover(targetPos);
                break;

            case State.Returning:
                MatchManager.Instance.currentPhase = MatchManager.GamePhase.Spiking;
                Hover(initialPos);
                if (Vector3.Distance(transform.position, initialPos) < 0.3f)
                {
                    currentState = State.Waiting;
                }
                break;
        }
    }

    void FindAndCalculateBall()
    {
        GameObject ball = GameObject.Find("injectionball(Clone)");
        if (ball == null) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        // 球がコート外に落ちると予測される場合はレシーブしない
        if (IsBallGoingOut(ballRb)) return;

        targetBall = ballRb;
        currentState = State.MovingToTrajectory;
    }

    // Y=0（地面）への着地予測がコート境界外かどうかを判定
    bool IsBallGoingOut(Rigidbody ballRb)
    {
        Vector3 landing = PredictLandingPoint(ballRb.position, ballRb.linearVelocity, 0f);
        return landing.x < courtXMin || landing.x > courtXMax ||
               landing.z < courtZMin || landing.z > courtZMax;
    }

    void OnCollisionEnter(Collision collision)
    {
        // MovingToTrajectory または Hovering 状態のときだけレシーブする
        // Returning 中に球が再接触しても無視（2重レシーブ防止）
        if (currentState != State.MovingToTrajectory && currentState != State.Hovering) return;

        if (collision.gameObject.CompareTag("injectionball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                if (MatchManager.Instance != null)
                    MatchManager.Instance.lastTeamToHit = myTeam;

                Vector3 startPos = collision.transform.position;
                float vx = (initialPos.x - startPos.x) / returnFlightTime;
                float vz = (initialPos.z - startPos.z) / returnFlightTime;
                float gravity = Physics.gravity.y;
                float vy = (initialPos.y - startPos.y - 0.5f * gravity * returnFlightTime * returnFlightTime) / returnFlightTime;
                ballRb.linearVelocity = new Vector3(vx, vy, vz);
                targetBall = null;
                currentState = State.Returning;
            }
        }
    }

    void Hover(Vector3 target)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 diff = target - transform.position;
        float distance = diff.magnitude;
        if (distance < 0.1f)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = target;
            return;
        }
        rb.linearVelocity = diff.normalized * moveSpeed;
    }

    Vector3 PredictLandingPoint(Vector3 startPos, Vector3 velocity, float targetY)
    {
        float gravity = Physics.gravity.y;
        float a = 0.5f * gravity;
        float b = velocity.y;
        float c = startPos.y - targetY;
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return startPos;
        float t = Mathf.Max((-b + Mathf.Sqrt(discriminant)) / (2 * a), (-b - Mathf.Sqrt(discriminant)) / (2 * a));
        return new Vector3(startPos.x + velocity.x * t, targetY, startPos.z + velocity.z * t);
    }

    public void ResetToInitialState()
    {
        currentState = State.Waiting;
        targetBall = null;
        transform.position = initialPos;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }
}
