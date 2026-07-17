// ボールのワールド座標がカメラ映像のどのピクセル位置に映っているかを可視化するスクリプト。
// 計算したスクリーン座標が正しいかをデバッグ目視確認するため、その位置に黄色い点を描画する。
// ドローンにつけられたカメラオブジェクトにアタッチする前提のスクリプトである。
//
// 点は専用の Canvas 上の Image として描画する。OnGUI(IMGUI) は Display 1 にしか
// 描画できないため、カメラごとに別のディスプレイへ点を出すには Canvas を使う必要がある。

using UnityEngine;

public class CameraOnDrone : MonoBehaviour
{
    private Camera targetCamera;

    private Transform ball;

    private const float pointSize = 5f;


    // 点の描画先。専用 Canvas を実行時に生成して保持する。
    private Canvas dotCanvas;
    private RectTransform dotRect;

    void Awake()
    {
        // カメラの取得：アタッチされたオブジェクトの Camera コンポーネント
        targetCamera = GetComponent<Camera>() ?? throw new System.Exception("CameraOnDrone: Camera component not found on this GameObject.");

        SetupDot();
    }

    // 点を描画するための Canvas と Image を生成する。
    // このスクリプトは同一プレハブの複数インスタンスから使われるため、Inspector で
    // Canvas を個別に割り当てることができない。よってインスタンスごとに実行時生成する。
    private void SetupDot()
    {
        var canvasObj = new GameObject($"BallDotCanvas ({name})");
        dotCanvas = canvasObj.AddComponent<Canvas>();
        dotCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 点をこのカメラの映像と同じディスプレイに出す。
        dotCanvas.targetDisplay = targetCamera.targetDisplay;

        // スクリーン座標をそのまま anchoredPosition に使うため、ConstantPixelSize（デフォルト）のままにする。
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        var dotObj = new GameObject("BallDot");
        dotObj.transform.SetParent(canvasObj.transform, false);
        var image = dotObj.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.yellow;
        image.raycastTarget = false;

        // アンカーと pivot を左下に揃え、WorldToScreenPoint と原点を一致させる。
        dotRect = image.GetComponent<RectTransform>();
        dotRect.anchorMin = Vector2.zero;
        dotRect.anchorMax = Vector2.zero;
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = new Vector2(pointSize * 2f, pointSize * 2f);
        dotObj.SetActive(false);
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

    // 点の位置を更新する。カメラは Update で動くため、その後の LateUpdate で反映する。
    void LateUpdate()
    {
        if (dotRect == null) return;

        if (targetCamera == null || !EnsureBall())
        {
            dotRect.gameObject.SetActive(false);
            return;
        }

        // ワールド座標 → スクリーン座標（原点は左下、z はカメラからの前後距離）。
        Vector3 screenPos = targetCamera.WorldToScreenPoint(ball.position);

        // カメラの背後にある場合は描画しない。
        if (screenPos.z <= 0f)
        {
            dotRect.gameObject.SetActive(false);
            return;
        }

        // Canvas は Overlay かつ ConstantPixelSize、アンカーは左下なので
        // スクリーン座標をそのまま渡せる（y の反転は不要）。
        dotRect.gameObject.SetActive(true);
        dotRect.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
    }

    // Canvas はこのオブジェクトの子ではなくルートに生成されるため、明示的に破棄する。
    void OnDestroy()
    {
        if (dotCanvas != null) Destroy(dotCanvas.gameObject);
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
