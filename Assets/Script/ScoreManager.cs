/*
【ScoreManager】
概要:
  得失点の管理と UI 表示を担当するシングルトン。
  ボールの着弾位置と最後に打ったチームをもとに得点チームを自動判定し、
  スコアテキスト・結果テキスト（IN/OUT）を TextMeshPro で表示する。
  UI が Inspector 未設定の場合は Canvas と TMP テキストを自動生成する。

他スクリプトとの関係:
  ・BallResetOnCollision  ← DetermineScore(ballPosition) を呼ばれる
  ・MatchManager          ← DetermineScore 内で lastTeamToHit を参照、
                            AddPoint 内で serveRight を更新する

注意:
  MatchManager と密接に連携しているため、VolleyballManager 系の旧シーンには
  そのままでは対応しない。
*/
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// バレーボール得失点管理 + UI表示
/// シーン内の任意のGameObjectにアタッチしてください。
/// Canvas / TextはInspectorで設定しなければ自動生成されます。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("スコア")]
    public int allyScore = 0;
    public int enemyScore = 0;

    [Header("UI参照 (未設定時は自動生成)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;

    [Header("結果表示時間(秒)")]
    public float resultDisplayDuration = 2f;

    [Header("コート境界 (ReceiverAllyEnemyと合わせること)")]
    public float courtHalfX = 21f;
    public float courtHalfZ = 10f;

    private Coroutine resultCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupUI();
    }

    // ─────────────────────────────────────────
    // 得点判定 (BallResetOnCollision から呼ぶ)
    // ─────────────────────────────────────────

    /// <summary>
    /// ボールが着弾した位置と、最後に打ったチームをもとに得点を決定する。
    /// inBounds かつ X &lt;= 0 → Enemy コート → Ally得点 "in"
    /// inBounds かつ X &gt; 0  → Ally コート → Enemy得点 "in"
    /// out of bounds           → 最後に打ったチームの相手が得点 "out"
    /// </summary>
    public void DetermineScore(Vector3 ballPosition)
    {
        bool inBounds = Mathf.Abs(ballPosition.x) <= courtHalfX &&
                        Mathf.Abs(ballPosition.z) <= courtHalfZ;

        if (inBounds)
        {
            if (ballPosition.x <= 0f)
                AddPoint(Team.Ally, "in");    // Enemyコートに着弾
            else
                AddPoint(Team.Enemy, "in");   // Allyコートに着弾
        }
        else
        {
            Team lastHitter = MatchManager.Instance != null
                ? MatchManager.Instance.lastTeamToHit
                : Team.Ally;
            Team winner = lastHitter == Team.Ally ? Team.Enemy : Team.Ally;
            AddPoint(winner, "out");
        }
    }

    // ─────────────────────────────────────────
    // スコア加算 & UI更新
    // ─────────────────────────────────────────

    public void AddPoint(Team scoringTeam, string reason)
    {
        if (scoringTeam == Team.Ally)
            allyScore++;
        else
            enemyScore++;

        if (MatchManager.Instance != null)
            MatchManager.Instance.serveRight = scoringTeam;

        UpdateScoreUI();
        ShowResult(reason);
        Debug.Log($"[Score] {scoringTeam} scored ({reason}) | Ally {allyScore} - {enemyScore} Enemy");
    }

    public void ResetScore()
    {
        allyScore = 0;
        enemyScore = 0;
        UpdateScoreUI();
    }

    // ─────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Ally  {allyScore} : {enemyScore}  Enemy";
    }

    void ShowResult(string message)
    {
        if (resultCoroutine != null) StopCoroutine(resultCoroutine);
        resultCoroutine = StartCoroutine(DisplayResult(message.ToUpper()));
    }

    IEnumerator DisplayResult(string message)
    {
        if (resultText == null) yield break;
        resultText.text = message;
        resultText.gameObject.SetActive(true);
        yield return new WaitForSeconds(resultDisplayDuration);
        resultText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────
    // Canvas / Text 自動生成
    // ─────────────────────────────────────────

    void SetupUI()
    {
        // 既存Canvasのモード・スケール設定に依存しないよう、スコア専用Canvasを必ず新規作成する
        var go = new GameObject("ScoreCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<UnityEngine.UI.CanvasScaler>(); // ConstantPixelSize（デフォルト）
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        if (scoreText == null)
            scoreText = CreateTMP(canvas, "ScoreText",
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1),
                pivot: new Vector2(0, 1),
                anchoredPos: new Vector2(20, -20), size: new Vector2(520, 80),
                fontSize: 36, align: TextAlignmentOptions.TopLeft, color: Color.white);

        if (resultText == null)
        {
            resultText = CreateTMP(canvas, "ResultText",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPos: Vector2.zero, size: new Vector2(600, 160),
                fontSize: 80, align: TextAlignmentOptions.Center, color: Color.yellow);
            resultText.fontStyle = FontStyles.Bold;
            resultText.gameObject.SetActive(false);
        }

        UpdateScoreUI();
    }

    TextMeshProUGUI CreateTMP(Canvas canvas, string objName,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size, float fontSize,
        TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(canvas.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        var rt = tmp.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return tmp;
    }
}
