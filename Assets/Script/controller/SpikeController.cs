// SpikeDrone 用プレイヤー入力コントローラ（操作値を画面右側に表示）
using UnityEngine;
using UnityEngine.InputSystem;

public class SpikeController : MonoBehaviour
{
    [SerializeField] private SpikeDrone spiker;

    [Header("スタミナUI用（EnemySpikeDroneを設定）")]
    [SerializeField] private EnemySpikeDrone enemySpiker;

    // trueのときに、デバッグ用UIを表示する
    [SerializeField] private bool showDebugUI = false;

    private readonly float chargeRate = 20f;
    private readonly float courseRate = 1f;

    private float currentVelocity;
    private float currentCourse;

    // スタミナ段階変化テキスト演出
    private StaminaStage prevAllyStage = StaminaStage.Full;
    private StaminaStage prevEnemyStage = StaminaStage.Full;
    private float allyTextTimer = 0f;
    private float enemyTextTimer = 0f;
    private const float stageTextDuration = 1.5f;

    // タイミングUI
    private Texture2D ringTex;
    private bool kWasPressedLastFrame = false;

    // タイミング結果表示演出
    private TimingResult lastTimingResult = TimingResult.None;
    private float resultDisplayTimer = 0f;
    private const float resultDisplayDuration = 1.2f;

    // スタミナゲージ演出（JUST=光る, Timeout=震える）
    private float allyGaugeFlash = 0f;
    private float allyGaugeShake = 0f;

    // スタミナ増減フローティングテキスト
    private class StaminaDeltaEntry { public float value; public float timer; public bool isAlly; }
    private readonly System.Collections.Generic.List<StaminaDeltaEntry> staminaDeltaEntries = new();
    private const float deltaTextDuration = 1.5f;

    void Start()
    {
        if (spiker?.Stamina != null)
            spiker.Stamina.OnStaminaChanged += d => staminaDeltaEntries.Add(new StaminaDeltaEntry { value = d, timer = deltaTextDuration, isAlly = true });
        if (enemySpiker?.Stamina != null)
            enemySpiker.Stamina.OnStaminaChanged += d => staminaDeltaEntries.Add(new StaminaDeltaEntry { value = d, timer = deltaTextDuration, isAlly = false });
    }

    void Awake()
    {
        ringTex = MakeRingTexture(128, 0.78f);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        var kb = Keyboard.current;

        if (kb.aKey.isPressed)
            currentCourse = Mathf.Max(currentCourse - courseRate * Time.deltaTime, -1f);
        if (kb.dKey.isPressed)
            currentCourse = Mathf.Min(currentCourse + courseRate * Time.deltaTime, 1f);

        bool kIsPressed = kb.kKey.isPressed;
        bool kJustReleased = !kIsPressed && kWasPressedLastFrame;
        kWasPressedLastFrame = kIsPressed;

        // 離した瞬間に渡す充電値（else で 0 化される前の値を退避）
        float chargeBeforeUpdate = currentVelocity;
        if (kIsPressed)
            currentVelocity = Mathf.Min(
                currentVelocity + chargeRate * Time.deltaTime,
                spiker.CurrentMaxVelocity
            );
        else
            currentVelocity = 0f;

        spiker.inputCourse = currentCourse;
        spiker.inputVelocity = currentVelocity;

        // ── タイミングウィンドウ処理 ───────────────────────────────────
        var timing = spiker.TimingWindow;
        if (timing != null && timing.IsWindowOpen)
        {
            if (kIsPressed) timing.NotifyKPressed();

            TimingResult tickResult = timing.Tick(Time.deltaTime);
            if (tickResult != TimingResult.None)
            {
                OnTimingResult(tickResult);
            }
            else if (kJustReleased)
            {
                TimingResult releaseResult = timing.RegisterRelease();
                if (releaseResult != TimingResult.None)
                    OnTimingResult(releaseResult);

                // 離した瞬間にスパイク突進を要求（コース＝現在値、速度＝0化前の充電値）
                spiker.RequestStrike(currentCourse, chargeBeforeUpdate);
            }
        }

        if (resultDisplayTimer > 0f) resultDisplayTimer -= Time.deltaTime;
        if (allyGaugeFlash > 0f) allyGaugeFlash -= Time.deltaTime;
        if (allyGaugeShake > 0f) allyGaugeShake -= Time.deltaTime;

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

        for (int i = staminaDeltaEntries.Count - 1; i >= 0; i--)
        {
            staminaDeltaEntries[i].timer -= Time.deltaTime;
            if (staminaDeltaEntries[i].timer <= 0f) staminaDeltaEntries.RemoveAt(i);
        }
    }

    void OnGUI()
    {
        DrawStaminaUI();
        DrawStaminaDeltas();
        if (showDebugUI)
        {
            DrawSpikeControlUI();
        }
        DrawTimingCircles();
        DrawTimingResult();
        DrawChargeGauge();
        DrawCurrentCourse();
    }

    // ── スタミナ増減フローティングテキスト ──────────────────────────────

    void DrawStaminaDeltas()
    {
        if (staminaDeltaEntries.Count == 0) return;

        // ゲージと同じレイアウト定数
        float barW   = 260f;
        float labelW = 60f;
        float stageW = 120f;
        float gap    = 20f;
        float panelW = labelW + barW + stageW;
        float cx     = Screen.width / 2f;
        // ゲージ下端（panelY=12, barH=26, padding=5+8=13 → 約54px）の少し下に開始点
        float startY = 58f;

        float allyBarCenterX  = (cx - gap / 2f - panelW) + labelW + barW / 2f;
        float enemyBarCenterX = (cx + gap / 2f) + stageW + barW / 2f;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };

        foreach (var entry in staminaDeltaEntries)
        {
            float progress = 1f - (entry.timer / deltaTextDuration); // 0→1
            float alpha    = progress < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.7f) / 0.3f);
            float yOffset  = 35f * progress; // 下方向にドリフト

            bool positive = entry.value > 0f;
            string text   = positive ? $"+{entry.value:F0}" : $"{entry.value:F0}";
            Color color   = positive
                ? new Color(0.2f, 1f, 0.3f, alpha)
                : new Color(1f, 0.35f, 0.2f, alpha);

            style.fontSize = 20;
            style.normal.textColor = color;

            float x = entry.isAlly ? allyBarCenterX : enemyBarCenterX;
            GUI.Label(new Rect(x - 50f, startY + yOffset, 100f, 36f), text, style);
        }
    }

    // ── スタミナゲージUI ─────────────────────────────────────────────

    void DrawStaminaUI()
    {
        // 画面上部に2本のゲージを左右対称に並べる
        // レイアウト: [Ally名 | ████ゲージ████ | 段階]  [段階 | ████ゲージ████ | Enemy名]
        float barW = 260f;
        float barH = 26f;
        float labelW = 60f;
        float stageW = 120f;
        float gap = 20f;  // 中央の隙間
        float panelW = labelW + barW + stageW; // 1本分の幅 = 440

        float cx = Screen.width / 2f;
        float panelY = 12f;

        float allyX = cx - gap / 2f - panelW; // Ally パネル左端
        float enemyX = cx + gap / 2f;           // Enemy パネル左端

        // Ally
        StaminaSystem allySys = spiker != null ? spiker.Stamina : null;
        DrawStaminaBar(allyX, panelY, labelW, barW, barH, stageW,
            "Ally", allySys, allyTextTimer, leftAlign: true,
            gaugeFlash: allyGaugeFlash, gaugeShake: allyGaugeShake);

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
                        bool leftAlign,
                        float gaugeFlash = 0f, float gaugeShake = 0f)
    {
        bool connected = sys != null;
        float ratio = connected ? Mathf.Clamp01(sys.stamina / sys.maxStamina) : 0f;
        Color fillColor = connected ? StageColor(sys.CurrentStage) : new Color(0.4f, 0.4f, 0.4f);
        string stageLabel = connected ? sys.StageLabel : "未接続";

        // ゲージ演出: 光る（JUST）
        if (gaugeFlash > 0f)
        {
            float t = gaugeFlash / resultDisplayDuration;
            fillColor = Color.Lerp(fillColor, Color.white, t * 0.6f);
        }

        // ゲージ演出: 震える（Timeout）
        float shakeOffsetX = 0f;
        if (gaugeShake > 0f)
        {
            float t = gaugeShake / resultDisplayDuration;
            shakeOffsetX = Mathf.Sin(Time.realtimeSinceStartup * 40f) * 6f * t;
        }
        panelX += shakeOffsetX;

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
            fontSize = 18,
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
            fontSize = fontSize,
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
        float w = 560f;
        float x = Screen.width - w - 30f;
        float y = 20f;

        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 24 };
        var boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 24 };

        GUI.Box(new Rect(x - 10, y - 10, w + 20, lh * 5 + 30), "", boxStyle);

        // 状態
        string stateStr = spiker.isReady ? "★ 待機中" : "移動中...";
        GUI.Label(new Rect(x, y, w, lh), $"状態:  {stateStr}", labelStyle);
        y += lh;

        // トス品質
        string tossStr;
        switch (spiker.CurrentTossQuality)
        {
            case TossQuality.High: tossStr = "高トス（強打OK）"; break;
            case TossQuality.Medium: tossStr = "中トス（中程度まで）"; break;
            default: tossStr = "低トス（弱い球のみ）"; break;
        }
        GUI.Label(new Rect(x, y, w, lh), $"トス:  {tossStr}", labelStyle);
        y += lh;

        // コース
        string courseDir = currentCourse < -0.1f ? "◀ 左"
                         : currentCourse > 0.1f ? "右 ▶"
                         : "中央";
        GUI.Label(new Rect(x, y, w, lh),
            $"コース:  {currentCourse:F2}  {courseDir}", labelStyle);
        y += lh;

        // 速度チャージ
        float maxV = spiker.CurrentMaxVelocity;
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

    // ── タイミング演出 ───────────────────────────────────────────────

    void OnTimingResult(TimingResult result)
    {
        lastTimingResult = result;
        resultDisplayTimer = resultDisplayDuration;

        switch (result)
        {
            case TimingResult.Just:
                allyGaugeFlash = resultDisplayDuration;
                break;
            case TimingResult.Timeout:
                allyGaugeShake = resultDisplayDuration;
                break;
        }
    }

    /// <summary>Ally スパイク中のみ、ボールの周囲にタイミングウィンドウの収縮円を描画する</summary>
    void DrawTimingCircles()
    {
        if (spiker == null || Camera.main == null) return;

        var timing = spiker.TimingWindow;
        if (timing == null || !timing.IsWindowOpen) return;

        GameObject ball = GameObject.FindGameObjectWithTag(spiker.ballTag);
        if (ball == null) return;
        Vector3 sp = Camera.main.WorldToScreenPoint(ball.transform.position);
        if (sp.z < 0f) return;
        float cx = sp.x;
        float cy = Screen.height - sp.y;

        const float justR = 38f;
        const float goodR = 76f;
        const float startR = 140f;

        // 基準円
        DrawRingAt(cx, cy, goodR, new Color(1f, 0.9f, 0.1f, 0.45f));
        DrawRingAt(cx, cy, justR, new Color(0.2f, 1f, 0.3f, 0.6f));

        // 収縮するリング
        float movingR = Mathf.Lerp(startR, justR, timing.WindowProgress);
        Color movingColor = timing.WindowProgress >= timing.justZoneStart
            ? new Color(0.2f, 1f, 0.3f, 0.9f)
            : timing.WindowProgress >= timing.goodZoneStart
                ? new Color(1f, 0.9f, 0.1f, 0.9f)
                : new Color(1f, 0.3f, 0.2f, 0.85f);

        DrawRingAt(cx, cy, movingR, movingColor);
    }

    void DrawRingAt(float cx, float cy, float radius, Color color)
    {
        if (ringTex == null) return;
        float size = radius * 2f;
        GUI.color = color;
        GUI.DrawTexture(new Rect(cx - radius, cy - radius, size, size), ringTex);
        GUI.color = Color.white;
    }

    /// <summary>タイミング結果テキストを画面中央付近に表示</summary>
    void DrawTimingResult()
    {
        if (resultDisplayTimer <= 0f) return;

        float alpha = Mathf.Clamp01(resultDisplayTimer / resultDisplayDuration);
        string text;
        Color color;
        int fontSize;

        switch (lastTimingResult)
        {
            case TimingResult.Just:
                text = "JUST!!";
                color = new Color(0.2f, 1f, 0.3f, alpha);
                fontSize = 52;
                break;
            case TimingResult.Good:
                text = "GOOD";
                color = new Color(1f, 0.9f, 0.1f, alpha);
                fontSize = 38;
                break;
            case TimingResult.Timeout:
                text = "TIMEOUT...";
                color = new Color(1f, 0.3f, 0.2f, alpha);
                fontSize = 38;
                break;
            case TimingResult.Miss:
                text = "MISS";
                color = new Color(0.7f, 0.7f, 0.7f, alpha);
                fontSize = 32;
                break;
            default:
                return;
        }

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = color;
        float w = 300f, h = 80f;
        GUI.Label(new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - 80f, w, h), text, style);
    }

    // スパイクはKキーを押した時間が長ければ長いほど早くなる仕様である。
    // どれだけパワーをチャージできているかを可視化するため。
    // ゲージはプレイヤーが操作するスパイクドローンの下に表示する。
    void DrawChargeGauge()
    {
        if (spiker == null) throw new System.Exception("spiker is null");
        if (Camera.main == null) throw new System.Exception("Camera.main is null");
        if (gameObject == null) throw new System.Exception("gameObject is null");

        // GUIの描画のため、オブジェクトの座標をカメラからみたスクリーン座標に変換する。
        Vector3 sp = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        if (sp.z < 0f) return;

        // ゲージの中心座標とサイズ
        float gaugeCenterX = sp.x;
        float gaugeCenterY = Screen.height - sp.y;
        float gaugeWidth = 120f;
        float gaugeHeight = 12f;

        // チャージ率を計算（0.0～1.0）
        float chargeRatio = spiker.CurrentMaxVelocity > 0f
            ? Mathf.Clamp01(currentVelocity / spiker.CurrentMaxVelocity)
            : 0f;

        // ゲージの枠
        GUI.color = new Color(0.25f, 0.25f, 0.25f);
        GUI.DrawTexture(new Rect(gaugeCenterX - gaugeWidth / 2f, gaugeCenterY + 50f, gaugeWidth, gaugeHeight), Texture2D.whiteTexture);

        // ゲージ本体
        GUI.color = new Color(0.2f, 1f, 0.3f);
        if (chargeRatio > 0f)
            GUI.DrawTexture(new Rect(gaugeCenterX - gaugeWidth / 2f, gaugeCenterY + 50f, gaugeWidth * chargeRatio, gaugeHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // ドローンがスパイクを打つコースをプレイヤーにも分かるように表示する。
    // 打つコースがあまりにも明確すぎると、ゲーム性が損なわれるため、大まかな方向を示す。
    // その実現として、画面にレティクルを表示して、レティクルの位置がコースの方向を示すようにする。
    void DrawCurrentCourse()
    {
        if (spiker == null) throw new System.Exception("spiker is null");
        if (Camera.main == null) throw new System.Exception("Camera.main is null");

        // ドローンの位置をスクリーン座標に変換
        Vector3 sp = Camera.main.WorldToScreenPoint(spiker.transform.position);
        if (sp.z < 0f) return;

        // レティクルの中心座標とサイズ
        float centerX = sp.x;
        float centerY = Screen.height - sp.y;
        float size = 7f;

        // コースの方向に応じてレティクルの位置を決めるため
        float offsetX = currentCourse * 80f;
        float offsetY = -70f;

        // レティクルの描画
        // レティクルは赤色の円で表示
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(centerX + offsetX - size / 2f, centerY + offsetY - size / 2f, size, size), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    /// <summary>リング状テクスチャを生成する（Awake で一度だけ呼ぶ）</summary>
    static Texture2D MakeRingTexture(int size, float innerRatio)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float cx = size / 2f;
        float cy = size / 2f;
        float outerR = size / 2f;
        float innerR = outerR * innerRatio;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                pixels[y * size + x] = (d <= outerR && d >= innerR) ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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
