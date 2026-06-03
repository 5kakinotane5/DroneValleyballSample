/*
【BallResetOnCollision】
概要:
  現行システムでボールの Prefab にアタッチする中核スクリプト。
  ドローン・ネット以外のオブジェクトにボールが衝突したとき、
  得点判定 → ゲームリセット → 自己削除 を一括で行う。

他スクリプトとの関係:
  ・ScoreManager          ← DetermineScore(position) を呼んで得点を確定させる
  ・MatchManager          ← ResetGame() を呼んでフェーズとサーブ権をリセット
  ・SpikerAllyEnemyV2     ← ResetToInitialState() を呼んで初期位置に戻す
  ・ReceiverAllyEnemy     ← ResetToInitialState() を呼んで初期位置に戻す

注意:
  このスクリプト 1 つが得点・リセット・各ドローンへの通知をまとめて担う。
  BallDestruction や DestoryOnCollision と削除タイミングが重複する場合があるため、
  Prefab に複数の削除スクリプトを同時にアタッチしないよう注意。
*/
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
            if (string.IsNullOrEmpty(tag)) continue;
            // 衝突相手本体、または親階層にタグが設定されている場合は無視
            if (other.CompareTag(tag) || other.transform.root.gameObject.CompareTag(tag)) return;
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

        foreach (var r in FindObjectsByType<ReceiverAllyEnemy>(FindObjectsSortMode.None))
            r.ResetToInitialState();
    }
}
