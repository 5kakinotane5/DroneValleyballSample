using UnityEngine;
using System.Collections;

public enum GamePhase { Serving, Rally, PointScored, GameOver }
public enum BallPossession { TeamA, TeamB }

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;

    // --- 状態管理 ---
    public GamePhase CurrentPhase { get; private set; }
    public BallPossession CurrentPossession { get; private set; }
    public int TeamAScore { get; private set; }
    public int TeamBScore { get; private set; }

    // --- 参照 ---
    [SerializeField] private TeamController teamA;
    [SerializeField] private TeamController teamB;
    [SerializeField] private Ball ball;

    // --- ルール設定 ---
    private int touchCount; 
    private const int MaxTouches = 3;
    [SerializeField] private int winningScore = 15;

    void Awake() => Instance = this;

    void Start() => StartMatch();

    public void StartMatch()
    {
        TeamAScore = 0;
        TeamBScore = 0;
        CurrentPossession = BallPossession.TeamA;
        StartServe();
    }

    private void StartServe()
    {
        CurrentPhase = GamePhase.Serving;
        touchCount = 0;
        
        // ボールをサーバーの位置へリセットする処理などをここに入れる
        ball.ResetPosition(CurrentPossession == BallPossession.TeamA);
        
        Debug.Log($"<color=yellow>Serve Start: {CurrentPossession}</color>");
        GetCurrentTeam().Serve(ball);
    }

    // ドローンがボールに触れた時に各ドローンスクリプトから呼ぶ
    public void OnBallTouched(BallPossession byTeam)
    {
        if (CurrentPhase == GamePhase.PointScored) return;

        if (byTeam == CurrentPossession)
        {
            touchCount++;
            Debug.Log($"{byTeam} Touch: {touchCount}");

            if (touchCount > MaxTouches)
            {
                Debug.Log("Over Touch!");
                HandleScore(byTeam == BallPossession.TeamA ? BallPossession.TeamB : BallPossession.TeamA);
                return;
            }
            
            // フェーズをラリー中に更新
            if (CurrentPhase == GamePhase.Serving) CurrentPhase = GamePhase.Rally;
        }
        else
        {
            // 相手が触ったのでターン交代
            CurrentPossession = byTeam;
            touchCount = 1;
            Debug.Log($"Possession Changed: {CurrentPossession}");
        }
    }

    // ボールが地面や場外に触れた時にボールスクリプトから呼ぶ
    public void OnBallLanded(Vector3 landPoint, bool isOut)
    {
        if (CurrentPhase == GamePhase.PointScored) return;

        // 簡易的なコート判定ロジック（X座標で判定する例）
        bool inTeamACourt = landPoint.x < 0;

        if (isOut)
        {
            // アウトなら、最後に触ったチームの反対側に点が入る
            HandleScore(CurrentPossession == BallPossession.TeamA ? BallPossession.TeamB : BallPossession.TeamA);
        }
        else
        {
            // インなら、落ちたコートの反対側に点が入る
            HandleScore(inTeamACourt ? BallPossession.TeamB : BallPossession.TeamA);
        }
    }

    private void HandleScore(BallPossession winner)
    {
        CurrentPhase = GamePhase.PointScored;
        
        if (winner == BallPossession.TeamA) TeamAScore++;
        else TeamBScore++;

        Debug.Log($"<color=cyan>Score! A:{TeamAScore} - B:{TeamBScore}</color>");

        if (TeamAScore >= winningScore || TeamBScore >= winningScore)
        {
            CurrentPhase = GamePhase.GameOver;
            Debug.Log("Game Over!");
        }
        else
        {
            // 次のサーブ権は得点したチーム（バレーの基本ルール）
            CurrentPossession = winner;
            Invoke(nameof(StartServe), 2.0f);
        }
    }

    private TeamController GetCurrentTeam() 
        => CurrentPossession == BallPossession.TeamA ? teamA : teamB;
}