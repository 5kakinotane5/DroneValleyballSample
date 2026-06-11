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
        // 画面上部に2本のゲージを左右対称に並べる
        // レイアウト: [Ally名 | ████ゲージ████ | 段階]  [段階 | ████ゲージ████ | Enemy名]
        float barW   = 260f;
        float barH   = 26f;
        float labelW = 60f;
        float stageW = 120f;
        float gap    = 20f;  // 中央の隙間
        float panelW = labelW + barW + stageW; // 1本分の幅 = 440

        float cx     = Screen.width / 2f;
        float panelY = 12f;

        float allyX  = cx - gap / 2f - panelW; // Ally パネル左端
        float enemyX = cx + gap / 2f;           // Enemy パネル左端

        // Ally
        StaminaSystem allySys = spiker != null ? spiker.Stamina : null;
        DrawStaminaBar(allyX, panelY, labelW, barW, barH, stageW,
            "Ally", allySys, allyTextTimer, leftAlign: true);

        // Enemy（未接続でも枠だけ表示して分かるようにする）
        StaminaSystem enemySys = enemySpiker != null ? enemySpiker.Stamina : null;
        DrawStaminaBar(enemyX, panelY, labelW, barW, barH, stageW,
            "Enemy", enemySys, enemyTextTimer, leftAlign: false);
    }

    // leftAlign=true  → [ラベル | ゲージ | 段階テキスト]  Ally用
    // leftAlign=false → [段階テキスト | ゲージ | ラベル]  Enemy用
    void DrawStaminaBar(float panelX, float panelY,
                        float labelW, float barW, float barH, float stageW,
                        string teamName, StaminaSystem sys, float textTimer,
                        bool leftAlign)
    {
        bool connected = sys != null;
        float ratio    = connected ? Mathf.Clamp01(sys.stamina / sys.maxStamina) : 0f;
        Color fillColor = connected ? StageColor(sys.CurrentStage) : new Color(0.4f, 0.4f, 0.4f);
        string stageLabel = connected ? sys.StageLabel : "未接続";

        float totalW = labelW + barW + stageW;
        float totalH = barH + 16f;

        // 外枠
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        GUI.Box(new Rect(panelX - 4, panelY - 4, totalW + 8, totalH + 8),
                "", new GUIStyle(GUI.skin.box));
        GUI.color = Color.white;

        float barX = leftAlign ? panelX + labelW : panelX + stageW;

        // ゲージ背景
        GUI.color = new Color(0.25f, 0.25f, 0.25f);
        GUI.DrawTexture(new Rect(barX, panelY + 5, barW, barH), Texture2D.whiteTexture);

        // ゲージ本体
        GUI.color = fillColor;
        if (ratio > 0f)
            GUI.DrawTexture(new Rect(barX, panelY + 5, barW * ratio, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ラベル
        var nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            alignment = leftAlign ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft
        };
        nameStyle.normal.textColor = connected ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        float nameX = leftAlign ? panelX : panelX + stageW + barW;
        GUI.Label(new Rect(nameX, panelY, labelW, totalH), teamName, nameStyle);

        // 段階テキスト
        int fontSize = connected && textTimer > 0f
            ? (int)Mathf.Lerp(18f, 30f, textTimer / stageTextDuration)
            : 18;
        var stageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize,
            fontStyle = connected && textTimer > 0f ? FontStyle.Bold : FontStyle.Normal,
            alignment = leftAlign ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight
        };
        stageStyle.normal.textColor = connected ? fillColor : new Color(0.5f, 0.5f, 0.5f);
        float stageX = leftAlign ? panelX + labelW + barW : panelX;
        GUI.Label(new Rect(stageX, panelY, stageW, totalH), stageLabel, stageStyle);
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
