using UnityEngine;
public class BallToss:MonoBehaviour
{
    [Header("判定するターゲットのタグ")]    
    public string targetTag="valleyball";

    [Header("トスの強さ倍率")]
    public float tossBoost=1.2f;

    [Header("最低限の跳ね上がり力")]
    public float minTossForce=5f;

    private Rigidbody droneRb;

    void Start(){
        droneRb=GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision){
        if(collision.gameObject.CompareTag(targetTag)){
            Rigidbody valleyballRb=collision.gameObject.GetComponent<Rigidbody>();

            if(valleyballRb!=null){
                //ドローンの現在の速度を取得
                float droneSpeedY=Mathf.Max(0,droneRb.linearVelocity.y);

                //加える力を計算
                float finalTossForce=(droneSpeedY*tossBoost)+minTossForce;

                //ボールの速度をリセット。重力の影響を消す
                valleyballRb.linearVelocity=new Vector3(valleyballRb.linearVelocity.x,0,valleyballRb.linearVelocity.z);

                valleyballRb.AddForce(Vector3.up*finalTossForce,ForceMode.Impulse);

                Debug.Log($"トス成功!加えた力{finalTossForce}(ドローン速度:{droneSpeedY})");

            }
        }
    }
}