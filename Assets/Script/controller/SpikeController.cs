// SpikeDrone 用プレイヤー入力コントローラ（操作値を画面右側に表示）
using UnityEngine;
using UnityEngine.InputSystem;

public class SpikeController : MonoBehaviour
{
    [SerializeField] private SpikeDrone spiker;

    [Header("スタミナUI用（EnemySpikeDroneを設定）")]
    [SerializeField] private EnemySpikeDrone enemySpiker;

    private readonly float chargeRate = 20f;
    private readonly float courseRate = 1f;

    private float currentVelocity;
    private float currentCourse;

    // スタミナ段階変化テキスト演出
    private StaminaStage prevAllyStage   = StaminaStage.Full;
    private StaminaStage prevEnemyStage  = StaminaStage.Full;
    private float        allyTextTimer   = 0f;
    private float        enemyTextTimer  = 0f;
    private const float  stageTextDuration = 1.5f;

    void Update()
    {
        if (Keyboard.current == null) return;

        var kb = Keyboard.current;

        if (kb.aKey.isPressed)
            currentCourse = Mathf.Max(currentCourse - courseRate * Time.deltaTime, -1f);
        if (kb.dKey.isPressed)
            currentCourse = Mathf.Min(currentCourse + courseRate * Time.deltaTime,  1f);

        if (kb.kKey.isPressed)
            currentVelocity = Mathf.Min(
                currentVelocity + chargeRate * Time.deltaTime,
                spiker.CurrentMaxVelocity
            );
        else
            currentVelocity = 0f;

        spiker.inputCourse   = currentCourse;
        spiker.inputVelocity = currentVelocity;

        // 段階変化を検知してテキスト演出タイマーをセット
        if (spiker.Stamina != null)
        {
            if (spiker.Stamina.CurrentStage != prevAllyStage)
            {
                prevAllyStage = spiker.Stamina.CurrentStage;
                allyTextTimer = stageTextDuration;
            }
            if (allyTextTimer > 0f) allyTextTimer -= Time.deltaTime;
        }

        if (enemySpiker != null && enemySpiker.Stamina != null)
        {
            if (enemySpiker.Stamina.CurrentStage != prevEnemyStage)
            {
                prevEnemyStage = enemySpiker.Stamina.CurrentStage;
                enemyTextTimer = stageTextDuration;
            }
            if (enemyTextTimer > 0f) enemyTextTimer -= Time.deltaTime;
        }
    }

    void OnGUI()
    {
        DrawStaminaUI();
        DrawSpikeControlUI();
    }

    // ── スタミナゲージUI ─────────────────────────────────────────────

    void DrawStaminaUI()
    {
        float cx     = Screen.width / 2f;
        float barW   = 280f;
        float barH   = 26f;
        float panelY = 12f;

        // Ally ゲージ（中央左）
        if (spiker.Stamina != null)
            DrawStaminaBar(cx - 400f, panelY, barW, barH,
                "Ally", spiker.Stamina, allyTextTimer, leftAlign: true);

        // Enemy ゲージ（中央右）
        if (enemySpiker != null && enemySpiker.Stamina != null)
            DrawStaminaBar(cx + 120f, panelY, barW, barH,
                "Enemy", enemySpiker.Stamina, enemyTextTimer, leftAlign: false);
    }

    void DrawStaminaBar(float x, float y, float barW, float barH,
                        string teamName, StaminaSystem sys,
                        float textTimer, bool leftAlign)
    {
        float ratio     = Mathf.Clamp01(sys.stamina / sys.maxStamina);
        Color fillColor = StageColor(sys.CurrentStage);

        // 背景ボックス
        var bgStyle = new GUIStyle(GUI.skin.box);
        GUI.color   = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        GUI.Box(new Rect(x - 8, y - 6, barW + 120f, barH + 20f), "", bgStyle);
        GUI.color   = Color.white;

        // ゲージ背景（グレー）
        GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        GUI.DrawTexture(new Rect(x, y + 4, barW, barH), Texture2D.whiteTexture);

        // ゲージ本体
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, y + 4, barW * ratio, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // チーム名ラベル
        var nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize   = 18,
            fontStyle  = FontStyle.Bold,
            alignment  = leftAlign ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft
        };
        nameStyle.normal.textColor = Color.white;
        float nameX = leftAlign ? x - 68f : x + barW + 8f;
        GUI.Label(new Rect(nameX, y, 62f, barH + 8f), teamName, nameStyle);

        // 段階テキスト（変化時に大きくなる）
        int fontSize = textTimer > 0f
            ? (int)Mathf.Lerp(18f, 30f, textTimer / stageTextDuration)
            : 18;
        var stageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize,
            fontStyle = textTimer > 0f ? FontStyle.Bold : FontStyle.Normal,
            alignment = leftAlign ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight
        };
        stageStyle.normal.textColor = fillColor;
        float stageX = leftAlign ? x + barW + 6f : x - 118f;
        GUI.Label(new Rect(stageX, y, 112f, barH + 8f), sys.StageLabel, stageStyle);
    }

    // ── スパイク操作UI ─────────────────────────────────────────────

    void DrawSpikeControlUI()
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

    // ── ヘルパー ────────────────────────────────────────────────────

    static Color StageColor(StaminaStage stage)
    {
        switch (stage)
        {
            case StaminaStage.Full:
                return new Color(0.3f, 0.6f, 1f);  // 青
            case StaminaStage.Normal:
                return new Color(0.2f, 0.9f, 0.3f); // 緑
            case StaminaStage.Low:
                return Color.yellow;
            case StaminaStage.Exhausted:
                // 赤点滅
                return (Mathf.Sin(Time.realtimeSinceStartup * 6f) > 0f)
                    ? Color.red
                    : new Color(0.5f, 0f, 0f);
            default:
                return Color.white;
        }
    }
}
