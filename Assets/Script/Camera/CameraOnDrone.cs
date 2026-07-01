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

        // カメラの背後にある場合は線を描かない（可視化は前方のみ）。
        Vector3 screenPos = targetCamera.WorldToScreenPoint(ball.position);
        if (screenPos.z <= 0f) return;

        // このカメラからボールへの視線（Ray）を取得し、赤い線で表示する。
        // （Scene ビュー、および Game ビューの Gizmos を ON にすると表示される）
        Ray gaze = GetGaze();
        Debug.DrawLine(gaze.origin, gaze.origin + gaze.direction * screenPos.z, Color.red);
    }

    // このカメラからボールへの視線を返す.
    public Ray GetGaze()
    {
        if (targetCamera == null)
            throw new System.Exception("CameraOnDrone: targetCamera is null.");
        if (!EnsureBall())
            throw new System.Exception("CameraOnDrone: ball not found.");

        // ボールが映っているピクセル座標（原点は左下）を求め、
        // そのピクセルへ向かう視線（Ray）に変換する。
        Vector3 screenPos = targetCamera.WorldToScreenPoint(ball.position);
        return targetCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
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
