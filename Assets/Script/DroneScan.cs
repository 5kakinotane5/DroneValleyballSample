/*
【DroneScan】
概要:
  SphereCast でドローン前方のボールを検出し、発見時に名前・座標を Debug.Log に出力する
  デバッグ専用センサー。OnDrawGizmos でシーンビューにスキャン範囲を可視化できる。

他スクリプトとの関係:
  ・なし（完全独立・単体アタッチ用）

【注意 ─ 削除候補】
  DroneAroundScan と同様に純粋なデバッグツールで、ゲームロジックへの接続がない。
  DroneAroundScan が OverlapSphere 方式、こちらが SphereCast 方式という違いのみで
  目的が重複している。センサー確認後は両方削除してよい。
*/
using UnityEngine;

public class DroneScan: MonoBehaviour{
    public float detectionRadius=0.5f;
    public float maxDistance=10f;
    public LayerMask BallLayer;

    void Update(){
        RaycastHit hit;
        if(Physics.SphereCast(transform.position,detectionRadius,transform.up,out hit,maxDistance,BallLayer))
        {
            Debug.Log($"球を発見！名前:{hit.collider.name}");
            Debug.Log($"球の座標{hit.point}で発見");
        }
    }

    void OnDrawGizmos(){
        Gizmos.color=new Color(0,1,0,0.3f);

        Vector3 start=transform.position;
        Vector3 direction=transform.forward;
        Vector3 end=start+direction*maxDistance;

        //scanの開始位置に球を表示
        Gizmos.DrawSphere(start,detectionRadius);
        Gizmos.DrawSphere(end,detectionRadius);
        Gizmos.color=Color.green;
        Gizmos.DrawLine(start,end);
    }
}