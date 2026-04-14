using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCollision: MonoBehaviour{
    void OnCollisionEnter(Collision collision){
        Debug.Log("当たり");
    }
}