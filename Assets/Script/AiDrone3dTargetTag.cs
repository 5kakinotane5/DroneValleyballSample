using UnityEngine;
//Ballタグ付けされたオブジェクトを追跡する機能です。
public class AiDrone3dTargetTag : MonoBehaviour
{
    //[SerializeField] private Rigidbody targetRb;      // ボールのRigidbodyをアサイン
    [Header("判定するターゲットのタグ")]    
    public string targetTag="injectionball";
    [SerializeField] private float interceptTime = 2f; // T：何秒後に到達するか
    [SerializeField] private float minCatchHeight = 0.6f; // 床に激突しない安全高度
    private Rigidbody rb;
    private Rigidbody targetRb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        GameObject ballObj = GameObject.FindGameObjectWithTag(targetTag);
        if(ballObj==null){
            return;
        }
        targetRb=ballObj.GetComponent<Rigidbody>();
        Vector3 g=Physics.gravity;//デフォルトは(0,-9.81,0)
        float gY = g.y;
        Vector3 ballPos = targetRb.position;
        Vector3 ballVel = targetRb.linearVelocity;
        
        float T = interceptTime;

        //1ボールが最低高度(minCatchHeight)に達する時間を逆算する
        //二次方程式: 0.5 * gY * t^2 + Vy * t + (currentY - minCatchHeight) = 0
        float a=0.5f*gY;
        float b=ballVel.y;
        float c=ballPos.y-minCatchHeight;

        float determinant=b*b-4f*a*c;

        if(determinant>=0){
            // 解の公式より、未来の到達時間 t を算出
            // 重力 a がマイナスなので、分母を考慮して適切な解(未来)を選ぶ
            float tSolution=(-b-Mathf.Sqrt(determinant))/(2f*a);
            
            //もしボールが現在の設定時間(interceptTime)より早く床(付近)に着くなら、Tを短縮する
            if (tSolution>0&&tSolution<interceptTime){
                T=tSolution;
            }
        }

        //2ボールのt秒後の本来の未来位置を予測
        // 公式: P_future = P_now + V_now * T + 0.5 * g * T^2    }
        float T2=T*T;

        Vector3 ballFuturePos=ballPos+ballVel*T+0.5f*g*T2;
        // 高さが最低ラインを割らないように最終ガード（計算誤差対策）
        if (ballFuturePos.y<minCatchHeight){
            ballFuturePos.y=minCatchHeight;
        }
        

        //3ドローンの未来位置（現在の速度のままT秒後の位置）
        //重力も加速度もない場合のT秒後の位置
        Vector3 droneFuturePosBase=transform.position+rb.linearVelocity*T;

        //4必要な加速度aの計算
        // 目標位置 (ballFuturePos) と 現在の予測位置 (droneFuturePosBase) の差を埋めるための加速度
        // 公式: ΔP = 0.5 * a * T^2  より  a = (2 * ΔP) / T^2
        Vector3 deltaP=ballFuturePos-droneFuturePosBase;//相対位置
        
        
        //Vector3 acceleration=(2f*deltaP)/T2;

        // 分母(T2)が小さすぎると加速度が爆発するので、ごく近い場合は計算を止める
        Vector3 acceleration = (T > 0.05f) ? (2f * deltaP) / T2 : Vector3.zero;

        rb.AddForce(acceleration,ForceMode.Acceleration);

        Debug.DrawLine(transform.position,ballFuturePos,Color.red);
    }
}
