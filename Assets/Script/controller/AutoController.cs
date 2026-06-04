using UnityEngine;

public class AutoController : MonoBehaviour
{
    [SerializeField] private SpikerMock spiker;

    // ゲームをプレイしてみて調整が必要。
    private readonly float chargeRate = 20f;
    private readonly float spikeDelay = 1f;

    private float elapsedTime = 0f;

    void Update()
    {
        if (!spiker.isReady)
        {
            elapsedTime = 0f;
            return;
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= spikeDelay)
        {
            float course = Random.Range(-1f, 1f);
            float maxVelocity = Mathf.Min(elapsedTime * chargeRate, spiker.maxVelocity);
            float spikeVelocity = Random.Range(0f, maxVelocity);
            spiker.Spike(course, spikeVelocity);
        }
    }
}
