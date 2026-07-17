using UnityEngine;

public class Escape
{
    private BallGetterOnDrone _ballGetter;
    private BallVelocity _ballVelocity;
    private Predict _predict;
    private Team _myTeam;
    private float _netX;

    public Escape(BallGetterOnDrone ballGetter, BallVelocity ballVelocity, Predict predict, Team team, float netX)
    {
        if (ballGetter == null)
        {
            throw new System.ArgumentNullException(nameof(ballGetter));
        }
        if (ballVelocity == null)
        {
            throw new System.ArgumentNullException(nameof(ballVelocity));
        }
        if (predict == null)
        {
            throw new System.ArgumentNullException(nameof(predict));
        }
        _predict = predict;
        _myTeam = team;
        _ballGetter = ballGetter;
        _ballVelocity = ballVelocity;
        _netX = netX;
    }

    private const float _trajectoryCheckRadius = 3f;
    private const float _trajectoryAvoidSpeed = 25f;

    public bool TryGetTrajectoryAvoidVector(Rigidbody rb, Vector3 position, int trajectorySamples, float timeUntilImpact, out Vector3 avoidVector)
    {
        avoidVector = Vector3.zero;

        // targetRb 確定前はコート上のボールを対象にするため、自陣側にあるときだけ回避する。
        // targetRb 確定後は捕捉済みなのでサイド判定を省く（従来挙動を維持）。
        Vector3? ballPos = _ballGetter.GetPosition();
        if (!ballPos.HasValue)
        {
            return false;
        }
        if (rb == null && !Side.IsBallOnMySide(_myTeam, ballPos.Value, _netX))
        {
            return false;
        }

        float duration = (timeUntilImpact > 0.05f) ? timeUntilImpact : 3f;

        if (!TryGetClosestApproachNormal(position, ballPos.Value, _ballVelocity.GetEstimatedBallVelocity(), duration, trajectorySamples,
                out Vector3 normalDir, out float minDist))
        {
            return false;
        }

        if (minDist >= _trajectoryCheckRadius)
        {
            return false;
        }

        float strength = 1f - (minDist / _trajectoryCheckRadius);
        avoidVector = normalDir * strength * _trajectoryAvoidSpeed;
        return true;
    }

    private const float _dodgeRadius = 3f;
    private const float _dodgeSpeed = 15f;
    private const float _dodgePredictionTime = 1f;  // 最接近点を探す予測時間

    // dodgeRadius内の非ターゲットボールを、最接近点の法線ベクトル方向に回避
    public void ApplyDodgeVelocity(Rigidbody rb, Vector3 position, GameObject targetBall, string ballTag,
        int trajectorySamples, float vMaxDrone)
    {
        Vector3 dodge = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(position, _dodgeRadius);
        foreach (var col in nearby)
        {
            if (!col.CompareTag(ballTag)) continue;
            if (col.gameObject == targetBall) continue;

            Rigidbody ballRb = col.attachedRigidbody;
            if (ballRb == null) continue;

            if (!TryGetClosestApproachNormal(position, ballRb.position, ballRb.linearVelocity, _dodgePredictionTime,
                    trajectorySamples, out Vector3 normalDir, out float minDist))
                continue;
            if (minDist >= _dodgeRadius) continue;

            float weight = 1f - Mathf.Clamp01(minDist / _dodgeRadius);
            dodge += normalDir * weight * _dodgeSpeed;
        }

        if (dodge.sqrMagnitude > 0.001f)
        {
            rb.linearVelocity += dodge;
            if (rb.linearVelocity.magnitude > vMaxDrone)
                rb.linearVelocity = rb.linearVelocity.normalized * vMaxDrone;
        }
    }

    // ボール軌道を duration 秒先までサンプリングし、ドローンと最も近づく点を探す。
    // その最接近点でのボールからドローンへの方向は、軌道の接線（速度）にほぼ垂直な
    // 「法線ベクトル」になる（距離が最小になる点では、距離ベクトルと速度が直交するため）。
    public bool TryGetClosestApproachNormal(Vector3 position, Vector3 ballPos, Vector3 ballVel, float duration, int samples,
        out Vector3 normalDir, out float minDist)
    {
        minDist = float.MaxValue;
        Vector3 closestBallPos = ballPos;
        float closestT = 0f;

        for (int i = 0; i <= samples; i++)
        {
            float t = duration * i / samples;
            Vector3 bp = _predict.PredictPosition(ballPos, ballVel, t);
            float d = Vector3.Distance(position, bp);
            if (d < minDist)
            {
                minDist = d;
                closestBallPos = bp;
                closestT = t;
            }
        }

        // transformが必要
        Vector3 awayDir = position - closestBallPos;

        if (awayDir.magnitude < 0.01f)
        {
            Vector3 ballVelAtT = new Vector3(ballVel.x, ballVel.y + Physics.gravity.y * closestT, ballVel.z);
            awayDir = Vector3.Cross(ballVelAtT.normalized, Vector3.up);
            if (awayDir.magnitude < 0.01f)
                awayDir = Vector3.forward;
        }

        normalDir = awayDir.normalized;
        return true;
    }
}