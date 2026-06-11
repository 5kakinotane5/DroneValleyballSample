// スパイカーのスタミナ管理コンポーネント（SpikeDrone にアタッチして使用）
using UnityEngine;

public enum StaminaStage { Full, Normal, Low, Exhausted }

public class StaminaSystem : MonoBehaviour
{
    [Header("スタミナ設定")]
    [Range(0f, 100f)] public float stamina = 100f;
    public float maxStamina = 100f;

    [Header("回復量（/秒）")]
    public float waitingRecoveryRate = 8f;
    public float movingRecoveryRate  = 2f;

    [Header("消費設定")]
    [Tooltip("SpikeController の chargeRate と合わせること（デフォルト 20）")]
    public float chargeRate  = 20f;
    [Tooltip("チャージ時間 × consumeRate がスタミナ消費量")]
    public float consumeRate = 15f;

    [Header("得点時回復量")]
    public float scoreBonus = 20f;

    [Header("ブレ量（ワールド単位、段階別）")]
    public float normalBlurRadius    = 2f;
    public float lowBlurRadius       = 4f;
    public float exhaustedBlurMax    = 8f;

    // ── 読み取り専用プロパティ ──────────────────────────────────────

    public StaminaStage CurrentStage { get; private set; } = StaminaStage.Full;

    /// <summary>段階に応じたスパイク速度の倍率</summary>
    public float SpeedMultiplier
    {
        get
        {
            switch (CurrentStage)
            {
                case StaminaStage.Full:      return 1.00f;
                case StaminaStage.Normal:    return 0.75f;
                case StaminaStage.Low:       return 0.50f;
                case StaminaStage.Exhausted: return 0.20f;
                default: return 1f;
            }
        }
    }

    /// <summary>
    /// 目標座標に加えるブレ量を返す。
    /// Exhausted は呼ぶたびにランダム値になるためスパイク/トス時に1回だけ呼ぶこと。
    /// </summary>
    public float GetBlur()
    {
        switch (CurrentStage)
        {
            case StaminaStage.Full:      return 0f;
            case StaminaStage.Normal:    return normalBlurRadius;
            case StaminaStage.Low:       return lowBlurRadius;
            case StaminaStage.Exhausted: return Random.Range(0f, exhaustedBlurMax);
            default: return 0f;
        }
    }

    /// <summary>段階に対応するUI表示テキスト</summary>
    public string StageLabel
    {
        get
        {
            switch (CurrentStage)
            {
                case StaminaStage.Full:      return "FULL POWER!!";
                case StaminaStage.Normal:    return "GOOD";
                case StaminaStage.Low:       return "WEAK...";
                case StaminaStage.Exhausted: return "EXHAUSTED!";
                default: return "";
            }
        }
    }

    // ── 外部から呼ぶメソッド ────────────────────────────────────────

    /// <summary>チャージスパイク実行時に呼ぶ（velocity = pendingVelocity）</summary>
    public void ConsumeCharge(float velocity)
    {
        float chargeTime = velocity / chargeRate;
        stamina = Mathf.Max(0f, stamina - chargeTime * consumeRate);
        RefreshStage();
    }

    /// <summary>SpikeDrone の FixedUpdate から毎フレーム呼ぶ</summary>
    public void RecoverTick(bool isWaiting)
    {
        float rate = isWaiting ? waitingRecoveryRate : movingRecoveryRate;
        stamina = Mathf.Min(maxStamina, stamina + rate * Time.fixedDeltaTime);
        RefreshStage();
    }

    /// <summary>得点したチームのスパイカーに呼ぶ</summary>
    public void AddScoreBonus()
    {
        stamina = Mathf.Min(maxStamina, stamina + scoreBonus);
        RefreshStage();
    }

    // ── 内部 ──────────────────────────────────────────────────────

    void RefreshStage()
    {
        if      (stamina >= 70f) CurrentStage = StaminaStage.Full;
        else if (stamina >= 40f) CurrentStage = StaminaStage.Normal;
        else if (stamina >= 10f) CurrentStage = StaminaStage.Low;
        else                     CurrentStage = StaminaStage.Exhausted;
    }
}
