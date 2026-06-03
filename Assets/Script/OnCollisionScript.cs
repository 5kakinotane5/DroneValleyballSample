/*
【OnCollisionScript】
概要:
  衝突したとき Debug.Log("当たり") を出力するだけのデバッグ専用スクリプト。
  実機能はゼロ。

他スクリプトとの関係:
  ・なし

【注意 ─ 削除候補】
  OnCollision と内容が完全に同一（クラス名だけ異なる）。
  どちらか一方のみ残すか、デバッグ確認後は両方削除すること。
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCollisionScript: MonoBehaviour{
    void OnCollisionEnter(Collision collision){
        Debug.Log("当たり");
    }
}