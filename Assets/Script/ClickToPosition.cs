/*
【ClickToPosition】
概要:
  マウス左クリックでレイキャストを行い、クリックした位置の 3D 座標を
  Debug.Log に出力するデバッグ専用ユーティリティ。
  「ドローンをそこへ向かわせる処理がここに書ける」というコメントが残っており、
  機能の骨格のみ存在する。

他スクリプトとの関係:
  ・なし（完全独立・単体アタッチ用）

【注意 ─ 削除候補】
  ゲームロジックとの接続がなく、コート座標の確認にのみ使用するデバッグツール。
  座標確認が済んだ後は削除してよい。
*/
using UnityEngine;

public class ClickToPosition : MonoBehaviour
{
    void Update()
    {
        // マウスの左ボタン(0)が押された瞬間を判定
        if (Input.GetMouseButtonDown(0))
        {
            // 1. カメラからマウスの位置に向かう「光線（Ray）」を作る
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 2. その光線が何かに当たったか判定
            if (Physics.Raycast(ray, out hit))
            {
                // 当たった場所の座標をログに表示
                Vector3 targetPos = hit.point;
                Debug.Log($"クリックした座標: {targetPos}");

                // 【応用】そこにドローンを向かわせる、などの処理がここに書ける
            }
        }
    }
}