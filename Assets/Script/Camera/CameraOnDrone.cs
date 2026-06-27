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

    void OnGUI()
    {
        if (targetCamera == null) return;

        // ボールは実行時に injectionball(Clone) として生成されるため、未取得・破棄済みの間は
        // 毎フレーム再取得を試みる（生成前に例外を投げると OnGUI が止まり描画されなくなる）。
        if (ball == null)
        {
            GameObject found = GameObject.FindWithTag("injectionball");
            if (found != null) ball = found.transform;
        }
        if (ball == null) return;

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
}
