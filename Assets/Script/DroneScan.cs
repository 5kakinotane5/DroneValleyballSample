using UnityEngine;

public class DroneScan: MonoBehaviour{
    public float detectionRadius=0.5f;
    public float maxDistance=10f;
    public LayerMask BallLayer;

    void Update(){
        RaycastHit hit;
        if(Physics.SphereCast(transform.position,detectionRadius,transform.up,out hit,maxDistance,BallLayer))
        {
            Debug.Log($"球を発見！名前:{hit.collider.name}");
            Debug.Log($"球の座標{hit.point}で発見");
        }
    }

    void OnDrawGizmos(){
        Gizmos.color=new Color(0,1,0,0.3f);

        Vector3 start=transform.position;
        Vector3 direction=transform.forward;
        Vector3 end=start+direction*maxDistance;

        //scanの開始位置に球を表示
        Gizmos.DrawSphere(start,detectionRadius);
        Gizmos.DrawSphere(end,detectionRadius);
        Gizmos.color=Color.green;
        Gizmos.DrawLine(start,end);
    }
}