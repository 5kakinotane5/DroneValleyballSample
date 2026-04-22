using UnityEngine;

public class DestroyOnCollision : MonoBehaviour
{
    [Header("このタグを持つ物体とぶつかると消去する")]
    public string targetTag = "Ball";

    [Header("自分を消す場合はチェックを入れる")]
    public bool destroySelf = false;

    // 衝突した瞬間に呼ばれる
    private void OnCollisionEnter(Collision collision)
    {
        // ぶつかった相手のタグをチェック
        if (collision.gameObject.CompareTag(targetTag))
        {
            Vector3 hitPoint=collision.contacts[0].point;
            Debug.Log($"{targetTag} にぶつかったので自分を消去しました。消失地点: {hitPoint}");
            Destroy(gameObject);
            Debug.Log($"{targetTag}にぶつかったので自分を消去しました。");
            
        }
    }
}