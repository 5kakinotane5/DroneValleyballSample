// スパイカーのスタミナ管理コンポーネント（SpikeDrone にアタッチして使用）
using UnityEngine;

public enum StaminaStage { Full, Normal, Low, Exhausted }

public class StaminaSystem : MonoBehaviour
{
    [Header("スタミナ設定")]
    [Range(0f, 100f)] public float stamina = 100f;
    public float maxStamina = 100f;

    [Header("回復量（/秒）")]
    public float waitingRecoveryRate = 3f;
    public float movingRecoveryRate  = 0.5f;

    [Header("消費設定")]
    [Tooltip("SpikeController の chargeRate と合わせること（デフォルト 20）")]
    public float chargeRate  = 20f;
    [Tooltip("チャージ時間 × consumeRate がスタミナ消費量")]
    public float consumeRate = 15f;

    [Header("得点時回復量")]
    public float scoreBonus = 20f;

    [Header("ブレ量（ワールド単位、段階別）")]
    public float normalBlurRadius     = 3f;
    public float lowBlurRadius        = 6f;
    public float exhaustedBlurMin     = 4f;
    public float exhaustedBlurMax     = 12f;

    [Header("消費倍率（段階別、低スタミナほど消費増加）")]
    public float normalConsumeMult    = 1.5f;
    public float lowConsumeMult       = 2.0f;
    public float exhaustedConsumeMult = 3.0f;

    // ── 読み取り専用プロパティ ──────────────────────────────────────

    public StaminaStage CurrentStage     { get; private set; } = StaminaStage.Full;
    /// <summary>Exhausted 突入後、MAX 回復するまで true。この間は強制 Exhausted 固定。</summary>
    public bool         IsLockedExhausted { get; private set; } = false;

    /// <summary>段階に応じたスパイク速度の倍率（EXHAUSTEDのみ制限）</summary>
    public float SpeedMultiplier
    {
        get
        {
            switch (CurrentStage)
            {
                case StaminaStage.Full:      return 1.00f;
                case StaminaStage.Normal:    return 1.00f;
                case StaminaStage.Low:       return 1.00f;
                case StaminaStage.Exhausted: return 0.40f;
                default: return 1f;
            }
        }
    }

    /// <summary>段階に応じたスタミナ消費倍率（低スタミナほど消費増加）</summary>
    public float ConsumptionMultiplier
    {
        get
        {
            switch (CurrentStage)
            {
                case StaminaStage.Full:      return 1.00f;
                case StaminaStage.Normal:    return normalConsumeMult;
                case StaminaStage.Low:       return lowConsumeMult;
                case StaminaStage.Exhausted: return exhaustedConsumeMult;
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
            case StaminaStage.Exhausted: return Random.Range(exhaustedBlurMin, exhaustedBlurMax);
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
        if (IsLockedExhausted) return;
        float chargeTime = velocity / chargeRate;
        stamina = Mathf.Max(0f, stamina - chargeTime * consumeRate * ConsumptionMultiplier);
        RefreshStage();
    }

    /// <summary>SpikeDrone の FixedUpdate から毎フレーム呼ぶ</summary>
    public void RecoverTick(bool isWaiting)
    {
        float rate = isWaiting ? waitingRecoveryRate : movingRecoveryRate;
        if (IsLockedExhausted) rate *= 2f;
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
        // Exhausted ロック中: MAX に達するまで強制 Exhausted
        if (IsLockedExhausted)
        {
            CurrentStage = StaminaStage.Exhausted;
            if (stamina >= maxStamina)
                IsLockedExhausted = false;
            return;
        }

        if      (stamina >= 70f) CurrentStage = StaminaStage.Full;
        else if (stamina >= 40f) CurrentStage = StaminaStage.Normal;
        else if (stamina >= 10f) CurrentStage = StaminaStage.Low;
        else
        {
            CurrentStage      = StaminaStage.Exhausted;
            IsLockedExhausted = true;
        }
    }
}
