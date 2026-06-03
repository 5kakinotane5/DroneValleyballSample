/*
【MatchManager】
概要:
  現行システムの試合管理シングルトン。
  Team（Ally/Enemy）、GamePhase（Waiting/Receiving/Spiking）、
  サーブ権（serveRight）、ボール所持チーム（currentPossesion）、
  タッチ回数（touchCount）を一元管理する。

他スクリプトとの関係:
  ・SpikerAllyEnemyV2、SpikerAllyEnemy  ← フェーズ/チームを監視してスパイク開始を判断
  ・ReceiverAllyEnemy                    ← フェーズ/チームを監視してレシーブ開始を判断
  ・ServeDrone、AllyEnemyballlauncher    ← サーブ権とフェーズを参照してサーブを制御
  ・BallResetOnCollision                 ← ラリー終了時にResetGame()を呼ぶ
  ・ScoreManager                         ← lastTeamToHit / serveRight を参照して得点を決定

注意:
  VolleyballManager（旧マネージャー）と役割が重複しているが、
  こちらが現行の主系統。Ally/Enemy のチーム概念と ScoreManager との
  連携があるため、VolleyballManager より機能が上位。
*/
using Unity.VisualScripting;
using UnityEngine;
public enum Team { Ally,Enemy }

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;
    public Team currentPossesion = Team.Ally;
    public int touchCount = 0;
    [HideInInspector] public Team lastTeamToHit = Team.Ally;
    public Team serveRight = Team.Ally;

    public enum GamePhase { Waiting,Receiving,Spiking}
    public GamePhase currentPhase;
    void Awake() => Instance=this;

    public void ChangePossesion(Team nextTeam)
    {
        currentPossesion=nextTeam;
        touchCount=0;
        currentPhase =GamePhase.Receiving;
        Debug.Log($"Turn Switched to :{nextTeam}");
        Debug.Log($"currentphase:{MatchManager.Instance.currentPhase},currentPossesion:{MatchManager.Instance.currentPossesion}");
  
    }
    public void StartPlay()
    {
        currentPhase=GamePhase.Receiving;
    }

    public void ResetGame()
    {
        currentPhase = GamePhase.Waiting;
        currentPossesion = serveRight;
        touchCount = 0;
        lastTeamToHit = serveRight;
    }
    /*
    public void OnResetButtonClicked()
    {
        currentPhase=GamePhase.Waiting;
        currentPossesion=Team.Ally;
        touchCount=0;
        ReceiverAllyEnemy[] receivers = FindObjectsByType<ReceiverAllyEnemy>(FindObjectsSortMode.None);
    }*/
}