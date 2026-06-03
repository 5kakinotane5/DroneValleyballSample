/*
【VolleyballManager】
概要:
  旧バージョンの試合フェーズ管理シングルトン。
  GamePhase（Waiting/Receiving/Spiking）のみを管理し、
  チームや得点の概念は持たない。

他スクリプトとの関係:
  ・ReceiverStatemanage  ← currentPhase を監視してレシーブ開始を判断（旧レシーバー）
  ・ManageReceiver       ← currentPhase を監視（旧レシーバー）
  ・SpikerStatemange     ← currentPhase を監視（旧スパイカー）
  ・SpikerStatemangeVS   ← currentPhase を監視（旧スパイカー）
  ・SpikerStatemanageX   ← currentPhase を監視（旧スパイカー）
  ・RandomBallLauncher   ← 発射時に StartPlay() を呼ぶ（旧ランチャー）

【注意 ─ 削除候補】
  現行システムは MatchManager に移行済み。このスクリプトと連携する
  ReceiverStatemanage / ManageReceiver / SpikerStatemange / SpikerStatemangeVS /
  SpikerStatemanageX / RandomBallLauncher はすべて旧系統であり、
  現行の AllyEnemy 系スクリプトからは参照されていない。
  新設計への移行後はこのファイルごと削除可能。
*/
using UnityEngine;
public enum GamePhase{Waiting,Receiving,Spiking};

public class VolleyballManager : MonoBehaviour
{
    public static VolleyballManager Instance;
    public GamePhase currentPhase=GamePhase.Waiting;

    void Awake(){ Instance = this; }

    public void StartPlay()
    {
        currentPhase=GamePhase.Receiving;
    }

    public void OnReceiveSuccess()
    {
        currentPhase=GamePhase.Spiking;
    }

    public void ResetPhase()
    {
        currentPhase=GamePhase.Waiting;
    }

}