using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher: MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform firePoint;
    public float shotForce=30f;

    [Header("角度調整")]
    
    [Range(-90f,45f)] 
    public float launchAngle=-90;//-45でｙ軸正に45度
    
    [Range(-180f,180f)] 
    public float horizontalAngle=90f;

    void Update(){
        transform.localRotation=Quaternion.Euler(launchAngle,horizontalAngle,0);

        if(Keyboard.current!=null && Keyboard.current.enterKey.wasPressedThisFrame){
            Launch();
        }
    }

    void Launch(){
        GameObject ball=Instantiate(ballPrefab,firePoint.position,firePoint.rotation);

        Rigidbody rb=ball.GetComponent<Rigidbody>();
        if(rb!=null){
            rb.AddForce(firePoint.forward*shotForce,ForceMode.Impulse);
        }
    }
}