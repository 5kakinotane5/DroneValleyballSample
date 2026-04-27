/*
トスされた球の弾道計算
球が高さbの時の時刻tbと地点Aの座標(a,b,c)を求める
球の狙う位置(地点B)を-21<x<-10.5,y=0,-10<z<10からランダムに地点Bを決める
地点Aでドローンが球と衝突するときに必用な速度を求める。
球の速度はドローンが球と当たった時にドローンの速度の二倍の速度になる。
ドローンは現在位置から、上記の求めた速度で地点Aを通りこの速度のベクトルで同じ傾きな直線状の軌道に入る。
そして、球に衝突できるようにドローンは待ち構え地点Aへ求めた速度で飛行する。
ボールと衝突後ドローンは初期位置の座標(10.5,h,0)に戻りホバリングする
改善点
地点Bにマーカーをつける。
ネットに引っかからないようにする。
｛ネットの高さは4.8であり余裕をもって安全高度を4.9とする。
　ネットを超える地点Aのx座標/球の速度｝
トス強度を上げる。

問題点
現在はspikeFlightTimeによってスパイクが依存されて想定した地点に打球できてない
//Spike Success!衝突座標:(2.14, 15.15, -0.79)/予測座標:(-19.00, 0.00, 0.00)/
UnityEngine.Debug:Log (object)
AdvancedDroneSpiker:OnCollisionEnter (UnityEngine.Collision) (at Assets/Script/AdvancedDroneSpiker.cs:181)
UnityEngine.Physics:OnSceneContact (UnityEngine.PhysicsScene,intptr,int)

ドローンの衝突前のスピード:(-20.92, -13.03, 0.82)
UnityEngine.Debug:Log (object)
BallToss2:OnCollisionEnter (UnityEngine.Collision) (at Assets/Script/BallToss2.cs:36)
UnityEngine.Physics:OnSceneContact (UnityEngine.PhysicsScene,intptr,int)

トス成功! 合力速度: (-41.84, -26.06, 1.65) (倍率: 2)
UnityEngine.Debug:Log (object)
BallToss2:OnCollisionEnter (UnityEngine.Collision) (at Assets/Script/BallToss2.cs:48)
UnityEngine.Physics:OnSceneContact (UnityEngine.PhysicsScene,intptr,int)

court にぶつかったので自分を消去しました。消失地点: (-18.23, 0.00, 0.08)
UnityEngine.Debug:Log (object)
DestroyOnCollision:OnCollisionEnter (UnityEngine.Collision) (at Assets/Script/DestoryOnCollision.cs:18)
UnityEngine.Physics:OnSceneContact (UnityEngine.PhysicsScene,intptr,int)


*/

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
    [SerializeField] private float spikeFlightTime=0.2f;//地点AからBまでの滞空時間
    [SerializeField] private float runupTime=0.3f;

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
    
    enum State {Waiting,Hovering,MovingToTrajectory,Striking,Returning}
    [SerializeField] private State currentState=State.Waiting;

    void Start(){
        rb=GetComponent<Rigidbody>();
        rb.useGravity=false;
        transform.position=initialPos;
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
                Hover(initialPos);
                if(Vector3.Distance(transform.position,initialPos)<0.3f){
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
            GameObject ball=GameObject.FindGameObjectWithTag(ballTag);
            if(ball==null || ball==lastSpikedBall) return;
            targetRb=ball.GetComponent<Rigidbody>();
            if (targetRb==null) return;

            //ボールが上昇中かつ目標高より低いときに計算
            if(targetRb.linearVelocity.y>0 && targetRb.position.y<spikeHeight && VolleyballManager.Instance.currentPhase==GamePhase.Spiking){
                if(CalculateTrajectory()){
                    currentState=State.MovingToTrajectory;
                }
            }

            if (VolleyballManager.Instance.currentPhase == GamePhase.Spiking)
            {
                Debug.Log("MovingToTrajectoryへ移行");
                currentState=State.MovingToTrajectory;   
            }
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

            requiredDroneVel=vBallPost/2f;//地点Aでドローンが衝突する際の必要な速度=ボールの必要速度/トス強度

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
