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