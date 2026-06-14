using UnityEngine;

public enum TimingResult { None, Just, Good, Miss, Timeout }

/// <summary>スパイクタイミングウィンドウを管理するコンポーネント（SpikeDrone にアタッチ）</summary>
public class TimingWindowSystem : MonoBehaviour
{
    [Header("ウィンドウ持続時間（トス別）")]
    public float highTossWindow = 1.0f;
    public float medTossWindow  = 0.5f;
    public float lowTossWindow  = 0.2f;

    [Header("ゾーン境界（ウィンドウ進行度 0→1）")]
    [Range(0f, 1f)] public float goodZoneStart = 0.55f;
    [Range(0f, 1f)] public float justZoneStart = 0.80f;

    // ── 読み取り専用 ─────────────────────────────────────────────────
    public bool         IsWindowOpen   { get; private set; }
    public float        WindowProgress { get; private set; }
    public TossQuality  WindowToss     { get; private set; }
    public TimingResult LastResult     { get; private set; } = TimingResult.None;

    private float windowDuration;
    private float elapsed;
    private bool  kPressedDuringWindow;

    // ── 外部 API ─────────────────────────────────────────────────────

    /// <summary>ボール検出時に呼ぶ。トス種別に応じたウィンドウを開始する。</summary>
    public void StartWindow(TossQuality toss)
    {
        WindowToss   = toss;
        windowDuration = toss == TossQuality.High   ? highTossWindow
                       : toss == TossQuality.Medium ? medTossWindow
                       : lowTossWindow;
        elapsed              = 0f;
        WindowProgress       = 0f;
        LastResult           = TimingResult.None;
        kPressedDuringWindow = false;
        IsWindowOpen         = true;
    }

    /// <summary>K キーが押されているフレームに毎フレーム呼ぶ。</summary>
    public void NotifyKPressed()
    {
        if (IsWindowOpen) kPressedDuringWindow = true;
    }

    /// <summary>
    /// ウィンドウが開いている間、毎フレーム呼ぶ。
    /// タイムアウトした場合のみ Timeout / Miss を返す。それ以外は None。
    /// </summary>
    public TimingResult Tick(float dt)
    {
        if (!IsWindowOpen) return TimingResult.None;

        elapsed       += dt;
        WindowProgress = Mathf.Clamp01(elapsed / windowDuration);

        if (elapsed >= windowDuration)
        {
            IsWindowOpen = false;
            // K を押していなかった場合はペナルティなし（Miss 扱い）
            LastResult = kPressedDuringWindow ? TimingResult.Timeout : TimingResult.Miss;
            return LastResult;
        }
        return TimingResult.None;
    }

    /// <summary>K キーが離されたフレームに呼ぶ。ウィンドウ外なら None を返す。</summary>
    public TimingResult RegisterRelease()
    {
        if (!IsWindowOpen) return TimingResult.None;

        IsWindowOpen = false;
        LastResult   = WindowProgress >= justZoneStart ? TimingResult.Just
                     : WindowProgress >= goodZoneStart ? TimingResult.Good
                     : TimingResult.Miss;
        return LastResult;
    }

    /// <summary>スパイク後に呼んでウィンドウ状態をリセットする。</summary>
    public void Reset()
    {
        IsWindowOpen         = false;
        WindowProgress       = 0f;
        LastResult           = TimingResult.None;
        kPressedDuringWindow = false;
    }
}
