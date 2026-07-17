// Spiker/以下のスクリプトで全く同じ処理をしている関数が多いので,
// それらをまとめるためのクラス.

using UnityEngine;

public class BallVelocity
{
    private BallGetterOnDrone _ballGetter;
    private bool _hasLastBallPos;
    private Vector3 _estimatedBallVelocity;
    private Vector3 _lastBallPos;

    public BallVelocity(BallGetterOnDrone ballGetter)
    {
        if (ballGetter == null)
        {
            throw new System.ArgumentNullException(nameof(ballGetter));
        }
        _ballGetter = ballGetter;
    }

    public Vector3 GetEstimatedBallVelocity()
    {
        return _estimatedBallVelocity;
    }

    public void UpdateEstimatedBallVelocity()
    {
        if (!Ball.Exists())
        {
            _hasLastBallPos = false;
            _estimatedBallVelocity = Vector3.zero;
            return;
        }

        Vector3? currentPos = _ballGetter.GetPosition();
        if (!currentPos.HasValue)
        {
            _hasLastBallPos = false;
            _estimatedBallVelocity = Vector3.zero;
            return;
        }

        if (_hasLastBallPos)
            _estimatedBallVelocity = (currentPos.Value - _lastBallPos) / Time.fixedDeltaTime;

        _lastBallPos = currentPos.Value;
        _hasLastBallPos = true;
    }
}