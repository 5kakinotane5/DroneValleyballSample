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
    // ボールのタグ。実行時の実体名は injectionball(Clone)。
    private const string BallTag = "injectionball";

    // 現在追跡しているボールの Rigidbody（破棄/未生成時は null）。
    private static Rigidbody cachedRb;

    // ボールを明示的に登録する（サーブ生成直後や衝突時など、確実な実体を握っているとき）。
    public static void Register(Rigidbody ballRb)
    {
        cachedRb = ballRb;
    }

    // 現在のボール Rigidbody を返す。キャッシュが無効ならタグで再検索する。なければ null。
    private static Rigidbody ResolveRigidbody()
    {
        // Unity のオーバーロード == により、破棄済みオブジェクトは null 判定になる。
        if (cachedRb == null)
        {
            GameObject ball = GameObject.FindGameObjectWithTag(BallTag);
            cachedRb = ball?.GetComponent<Rigidbody>();
        }
        return cachedRb;
    }

    // ボールが存在するか。
    public static bool Exists => ResolveRigidbody() != null;

    // 位置・速度をまとめて取得する。取得できなければ false（out は zero）。
    public static bool TryGetState(out Vector3 position, out Vector3 velocity)
    {
        Rigidbody rb = ResolveRigidbody();
        if (rb == null)
        {
            position = Vector3.zero;
            velocity = Vector3.zero;
            return false;
        }
        position = GetPosition();
        velocity = GetVelocity();
        return true;
    }

    // ボールの現在位置（無ければ Vector3.zero）。
    public static Vector3 GetPosition()
    {
        Rigidbody rb = ResolveRigidbody();
        return rb != null ? rb.position : Vector3.zero;
    }

    // ボールの現在速度（無ければ Vector3.zero）。
    public static Vector3 GetVelocity()
    {
        Rigidbody rb = ResolveRigidbody();
        return rb != null ? rb.linearVelocity : Vector3.zero;
    }

    // 予測計算で使う重力加速度（y成分）。
    public static float Gravity => Physics.gravity.y;

    // ボールの速度を設定する（実 Rigidbody への書き込み）。
    public static void SetVelocity(Vector3 velocity)
    {
        Rigidbody rb = ResolveRigidbody();
        if (rb != null) rb.linearVelocity = velocity;
    }
}
