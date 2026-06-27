// ボールのワールド座標がカメラ映像のどのピクセル位置に映っているかを可視化するスクリプト。
// 計算したスクリーン座標が正しいかをデバッグ目視確認するため、その位置に黄色い点を描画する。
// ドローンにつけられたカメラオブジェクトにアタッチする前提のスクリプトである。

using UnityEngine;

public class CameraOnDrone : MonoBehaviour
{
    private Camera targetCamera;

    private Transform ball;

    private const float pointSize = 5f;


    // 黄色い点を描画するための 1x1 テクスチャ（キャッシュ）。
    private Texture2D dotTexture;

    void Awake()
    {
        // カメラの取得：アタッチされたオブジェクトの Camera コンポーネント
        targetCamera = GetComponent<Camera>() ?? throw new System.Exception("CameraOnDrone: Camera component not found on this GameObject.");

        // 黄色 1x1 テクスチャを生成。
        dotTexture = new Texture2D(1, 1);
        dotTexture.SetPixel(0, 0, Color.yellow);
        dotTexture.Apply();
    }

    void Update()
    {
        if (targetCamera == null || !EnsureBall()) return;

        // ボールが映っているピクセル座標を求める（原点は左下、z はカメラからの前後距離）。
        Vector3 screenPos = targetCamera.WorldToScreenPoint(ball.position);
        if (screenPos.z <= 0f) return; // カメラの背後なら計算しない。

        // ピクセル座標（u, v）からカメラの視線ベクトル（単位ベクトル）を取得する。
        // ※のちの処理でこの「ピクセル座標 → 視線ベクトル」の変換が必要になるため、
        //   ball.position から直接ではなく ScreenPointToRay 経由で求めている。
        float u = screenPos.x;
        float v = screenPos.y;
        Ray ray = targetCamera.ScreenPointToRay(new Vector3(u, v, 0));
        Vector3 viewVector = ray.direction; // カメラからピクセルへ向かう単位視線ベクトル（ワールド座標系）

        // 視線ベクトルを赤い線で表示する（カメラ位置からボールまでの距離分だけ伸ばす）。
        // （Scene ビュー、および Game ビューの Gizmos を ON にすると表示される）
        float length = screenPos.z; // カメラからボールまでの前方距離
        Debug.DrawLine(ray.origin, ray.origin + viewVector * length, Color.red);
    }

    void OnGUI()
    {
        if (targetCamera == null || !EnsureBall()) return;

        // ワールド座標 → スクリーン座標（原点は左下、z はカメラからの前後距離）。
        Vector3 screenPos = targetCamera.WorldToScreenPoint(ball.position);

        // カメラの背後にある場合は描画しない。
        if (screenPos.z <= 0f) return;

        // OnGUI は左上原点なので y を反転する。
        float x = screenPos.x;
        float y = Screen.height - screenPos.y;

        // ボールのピクセル位置に黄色い点を描画。
        GUI.DrawTexture(
            new Rect(x - pointSize, y - pointSize, pointSize * 2f, pointSize * 2f),
            dotTexture);
    }

    // ボールは実行時に injectionball(Clone) として生成されるため、未取得・破棄済みの間は
    // 毎フレーム再取得を試みる。取得できていれば true を返す。
    private bool EnsureBall()
    {
        if (ball == null)
        {
            GameObject found = GameObject.FindWithTag("injectionball");
            if (found != null) ball = found.transform;
        }
        return ball != null;
    }
}
