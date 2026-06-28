// ボールに関する情報を取得したり、ボールを操作するためのモジュール.
// ボールの管理を一元的に行うために作成した. 
// スパイク, レシーブでボールの位置を予測したりボールに速度を与えたりするときにこのモジュールを使用する.
/*
    - ボールに関する情報を取得できる. (ex: ボールの位置、速度)
    - ボールを操作できる. (ex: ボールに速度を設定する)
*/

using UnityEngine;

public static class Ball
{
    // constを付けない理由は、得点したときに古いボールが消えて新しいボールが生成されるため。
    private static Rigidbody ball;

    // サーブのときに生成されるボールを登録する。
    public static void Register(Rigidbody ball)
    {
        Ball.ball = ball;
    }

    public static bool Exists()
    {
        return ball != null;
    }

    // ボールの現在位置を返す
    public static Vector3 GetPosition()
    {
        if (ball == null) throw new System.Exception("Ball: ball is null. Make sure to register the ball before calling GetPosition.");
        return ball.position;
    }

    // ボールの現在速度を返す
    public static Vector3 GetVelocity()
    {
        if (ball == null) throw new System.Exception("Ball: ball is null. Make sure to register the ball before calling GetVelocity.");
        return ball.linearVelocity;
    }

    // ボールの速度を設定する
    public static void SetVelocity(Vector3 velocity)
    {
        if (ball == null) throw new System.Exception("Ball: ball is null. Make sure to register the ball before calling SetVelocity.");
        ball.linearVelocity = velocity;
    }
}
