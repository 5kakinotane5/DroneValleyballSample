/*
【DroneAroundScan】
概要:
  OverlapSphere でドローン周囲の指定レイヤー上のボールを毎フレーム検出し、
  発見時にオブジェクト名と座標を Debug.Log に出力するデバッグ専用センサー。

他スクリプトとの関係:
  ・なし（完全独立・単体アタッチ用）

【注意 ─ 削除候補】
  AiDrone3dTargetTag が FindGameObjectWithTag でボールを検知する機能を実装済みで、
  ゲームロジックへの接続もない純粋なデバッグツール。
  センサー確認が済んだ後は削除してよい。
  DroneScan と役割が重複している。
*/
using UnityEngine;

public class DroneAroundScan: MonoBehaviour{
    public float detectionRadius=3f;
    public LayerMask ballLayer;

    void Update(){
        Collider[] hits=Physics.OverlapSphere(transform.position,3f,ballLayer);
        if(hits.Length>0)
        {
            GameObject ball=hits[0].gameObject;
            Debug.Log($"ドローンの近くに球を発見！名前:{ball.name}");
            Debug.Log($"球の座標{ball.transform.position}で発見");
        }
    }
}