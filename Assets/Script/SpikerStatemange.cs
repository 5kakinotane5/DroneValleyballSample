using UnityEngine;

public class SpikerStatemanage : MonoBehaviour
{
    [Header("基本設定")]
    public string ballTag="injectionball";
    public float spikeHeight=10f;//地点Aの高さ打撃位置
    //public Vector3 initialPos;
    public Vector3 initialPos=new Vector3(10.5f,6.0f,0f);
    public float vMax=10f;

    [Header("弾道パラメータ")]
    [SerializeField] private float spikeFlightTime=0.6f;//地点AからBまでの滞空時間
    [SerializeField] private float runupTime=0.2f;

    [Header("ネット安全設定")]
    public float netX = 0f;            // ネットのX座標
    public float netHeightSafe = 4.9f; // 安全高度

    private Rigidbody rb;
    private Rigidbody targetRb;
    private Vector3 requiredDroneVel;//ドローンの必要速度
    private Vector3 pointA;//衝突予測地点A
    private Vector3 pointB=new Vector3(-19f,0f,0f);

    private Vector3 standbyPoint;//軌道に入るための待ち構え地点
    private float timeUntilImpact;//地点Aで衝突するまでの残り時間
    private GameObject lastSpikedBall;
    
    private float boost=2f;

    enum State {Waiting,Hovering,MovingToTrajectory,Striking,Returning}
    [SerializeField] private State currentState=State.Waiting;

    void Start(){
        rb=GetComponent<Rigidbody>();
        rb.useGravity=false;
        transform.position=initialPos;
        //ballTossScript = Object.FindFirstObjectByType<BallToss2>();
    }

    void FixedUpdate()
    {
        //if (VolleyballManager.Instance.currentPhase==GamePhase.Spiking){
        
        if(currentState==State.MovingToTrajectory || currentState==State.Striking){
            timeUntilImpact-=Time.fixedDeltaTime;
        }
        switch (currentState)
        {
            case State.Waiting:
                if (VolleyballManager.Instance.currentPhase == GamePhase.Spiking)
                {
                    Debug.Log("stateをhoveringへ");
                    currentState=State.Hovering;
                }
                Hover(initialPos);
                FindAndCalculateBall();

                break;
            case State.Hovering:
                Hover(initialPos);
                break;
            
            case State.MovingToTrajectory:
                //待ち構えフェーズ：計算された直線軌道上のスタンバイ地点へ急行
                MoveToPoint(standbyPoint);
                
                if(timeUntilImpact<=runupTime){
                    Debug.Log($"timeUntillImpact:{timeUntilImpact}, runupTime:{runupTime}");
                    currentState=State.Striking;
                }
                break;
            case State.Striking:
                //アタックフェーズ：地点Aを通り地点Bへ向かう直線軌道上に入る
                rb.linearVelocity=requiredDroneVel;

                Debug.DrawLine(standbyPoint,pointA,Color.red);
                Debug.DrawRay(transform.position,requiredDroneVel,Color.blue);
                break;
            case State.Returning:
                VolleyballManager.Instance.currentPhase=GamePhase.Waiting;
                Hover(initialPos);
                Vector2 posA=new Vector2(transform.position.x,transform.position.z);
                Vector2 posB=new Vector2(initialPos.x,initialPos.z);
                if(Vector2.Distance(posA,posB)<0.3f){
                    VolleyballManager.Instance.currentPhase=GamePhase.Waiting;
                    currentState=State.Waiting;
                }
                break;   
        }
    }
    
    private void OnCollisionEnter(Collision collision){
        if(collision.gameObject.CompareTag(ballTag) && currentState==State.Striking){
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                Vector3 hitPoint=collision.contacts[0].point;
                Debug.Log($"Spike Success!衝突座標:{hitPoint}/予測座標:{pointB}/");

                lastSpikedBall = collision.gameObject; // このボールを記憶
                currentState = State.Returning; // 即座に帰還状態へ
                Debug.Log("Spike Success! Returning home.");
            }
        }
    }

    void FindAndCalculateBall(){
            if(currentState!=State.Waiting && currentState!=State.Hovering) return;
            GameObject ball=GameObject.FindGameObjectWithTag(ballTag);
            if(ball==null || ball==lastSpikedBall) return;
            targetRb=ball.GetComponent<Rigidbody>();
            if (targetRb==null) return;
            //Debug.Log($"targetRb:{targetRb.linearVelocity.y}");
            //ボールが上昇中かつ目標高より低いときに計算
            if(targetRb.linearVelocity.y>0 && targetRb.position.y<spikeHeight && VolleyballManager.Instance.currentPhase==GamePhase.Spiking){
                if(CalculateTrajectory()){
                    Debug.Log("calculatetrajectory");
                    currentState=State.MovingToTrajectory;
                }
            }
            /*
            if (VolleyballManager.Instance.currentPhase == GamePhase.Spiking)
            {
                Debug.Log("MovingToTrajectoryへ移行");
                currentState=State.MovingToTrajectory;   
            }
            */
        }

    bool CalculateTrajectory(){
            float g=Physics.gravity.y;
            float y0=targetRb.position.y;//ドローンの現在のy座標
            float vy0=targetRb.linearVelocity.y;//ドローンの現在のy成分の速度

            //1地点Aの時刻tbを求める
            float a=0.5f*g;
            float b=vy0;
            float c=y0-spikeHeight;
            float det=b*b-4*a*c;

            if(det<0) return false;

            float t_rising=(-b+Mathf.Sqrt(det))/(2*a);
            float t_falling=(-b-Mathf.Sqrt(det))/(2*a);
            float tb=Mathf.Max(t_rising,t_falling);

            if (tb<0) return false;

            timeUntilImpact=tb;
            Debug.Log($"timeUtilImpact:{timeUntilImpact}");
             //2,地点Bをランダムに決定-21<x<10.5),y=0,-10<z<10f
            Vector3 pointB=new Vector3(Random.Range(-21f,-10.5f),0f,Random.Range(-10f,10f));
            
            

            //3,地点A(a,b,c)の座標確定
            //spikeFlightTime:地点Aから地点Bまでのスパイクの移動時間
            //vx=(targetRb.position.x-pointB.x)/spikeHeight,vz=(targetRb.position.z-pointB.z)/spikeHeight
            //pointA=new Vector3(,spikeHeight,)
            pointA=new Vector3(targetRb.position.x+(targetRb.linearVelocity.x*tb),spikeHeight,targetRb.position.z+(targetRb.linearVelocity.z*tb));

           

            //4ボールの必要速度を算出
            float vBallx=(pointB.x-pointA.x)/spikeFlightTime;
            float vBallZ=(pointB.z-pointA.z)/spikeFlightTime;
            float vBallY=(pointB.y-pointA.y-0.5f*g*spikeFlightTime*spikeFlightTime)/spikeFlightTime;

            //ネット回避チェック
            float tNet=(netX-pointA.x)/vBallx;
            float yNet=pointA.y+(vBallY*tNet)+(0.5f*g*tNet*tNet);
            if(yNet<netHeightSafe){
                Debug.LogWarning("Trajectory too low! Net collision predicted.");
                return false; // 低すぎる場合は打ち合わない
            }
            
            Vector3 vBallPost=new Vector3(vBallx,vBallY,vBallZ);

            // CalculateTrajectory の中の「4ボールの必要速度を算出」部分を修正
            //float boost = (ballTossScript != null) ? ballTossScript.tossBoost : 2.0f; // 安全策

            requiredDroneVel = vBallPost / boost;
            //requiredDroneVel=vBallPost/2f;//地点Aでドローンが衝突する際の必要な速度=ボールの必要速度/トス強度

            //軌道に入り待ち構えするためのスタンバイ地点を逆算
            //もしtbがrunnupTimeより短い場合は即座にStrikingに移行できるように調整
            float actualRunup=Mathf.Min(runupTime,tb);//助走に掛けれる時間
            standbyPoint=pointA-(requiredDroneVel*actualRunup);
            return true;
        }

    void MoveToPoint(Vector3 target){
        Vector3 diff=target-transform.position;
        //P制御で目的地に吸い付くように移動
        rb.linearVelocity=diff*10f;
        if(rb.linearVelocity.magnitude>vMax){
            rb.linearVelocity=rb.linearVelocity.normalized*vMax;
        }
    }
    void Hover(Vector3 target){
        Vector3 diff=target-transform.position;
        float currentSpeed=rb.linearVelocity.magnitude;
        float speedDiff=Mathf.Max(0f,vMax-currentSpeed);
        float logFactor=Mathf.Log10(speedDiff+1.0f);

        Vector3 antiGraviy=-Physics.gravity;
        Vector3 moveForce=(diff*2.0f*logFactor)+antiGraviy-(rb.linearVelocity*0.7f);

        rb.AddForce(moveForce,ForceMode.Acceleration);
    }
}
