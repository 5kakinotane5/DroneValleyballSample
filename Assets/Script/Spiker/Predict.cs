using UnityEngine;

public class Predict
{
    private BallEstimator _ball;

    public Predict(BallEstimator ballGetter)
    {
        if (ballGetter == null)
        {
            throw new System.ArgumentNullException(nameof(ballGetter));
        }
        _ball = ballGetter;
    }

    public Vector3 PredictPosition(Vector3 pos, Vector3 vel, float t)
    {
        return new Vector3(pos.x + vel.x * t,
                           pos.y + vel.y * t + 0.5f * Physics.gravity.y * t * t,
                           pos.z + vel.z * t);
    }

    public Vector3 PredictBallPosition(float t)
    {
        Vector3? ballPos = _ball.GetPosition();
        if (!ballPos.HasValue)
        {
            return Vector3.zero;
        }
        return PredictPosition(ballPos.Value, _ball.GetVelocity(), t);
    }
}
