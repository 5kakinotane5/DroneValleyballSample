using UnityEngine;

// ボールがドローン・ネット以外に衝突したとき、ボールを消去して試合をリセットする
// ボールのPrefabにアタッチして使用
public class BallResetOnCollision : MonoBehaviour
{
    [Header("衝突しても消えないタグ（ネット・ドローンなど）")]
    public string[] ignoreTags = { "Net", "Drone" };

    private bool hasReset = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasReset) return;

        GameObject other = collision.gameObject;

        foreach (var tag in ignoreTags)
        {
            if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag)) return;
        }

        hasReset = true;
        DetermineScore();
        ResetAll();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (!hasReset && MatchManager.Instance != null)
        {
            hasReset = true;
            ResetAll();
        }
    }

    void DetermineScore()
    {
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.DetermineScore(transform.position);
    }

    void ResetAll()
    {
        if (MatchManager.Instance != null)
            MatchManager.Instance.ResetGame();

        foreach (var s in FindObjectsByType<SpikerAllyEnemyV2>(FindObjectsSortMode.None))
            s.ResetToInitialState();

        foreach (var s in FindObjectsByType<SpikerAllyEnemy>(FindObjectsSortMode.None))
            s.ResetToInitialState();

        foreach (var r in FindObjectsByType<ReceiverAllyEnemy>(FindObjectsSortMode.None))
            r.ResetToInitialState();
    }
}
