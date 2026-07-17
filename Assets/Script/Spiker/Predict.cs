using UnityEngine;

public class Predict
{
    private BallGetterOnDrone _ballGetter;
    private BallVelocity _ballVelocity;

    public Predict(BallGetterOnDrone ballGetter, BallVelocity ballVelocity)
    {
        if (ballGetter == null)
        {
            throw new System.ArgumentNullException(nameof(ballGetter));
        }
        if (ballVelocity == null)
        {
            throw new System.ArgumentNullException(nameof(ballVelocity));
        }
        _ballGetter = ballGetter;
        _ballVelocity = ballVelocity;
    }

    public Vector3 PredictPosition(Vector3 pos, Vector3 vel, float t)
    {
        return new Vector3(pos.x + vel.x * t,
                           pos.y + vel.y * t + 0.5f * Physics.gravity.y * t * t,
                           pos.z + vel.z * t);
    }

    public Vector3 PredictBallPosition(float t)
    {
        Vector3? ballPos = _ballGetter.GetPosition();
        if (!ballPos.HasValue)
        {
            return Vector3.zero;
        }
        return PredictPosition(ballPos.Value, _ballVelocity.GetEstimatedBallVelocity(), t);
    }
}
