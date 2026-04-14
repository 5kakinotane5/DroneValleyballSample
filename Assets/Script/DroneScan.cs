using UnityEngine;

public class DroneSensor: MonoBehaviour{
    public float detectionRadius=0.5f;
    public float maxDistance=10f;
    public LayerMask ballLayer;

    void Update(){
        RaycastHit hit;

        if(Physics.SphereCast(transform.position,detectionRadius,transform.forward,out hit,maxDistance,ballLayer))
        {
            Debug.Log($"球を発見！名前:{hit.collider.name}");

        }
    }
}