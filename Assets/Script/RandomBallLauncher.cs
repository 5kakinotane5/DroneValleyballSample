using UnityEngine;
using UnityEngine.InputSystem;
public class RandomBallLauncher : MonoBehaviour
{
    [Header("発射するボールのプレハブ")]
    public GameObject ballPrefab;

    [Header("着弾までの時間（秒）")]
    public float flightTime = 3f;

    void Update()
    {
        // エンターキーで発射！
        if (Keyboard.current!=null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ShootBall();
        }
    }

    void ShootBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("Ball Prefabがセットされていません！");
            return;
        }

        // 1. ボールを生成
        GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        // 2. 自分のコートのランダムな目標地点を決める
        // ※ここの数字を自分のコートの座標に合わせて微調整してください
        float randomX = Random.Range(1f, 21f);
        float randomZ = Random.Range(-10f, 10f);
        //Vector3 targetPoint = new Vector3(randomX, 0f, randomZ);
        Vector3 targetPoint=new Vector3(18.27f,0f,-5.7f);//この場合ドローンはスパイクできない
        Debug.Log($"予想落下地点：{targetPoint}");
        // 3. 必要な初速を物理計算で出す
        Vector3 startPoint = transform.position;
        float vx = (targetPoint.x - startPoint.x) / flightTime;
        float vz = (targetPoint.z - startPoint.z) / flightTime;
        float gravity = Physics.gravity.y;
        float vy = (targetPoint.y - startPoint.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

        // 4. 速度をセット
        ballRb.linearVelocity = new Vector3(vx, vy, vz);
    }
}