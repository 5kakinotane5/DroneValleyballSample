using UnityEngine;

public class DroneAroundScan: MonoBehaviour{
    public float detectionRadius=3f;
    public LayerMask ballLayer;

    void Update(){
        Collider[] hits=Physics.OverlapSphere(transform.position,3f,ballLayer);
        if(hits.Length>0)
        {
            GameObject ball=hits[0].gameObject;
            Debug.Log($"ドローンの近くに球を発見！名前:{ball.name}");
            Debug.Log($"球の座標{ball.transform.position}で発見");
        }
    }
}