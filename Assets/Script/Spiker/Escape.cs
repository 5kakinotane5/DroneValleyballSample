using UnityEngine;

public class Escape
{
    private Predict _predict;

    public Escape(Predict predict)
    {
        if (predict == null)
        {
            throw new System.ArgumentNullException(nameof(predict));
        }
        _predict = predict;
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