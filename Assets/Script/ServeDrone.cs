// ネット越え弾道計算で自動サーブを行うドローン
/*
【ServeDrone】
概要:
  自動サーブを担当するドローン。MatchManager のサーブ権・フェーズを監視し、
  自チームのサーブ権があるとき Space キーでサーブシーケンスを開始する。
  コート外のサーブ位置に移動 → ネットを越える弾道を計算 → ボールを生成・発射
  → スパイク位置に戻る という流れをコルーチンで実行する。

他スクリプトとの関係:
  ・MatchManager          ← serveRight / currentPhase を参照して発動判断
                            発射後に ChangePossesion() を呼んで受け側に切り替え
  ・SpikerAllyEnemyV2     ← Start() 時に同 GameObject の myTeam / initialPos を取得

注意:
  AllyEnemyballlauncher（手動サーブ）と役割が重複するが、
  ServeDrone は自動でコート外に移動してから発射する点で機能が上位。
  どちらか一方を選択して使用すること。
*/
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class ServeDrone : MonoBehaviour
{
    public GameObject ballPrefab;

    [Header("サーブするチーム（SpikerAllyEnemyV2があれば自動設定）")]
    public Team serveTeam = Team.Ally;

    [Header("サーブ位置（自コート外）")]
    public Vector3 allyServePosition  = new Vector3(23f, 1f, 0f);
    public Vector3 enemyServePosition = new Vector3(-23f, 1f, 0f);

    [Header("サーブ先 X 範囲（相手コート）")]
    public float allyTargetMinX  = -20f;
    public float allyTargetMaxX  = -2f;
    public float enemyTargetMinX = 2f;
    public float enemyTargetMaxX = 20f;

    [Header("サーブ先 Z 範囲（共通）")]
    public float targetMinZ = -9f;
    public float targetMaxZ = 9f;

    [Header("ネット設定")]
    public float netX         = 0f;
    public float netHeight    = 6.0f;
    public float netClearance = 0.5f;

    [Header("移動時間（秒）")]
    public float moveToServeTime = 1.5f;
    public float moveToSpikeTime = 2.0f;

    [Header("飛行時間の探索範囲（短いほど速い弾道）")]
    public float minFlightTime  = 1.0f;
    public float maxFlightTime  = 6.0f;
    public float flightTimeStep = 0.05f;

    private Vector3 spikePosition;
    private bool isServing = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        SpikerAllyEnemyV2 spiker = GetComponent<SpikerAllyEnemyV2>();
        if (spiker != null)
        {
            serveTeam     = spiker.MyTeam;
            spikePosition = spiker.initialPos;
        }
        else
        {
            spikePosition = transform.position;
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        bool isMyServe = MatchManager.Instance != null &&
            MatchManager.Instance.serveRight   == serveTeam &&
            MatchManager.Instance.currentPhase == MatchManager.GamePhase.Waiting;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isServing && isMyServe)
            StartCoroutine(ServeSequence());
    }

    IEnumerator ServeSequence()
    {
        isServing = true;
        if (rb != null) rb.isKinematic = true;

        // 1. コート外のサーブ位置へ移動
        Vector3 servePos = serveTeam == Team.Ally ? allyServePosition : enemyServePosition;
        yield return StartCoroutine(MoveSmooth(transform.position, servePos, moveToServeTime));

        // 2. 相手コート内のランダムな着弾点を決定
        float targetX = serveTeam == Team.Ally
            ? Random.Range(allyTargetMinX, allyTargetMaxX)
            : Random.Range(enemyTargetMinX, enemyTargetMaxX);
        float targetZ = Random.Range(targetMinZ, targetMaxZ);
        Vector3 targetPos = new Vector3(targetX, 0f, targetZ);

        // 3. ネットを越える発射速度を計算
        Vector3 spawnPos = transform.position + Vector3.up * 0.3f;
        Vector3 launchVel = CalculateServeVelocity(spawnPos, targetPos);

        // 4. ボール生成・発射
        GameObject ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        Collider ballCol  = ball.GetComponent<Collider>();
        Collider droneCol = GetComponent<Collider>();
        if (ballCol != null && droneCol != null)
            Physics.IgnoreCollision(droneCol, ballCol, true);

        if (ballRb != null)
        {
            ballRb.linearVelocity = launchVel;
            ball.name = "injectionball(Clone)";
        }

        // 5. 相手チームにレシーブ開始を通知（possesion を相手に切り替え）
        Team receiver = serveTeam == Team.Ally ? Team.Enemy : Team.Ally;
        if (MatchManager.Instance != null)
            MatchManager.Instance.ChangePossesion(receiver);

        // 6. スパイク位置へ戻る
        yield return StartCoroutine(MoveSmooth(transform.position, spikePosition, moveToSpikeTime));

        if (rb != null)
        {
            rb.isKinematic     = false;
            rb.linearVelocity  = Vector3.zero;
        }
        transform.position = spikePosition;
        isServing = false;
    }

    IEnumerator MoveSmooth(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.position = to;
    }

    // ネットをクリアできる最小飛行時間で速度ベクトルを求める
    Vector3 CalculateServeVelocity(Vector3 start, Vector3 target)
    {
        float g = Physics.gravity.y;

        // ネットが start-target 間にあるか確認
        float dx = target.x - start.x;
        float alphaNet = Mathf.Abs(dx) > 0.001f ? (netX - start.x) / dx : -1f;
        bool checkNet = alphaNet > 0.01f && alphaNet < 0.99f;

        for (float T = minFlightTime; T <= maxFlightTime; T += flightTimeStep)
        {
            float vx = (target.x - start.x) / T;
            float vz = (target.z - start.z) / T;
            float vy = (target.y - start.y - 0.5f * g * T * T) / T;

            if (checkNet)
            {
                float tNet = alphaNet * T;
                float yNet = start.y + vy * tNet + 0.5f * g * tNet * tNet;
                if (yNet < netHeight + netClearance) continue;
            }

            return new Vector3(vx, vy, vz);
        }

        // フォールバック：最大飛行時間で計算
        float vxF = (target.x - start.x) / maxFlightTime;
        float vzF = (target.z - start.z) / maxFlightTime;
        float vyF = (target.y - start.y - 0.5f * g * maxFlightTime * maxFlightTime) / maxFlightTime;
        Debug.LogWarning("[ServeDrone] ネットクリア可能な弾道が見つかりませんでした。maxFlightTime で代替します。");
        return new Vector3(vxF, vzF, vyF);
    }
}
