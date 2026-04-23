/*using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform firePoint;
    public float shotForce = 30f;

    [Header("垂直角度のランダム範囲")]
    public float minLaunchAngle = -90f;
    public float maxLaunchAngle = -45f;

    // 現在の角度（インスペクターで見れるように public にしていますが、Launchで書き換わります）
    public float launchAngle = -45f;

    void Update()
    {
        // 常に現在の launchAngle を回転に反映させる
        // horizontal は不要とのことなので 0（または固定値）に設定
        transform.localRotation = Quaternion.Euler(launchAngle, 0, 0);

        // Enterキーで発射
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Launch();
        }
    }

    void Launch()
    {
        // 1. 発射の瞬間に角度をランダムに決定する
        launchAngle = Random.Range(minLaunchAngle, maxLaunchAngle);

        // 2. 回転を即座に更新（firePoint.forward を正しい方向に向けるため）
        transform.localRotation = Quaternion.Euler(launchAngle, 0, 0);

        // 3. ボールを生成して飛ばす
        if (ballPrefab != null && firePoint != null)
        {
            GameObject ball = Instantiate(ballPrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                rb.AddForce(firePoint.forward * shotForce, ForceMode.Impulse);
                Debug.Log($"発射！ 角度: {launchAngle:F1}");
            }
        }
    }
}*/
using UnityEngine;
using UnityEngine.InputSystem;
public class BallLauncher: MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform firePoint;
    public float shotForce=30f;

    
    
    

    [Header("角度調整")]
    
    [Range(-90f,45f)] 
    public float launchAngle= -90f;//-45でｙ軸正に45/
    
    [Range(-180f,180f)] 
    public float horizontalAngle=90f;

    void Update(){
        transform.localRotation=Quaternion.Euler(launchAngle,horizontalAngle,0);

        if(Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame){
            Launch();
        }
    }

    void Launch(){
        GameObject ball=Instantiate(ballPrefab,firePoint.position,firePoint.rotation);

        Rigidbody rb=ball.GetComponent<Rigidbody>();
        if(rb!=null){
            rb.AddForce(firePoint.forward*shotForce,ForceMode.Impulse);
        }
    }
}