// 落下予測・アウト判定・先読み配置付きAIレシーバー（Ally/Enemy対応）
using UnityEngine;
using System.Collections.Generic;

public class ReceiverAllyEnemy : MonoBehaviour
{
    [SerializeField] private Team myTeam;
    // ボールを追跡中かどうか（ボールの実体・速度は BallInfo 経由で取得する）。
    private bool tracking = false;
    public float moveSpeed = 10f;

    public Vector3 initialPos = new Vector3(10f, 1f, 0f);
    public float returnFlightTime = 3f;

    [Header("コート境界設定（アウト判定）")]
    [Tooltip("AllyはX:0〜21, EnemyはX:-21〜0 をそれぞれ設定する")]
    public float courtXMin = 0f;
    public float courtXMax = 21f;
    public float courtZMin = -10f;
    public float courtZMax = 10f;

    [Header("先読み待機位置")]
    [Tooltip("過去着地点を何球分記憶するか")]
    [SerializeField] private int anticipateHistorySize = 5;
    [Tooltip("0=常にinitialPos、1=完全に予測位置へ移動")]
    [Range(0f, 1f)]
    [SerializeField] private float anticipateWeight = 0.6f;

    private readonly Queue<Vector3> landingHistory = new Queue<Vector3>();
    private Vector3 hoverTarget;

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
        hoverTarget = initialPos;
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
                // ポイント間は先読み位置で待機
                Hover(hoverTarget);
                break;

            case State.Hovering:
                FindAndCalculateBall();
                Hover(hoverTarget);
                break;

            case State.MovingToTrajectory:
                if (!tracking || !BallInfo.Exists || IsBallGoingOut())
                {
                    tracking = false;
                    currentState = State.Hovering;
                    break;
                }
                Vector3 landingPos = PredictLandingPoint(BallInfo.Position, BallInfo.Velocity, transform.position.y);
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
        if (!BallInfo.Exists) return;

        if (IsBallGoingOut()) return;

        tracking = true;
        currentState = State.MovingToTrajectory;
    }

    bool IsBallGoingOut()
    {
        Vector3 landing = PredictLandingPoint(BallInfo.Position, BallInfo.Velocity, 0f);
        return landing.x < courtXMin || landing.x > courtXMax ||
               landing.z < courtZMin || landing.z > courtZMax;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (currentState != State.MovingToTrajectory && currentState != State.Hovering) return;

        if (collision.gameObject.CompareTag("injectionball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb == null) return;
            BallInfo.Register(ballRb); // 確実な実体を登録

            if (MatchManager.Instance != null)
                MatchManager.Instance.lastTeamToHit = myTeam;

            // レシーブ前の着地予測点を記録して次の待機位置を更新
            Vector3 incomingLanding = PredictLandingPoint(BallInfo.Position, BallInfo.Velocity, 0f);
            RecordLanding(incomingLanding);
            UpdateHoverTarget();

            Vector3 startPos = collision.transform.position;
            float vx = (initialPos.x - startPos.x) / returnFlightTime;
            float vz = (initialPos.z - startPos.z) / returnFlightTime;
            float gravity = BallInfo.Gravity;
            float vy = (initialPos.y - startPos.y - 0.5f * gravity * returnFlightTime * returnFlightTime) / returnFlightTime;
            BallInfo.SetVelocity(new Vector3(vx, vy, vz));
            tracking = false;
            currentState = State.Returning;
        }
    }

    void RecordLanding(Vector3 landing)
    {
        // コート内への球のみ記録
        if (landing.x < courtXMin || landing.x > courtXMax ||
            landing.z < courtZMin || landing.z > courtZMax) return;

        landingHistory.Enqueue(new Vector3(landing.x, initialPos.y, landing.z));
        if (landingHistory.Count > anticipateHistorySize)
            landingHistory.Dequeue();
    }

    void UpdateHoverTarget()
    {
        if (landingHistory.Count == 0) { hoverTarget = initialPos; return; }

        // 直近ほど重みを大きくした加重平均
        Vector3 weightedSum = Vector3.zero;
        float totalWeight = 0f;
        int i = 0;
        foreach (var p in landingHistory)
        {
            float w = i + 1f; // 古い順に 1, 2, 3, ... と重みを増やす
            weightedSum += p * w;
            totalWeight += w;
            i++;
        }
        Vector3 predicted = weightedSum / totalWeight;

        // initialPos と予測位置をブレンドしてコート内にクランプ
        Vector3 blended = Vector3.Lerp(initialPos, predicted, anticipateWeight);
        blended.x = Mathf.Clamp(blended.x, courtXMin + 0.5f, courtXMax - 0.5f);
        blended.z = Mathf.Clamp(blended.z, courtZMin + 0.5f, courtZMax - 0.5f);
        hoverTarget = blended;
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
        float gravity = BallInfo.Gravity;
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
        tracking = false;
        transform.position = initialPos;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        // 履歴はリセットしない（ラリーをまたいで学習し続ける）
    }
}
