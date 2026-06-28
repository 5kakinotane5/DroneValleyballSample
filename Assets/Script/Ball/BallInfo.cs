// ボール情報の読み取り・設定を一元化するモジュール（ファサード）。
/*
【BallInfo】
概要:
  これまで各ドローンが個別にボールを Find / FindWithTag し、
  Rigidbody の position / linearVelocity を直接読み書きしていた処理を一元化する。
  「読み取り」をこのファサードに集約しておくことで、
  将来カメラ三角測量によるボール3D座標推定へ実装を差し替えられるようにする。
  （速度の「設定」は実ボールの Rigidbody への書き込み＝物理そのものなので残す）

責務:
  ・メインボール（tag: injectionball）の参照管理（検索＋キャッシュ、破棄時は再検索）
  ・位置 / 速度の読み取り（将来の差し替え境界）
  ・予測計算用の重力加速度の提供
  ・速度の設定

対象外:
  ・複数ボール同時存在時の「非ターゲットボール回避」などの個別 GameObject 走査
    （FindGameObjectsWithTag を使う処理）は別関心事のため各スクリプトに残す。
*/
using UnityEngine;

public static class BallInfo
{
    // constを付けない理由は、得点したときに古いボールが消えて新しいボールが生成されるため。
    private static Rigidbody ball;

    // サーブのときに生成されるボールを登録する。
    public static void Register(Rigidbody ball)
    {
        BallInfo.ball = ball;
    }

    public static bool Exists()
    {
        return ball != null;
    }

    // ボールの現在位置（無ければ Vector3.zero）。
    public static Vector3 GetPosition()
    {
        return ball != null ? ball.position : Vector3.zero;
    }

    // ボールの現在速度（無ければ Vector3.zero）。
    public static Vector3 GetVelocity()
    {
        return ball != null ? ball.linearVelocity : Vector3.zero;
    }

    // 予測計算で使う重力加速度（y成分）。
    public static float Gravity => Physics.gravity.y;

    // ボールの速度を設定する（実 Rigidbody への書き込み）。
    public static void SetVelocity(Vector3 velocity)
    {
        if (ball != null) ball.linearVelocity = velocity;
    }
}
