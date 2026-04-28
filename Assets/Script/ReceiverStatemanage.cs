using UnityEngine;

public class ReceiverStatemanage : MonoBehaviour
{
    public Rigidbody targetBall;
    public float moveSpeed = 10f;


    // ★追加：レシーブを落とす目標地点（X, Y, Z）と、そこまでの時間
    public Vector3 initialPos = new Vector3(10f, 1f, 0f); // 後でインスペクターで設定します
    public float returnFlightTime = 1.5f; // ふわっと上げるための時間（秒）

    enum State {Waiting,Hovering,MovingToTrajectory,Receiving,Returning}
    [SerializeField] private State currentState=State.Waiting;

    void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Waiting:
            //Hover(initialPos);
                if (VolleyballManager.Instance.currentPhase == GamePhase.Receiving)
                {
                    currentState=State.Hovering;
                }
                Hover(initialPos);
                break;

            case State.Hovering:
                FindAndCalculateBall();
                Hover(initialPos);
                break;

            case State.MovingToTrajectory:
                Vector3 landingPos=PredictLandingPoint(targetBall.position,targetBall.linearVelocity,transform.position.y);
                Vector3 targetPos=new Vector3(landingPos.x,transform.position.y,landingPos.z);
                
                Hover(targetPos);
                //collisionしたらreceiveへ行く

                break;
            /*
            case State.Receiving:
                
                break;
            */
            case State.Returning:
                VolleyballManager.Instance.currentPhase=GamePhase.Spiking;
                Hover(initialPos);
                if (Vector3.Distance(transform.position, initialPos) < 0.3f)
                {
                    currentState=State.Waiting;
                }
                break;
        }
    }
    void FindAndCalculateBall()
    {
        GameObject ball=GameObject.Find("injectionball(Clone)");
        if (ball==null) return;
        targetBall=ball.GetComponent<Rigidbody>();
        currentState=State.MovingToTrajectory;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("injectionball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // ★変更：AddForceをやめて、完璧な速度を計算する

                Vector3 startPos = collision.transform.position; // ぶつかった今の場所

                // 必要な速度(X, Y, Z)を物理演算で逆算
                float vx = (initialPos.x - startPos.x) / returnFlightTime;
                float vz = (initialPos.z - startPos.z) / returnFlightTime;
                float gravity = Physics.gravity.y;
                float vy = (initialPos.y - startPos.y - 0.5f * gravity * returnFlightTime * returnFlightTime) / returnFlightTime;

                // ボールに計算した速度をピタッとセット！
                ballRb.linearVelocity = new Vector3(vx, vy, vz);

                Debug.Log("セッターのような完璧なレシーブ！");

                // 待機するための魔法（そのまま）
                collision.gameObject.name = "ReceivedBall";
                targetBall = null;
                currentState=State.Returning;
            }
        }
    }

    void Hover(Vector3 target){
        Rigidbody rb;
        rb=GetComponent<Rigidbody>();
        Vector3 diff=target-transform.position;
        float distance=diff.magnitude;

        if (distance < 0.1f)
        {
            rb.linearVelocity=Vector3.zero;
            transform.position=target;
            return;
        }
        Vector3 nextVelocity=diff.normalized*moveSpeed;

        rb.linearVelocity=nextVelocity;
    }

    

    // 落下地点予測の計算（そのまま）
    Vector3 PredictLandingPoint(Vector3 startPos, Vector3 velocity, float targetY)
    {
        float gravity = Physics.gravity.y;
        float a = 0.5f * gravity;
        float b = velocity.y;
        float c = startPos.y - targetY;
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return startPos;
        float t = Mathf.Max((-b + Mathf.Sqrt(discriminant)) / (2 * a), (-b - Mathf.Sqrt(discriminant)) / (2 * a));
        return new Vector3(startPos.x + velocity.x * t, targetY, startPos.z + velocity.z * t);
    }
    
}