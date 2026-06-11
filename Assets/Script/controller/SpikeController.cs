// SpikeDrone 用プレイヤー入力コントローラ（操作値を画面右側に2倍サイズで表示）
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SpikeDrone に対してプレイヤー入力を橋渡しするコントローラ。
/// K キーを押さなくても drone は自動でスパイクする（最低速度で）。
///
/// 操作:
///   A / D     : コース（左右の打ち分け）を -1〜1 の範囲で調整
///   K 長押し  : 速度をチャージ（長押し = 深い球、短押し相当 = 浅い球）
///               離すと 0 にリセット（drone 側で minVelocity に補完される）
///               低トス時は CurrentMaxVelocity が自動で下がり弱い球のみ出せる
/// </summary>
public class SpikeController : MonoBehaviour
{
    [SerializeField] private SpikeDrone spiker;

    // 要調整。ゲームとして自然かどうかをプレイしてみて判断する。
    private readonly float chargeRate = 20f; // velocity/秒
    private readonly float courseRate = 1f;  // course/秒

    private float currentVelocity;
    private float currentCourse;

    void Update()
    {
        if (Keyboard.current == null) return;

        var kb = Keyboard.current;

        // A/D でコース調整（isReady に関わらず常に受け付ける）
        if (kb.aKey.isPressed)
            currentCourse = Mathf.Max(currentCourse - courseRate * Time.deltaTime, -1f);
        if (kb.dKey.isPressed)
            currentCourse = Mathf.Min(currentCourse + courseRate * Time.deltaTime,  1f);

        // K 長押しで速度チャージ、離すと 0 にリセット
        if (kb.kKey.isPressed)
            currentVelocity = Mathf.Min(
                currentVelocity + chargeRate * Time.deltaTime,
                spiker.CurrentMaxVelocity
            );
        else
            currentVelocity = 0f;

        // 毎フレーム drone に入力値を渡す（drone 側で自動スパイクに使用）
        spiker.inputCourse   = currentCourse;
        spiker.inputVelocity = currentVelocity;
    }

    void OnGUI()
    {
        float lh = 48f;
        float w  = 560f;
        float x  = Screen.width - w - 30f;
        float y  = 20f;

        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 24 };
        var boxStyle   = new GUIStyle(GUI.skin.box)   { fontSize = 24 };

        GUI.Box(new Rect(x - 10, y - 10, w + 20, lh * 5 + 30), "", boxStyle);

        // 状態
        string stateStr = spiker.isReady ? "★ 待機中" : "移動中...";
        GUI.Label(new Rect(x, y, w, lh), $"状態:  {stateStr}", labelStyle);
        y += lh;

        // トス品質
        string tossStr;
        switch (spiker.CurrentTossQuality)
        {
            case TossQuality.High:   tossStr = "高トス（強打OK）";     break;
            case TossQuality.Medium: tossStr = "中トス（中程度まで）"; break;
            default:                 tossStr = "低トス（弱い球のみ）"; break;
        }
        GUI.Label(new Rect(x, y, w, lh), $"トス:  {tossStr}", labelStyle);
        y += lh;

        // コース
        string courseDir = currentCourse < -0.1f ? "◀ 左"
                         : currentCourse >  0.1f ? "右 ▶"
                         : "中央";
        GUI.Label(new Rect(x, y, w, lh),
            $"コース:  {currentCourse:F2}  {courseDir}", labelStyle);
        y += lh;

        // 速度チャージ
        float maxV  = spiker.CurrentMaxVelocity;
        float ratio = maxV > 0f ? currentVelocity / maxV : 0f;
        string velStr = currentVelocity > 0f
            ? $"{currentVelocity:F1} / {maxV:F1}  ({ratio * 100f:F0}%)"
            : $"なし → 最低速度 ({spiker.minVelocity:F1}) で自動実行";
        GUI.Label(new Rect(x, y, w, lh), $"速度チャージ:  {velStr}", labelStyle);
        y += lh;

        // 操作ガイド
        GUI.Label(new Rect(x, y, w, lh),
            "A/D: コース　K長押し: 強い球チャージ", labelStyle);
    }
}
