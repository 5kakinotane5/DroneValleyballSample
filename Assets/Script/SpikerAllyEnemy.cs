using System;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;
using Random=UnityEngine.Random;
public class SpikerAllyEnemy : MonoBehaviour
{
    [SerializeField] private Team myTeam;
    [Header("判定するターゲットのタグ")]    
    public string targetTag = "injectionball"; // タグ名を統一

    [Header("ドローンの速度の何倍で飛ばすか")]
    public float tossBoost = 2f;

    [Header("最低限の跳ね上がり速度 (m/s)")]
    public float minTossSpeed = 5f;

    public string ballTag="injectionball";
    public float spikeHeight=10f;//地点Aの高さ打撃位置
    //public Vector3 initialPos;
    public Vector3 initialPos=new Vector3(10.5f,6.0f,0f);
    public float vMaxDrone=40f;//droneの最高速
    public float vMax=>vMaxDrone*tossBoost;

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
    private Vector3 pointB;

    private Vector3 standbyPoint;//軌道に入るための待ち構え地点
    private float timeUntilImpact;//地点Aで衝突するまでの残り時間
    private GameObject lastSpikedBall;
    private float g=Physics.gravity.y;

    enum State {Waiting,Hovering,MovingToTrajectory,Striking,Returning}
    [SerializeField] private State currentState=State.Waiting;

    void Start(){
        rb=GetComponent<Rigidbody>();
        rb.useGravity=false;
        transform.position=initialPos;
        }
    void FixedUpdate()
    {
        
        if(currentState==State.MovingToTrajectory || currentState==State.Striking){
            timeUntilImpact-=Time.fixedDeltaTime;
        }
        switch (currentState)
        {
            case State.Waiting:
                //Debug.Log($"currentphase:{MatchManager.Instance.currentPhase},currentPossesion:{MatchManager.Instance.currentPossesion},myTeam:{myTeam}");

                rb.linearVelocity=Vector3.zero;
                if (MatchManager.Instance.currentPhase == MatchManager.GamePhase.Spiking  && MatchManager.Instance.currentPossesion ==  myTeam)
                {
                    //Debug.Log("stateをhoveringへ");
                    currentState=State.Hovering;
                }
                break;
            case State.Hovering:
                Hover(initialPos);
                FindAndCalculateBall();
                break;
            
            case State.MovingToTrajectory:
                //待ち構えフェーズ：計算された直線軌道上のスタンバイ地点へ急行
                MoveToPoint(standbyPoint);
                
                if(timeUntilImpact<=runupTime){
                    //Debug.Log($"timeUntillImpact:{timeUntilImpact}, runupTime:{runupTime}");
                    currentState=State.Striking;
                }
                break;
            case State.Striking:
                //アタックフェーズ：地点Aを通り地点Bへ向かう直線軌道上に入る
                rb.linearVelocity=requiredDroneVel;
                if (myTeam == Team.Ally){
                    MatchManager.Instance.ChangePossesion(Team.Enemy);
                }else{
                    MatchManager.Instance.ChangePossesion(Team.Ally);
                }
                break;
            case State.Returning:
                lastSpikedBall=null;
                targetRb=null;
                timeUntilImpact=0;
                //MatchManager.Instance.currentPhase=MatchManager.GamePhase.Waiting;
                Hover(initialPos);
                if(Vector3.Distance(transform.position, initialPos)<0.3f){
                    //MatchManager.Instance.currentPhase=MatchManager.GamePhase.Waiting;
                    
                    currentState=State.Waiting;
                }
                break;   
        }
    }
    
    private void OnCollisionEnter(Collision collision){
    if(collision.gameObject.CompareTag(ballTag) && currentState == State.Striking){
        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb != null) {
            // ★ ここで計算結果を反映させる！
            // vBallPost = requiredDroneVel * tossBoost の関係
            Vector3 finalBallVelocity = requiredDroneVel * tossBoost;

            // ボールの速度を計算値で上書き（一度ゼロにする必要はありません）
            ballRb.linearVelocity = finalBallVelocity;

            // ドローン自身は衝突の反動で止める
            rb.linearVelocity = Vector3.zero;

            lastSpikedBall = collision.gameObject;
            currentState = State.Returning;
        }
    }
}

    void FindAndCalculateBall(){
            if(currentState!=State.Waiting && currentState != State.Hovering)
            {
                //Debug.Log("currentStatewaitorhover");
                return;
            }
            GameObject ball=GameObject.FindGameObjectWithTag(ballTag);
            if(ball==null || ball == lastSpikedBall)
            {
                //Debug.Log("ballnull");
                return;
            }
            targetRb=ball.GetComponent<Rigidbody>();
            if (targetRb == null)
            {
                //Debug.Log("targetRBnull");
                return;
            }
            //Debug.Log($"targetRb:{targetRb.linearVelocity.y}");
            //ボールが上昇中かつ目標高より低いときに計算
            if(targetRb.linearVelocity.y>0 && targetRb.position.y<spikeHeight && MatchManager.Instance.currentPhase==MatchManager.GamePhase.Spiking){
                if(CalculateTrajectory()){
                    //Debug.Log("calculatetrajectory");
                    currentState=State.MovingToTrajectory;
                }
            }
        }

    bool CalculateTrajectory(){
            float t=CalculateFalling(spikeHeight);
            if(t==-1) return false;
            timeUntilImpact=t;//球とドローンが当たるまでの時間
            //Debug.Log($"timeUtilImpact:{timeUntilImpact}");
             //2,地点Bをランダムに決定-21<x<10.5),y=0,-10<z<10f
            Vector3 pointB;
            if (MatchManager.Instance.currentPossesion == Team.Ally)
            {
                pointB=new Vector3(Random.Range(-21f,-10.5f),0f,Random.Range(-10f,10f));
            }
            else
            {
                pointB=new Vector3(Random.Range(21f,10.5f),0f,Random.Range(-10f,10f));
            }
            //Vector3 pointB=new Vector3(Random.Range(-21f,-10.5f),0f,Random.Range(-10f,10f));
            
            //3,地点A(a,b,c)の座標確定
            //spikeFlightTime:地点Aから地点Bまでのスパイクの移動時間
            pointA=new Vector3(targetRb.position.x+(targetRb.linearVelocity.x*t),spikeHeight,targetRb.position.z+(targetRb.linearVelocity.z*t));

            float BAx=pointB.x-pointA.x;
            float BAz=pointB.z-pointA.z;

            //4ボールの必要速度を算出
            float vBallX=BAx/spikeFlightTime;
            float vBallZ=BAz/spikeFlightTime;
            float vBallY=(pointB.y-pointA.y-0.5f*g*spikeFlightTime*spikeFlightTime)/spikeFlightTime;
            Vector3 vBallPost=new Vector3(vBallX,vBallY,vBallZ);
            //Debug.Log($"vBallPost:{vBallPost.magnitude},vMax:{vMax}");
            Debug.Log($"pointB(狙う位置):{pointB}");
            if (vBallPost.magnitude > vMax)
            {
                //vBallPost=vBallPost.normalized*vMaxDrone;
                float a=0.25f*g*g;
                float b=g*spikeHeight-vMax*vMax;
                float c=spikeHeight*spikeHeight+BAx*BAx+BAz*BAz;
                float det=b*b-4f*a*c;
                if (det < 0f)
                {
                    Debug.Log("det<0");
                    return false;
                }
                float t_rising=(-b+Mathf.Sqrt(det))/(2f*a);
                float t_falling=(-b-Mathf.Sqrt(det))/(2f*a);
                float tb=Mathf.Max(t_rising,t_falling);//球とドローンが当たり、地面に着くまでの時間
                tb=Mathf.Sqrt(tb);
            
                vBallX=BAx/tb;
                vBallZ=BAz/tb;
                vBallY=(pointA.y -pointB.y+0.5f * g * tb * tb) / tb;
                vBallPost=new Vector3(vBallX,vBallY,vBallZ);
            }
            //ネット回避チェック
            float tNet=(netX-pointA.x)/vBallX;
            float yNet=pointA.y+(vBallY*tNet)+(0.5f*g*tNet*tNet);
            /*if(yNet<netHeightSafe){
                Debug.LogWarning("Trajectory too low! Net collision predicted.");
                return false; // 低すぎる場合は打ち合わない
            }*/
            if (yNet < netHeightSafe)
            {
                //Debug.LogWarning("Trajectory too low! Recalculating for Lob...");
                float x2=pointA.x+(netHeightSafe-pointB.y)/(pointA.y-pointB.y)*(pointA.x-pointB.x);
                float t0=(Mathf.Sqrt(((netHeightSafe-pointA.y)*(pointB.x-pointA.x)/(x2-pointA.x)+pointA.y)*2/g*(pointB.x-pointA.x)/(x2-pointB.x)));
                vBallX=BAx/t0;
                vBallZ=BAz/t0;
                vBallY=(netHeightSafe-pointA.y)/(x2-pointA.x)*vBallX-0.5f*g*(x2-pointA.x)/vBallX;
                vBallPost=new Vector3(vBallX,vBallY,vBallZ);
            }
            

            // CalculateTrajectory の中の「4ボールの必要速度を算出」部分を修正
            //float boost = (ballTossScript != null) ? ballTossScript.tossBoost : 2.0f; // 安全策

            requiredDroneVel = vBallPost / tossBoost;
            //requiredDroneVel=vBallPost/2f;//地点Aでドローンが衝突する際の必要な速度=ボールの必要速度/トス強度

            //軌道に入り待ち構えするためのスタンバイ地点を逆算
            //もしtbがrunnupTimeより短い場合は即座にStrikingに移行できるように調整
            float actualRunup=Mathf.Min(runupTime,t);//助走に掛けれる時間
            standbyPoint=pointA-(requiredDroneVel*actualRunup);
            //Debug.Log($"requiredDroneVel:{requiredDroneVel}");
            //Debug.Log($"vBallPost.magnitude球の速度:{vBallPost.magnitude}");
            return true;
        }

    float CalculateFalling(float h)//上昇中の球がある高さhに到達までの時間tbを返す
    {
        float y0=targetRb.position.y;//ドローンの現在のy座標
        float vy0=targetRb.linearVelocity.y;//ドローンの現在のy成分の速度

        //1地点Aの時刻tbを求める
        float a=0.5f*g;
        float b=vy0;
        float c=y0-h;
        float det=b*b-4*a*c;

        if(det<0) return -1;

        float t_rising=(-b+Mathf.Sqrt(det))/(2*a);
        float t_falling=(-b-Mathf.Sqrt(det))/(2*a);
        float tb=Mathf.Max(t_rising,t_falling);

        if (tb<0) return -1;

        return tb;
    }

    void MoveToPoint(Vector3 target){
        Vector3 diff=target-transform.position;
        //P制御で目的地に吸い付くように移動
        rb.linearVelocity=diff/0.8f;
        if(rb.linearVelocity.magnitude>vMax){
            rb.linearVelocity=rb.linearVelocity.normalized*vMax;
        }
    }
    void Hover(Vector3 target){
        Vector3 diff=target-transform.position;
        if (diff.magnitude < 0.3f)
        {
            rb.linearVelocity=Vector3.zero;
            transform.position=target;
        }
        Vector3 speed=diff.normalized*vMaxDrone/8f;
        rb.linearVelocity=speed;
        /*
        float currentSpeed=rb.linearVelocity.magnitude;
        float speedDiff=Mathf.Max(0f,vMax-currentSpeed);
        float logFactor=Mathf.Log10(speedDiff+1.0f);
        Vector3 antiGraviy=-Physics.gravity;
        Vector3 moveForce=(diff*2.0f*logFactor)+antiGraviy-(rb.linearVelocity*0.7f);
        rb.AddForce(moveForce,ForceMode.Acceleration);
        */
    }
    public void ResetToInitialState()
    {
        currentState=State.Waiting;
        targetRb=null;
        transform.position=initialPos;
        GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
    }
}
