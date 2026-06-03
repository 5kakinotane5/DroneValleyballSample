using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SpikerMock spiker;

    // 要調整。ゲームとして自然かどうかをプレイしてみて判断する。
    private readonly float chargeRate = 20f; // velocity/秒
    private readonly float courseRate = 1f;  // course/秒

    private float currentVelocity;
    private float currentCourse;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (!spiker.isReady)
        {
            currentVelocity = 0f;
            currentCourse = 0f;
            return;
        }

        var kb = Keyboard.current;

        // A/D でコース調整. Aは左、Dは右であり、-1から1の範囲で変化させる. 
        if (kb.aKey.isPressed)
            currentCourse = Mathf.Max(currentCourse - courseRate * Time.deltaTime, -1f);
        if (kb.dKey.isPressed)
            currentCourse = Mathf.Min(currentCourse + courseRate * Time.deltaTime, 1f);

        // K: 押した瞬間にリセット → 押し続けて加算 → 離したらスパイク
        if (kb.kKey.wasPressedThisFrame)
            currentVelocity = 0f;

        if (kb.kKey.isPressed)
            currentVelocity = Mathf.Min(
                currentVelocity + chargeRate * Time.deltaTime,
                spiker.maxVelocity
            );

        if (kb.kKey.wasReleasedThisFrame)
        {
            spiker.Spike(currentCourse, currentVelocity);
            currentVelocity = 0f;
            currentCourse = 0f;
        }
    }
}
