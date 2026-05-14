using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Diagnostics;
public class ServerDrone : MonoBehaviour
{
    public GameObject ballPrefab;

    [Header("1. トス（斜方投射）の設定")]
    public float timeToReachHitPoint = 1.2f;
    public float hitPointForwardOffset = 1.0f;
    public float hitPointHeightOffset = 6.0f;

    [Header("2. スパイク（ランダム着地）の設定")]
    public float serveFlightTime = 1f;
    public float courtMinX = 15f;
    public float courtMaxX = 21f;
    public float courtMinZ = -10.0f;
    public float courtMaxZ = 10.0f;

    private Vector3 originalPos;
    private Quaternion originalRot;
    private bool isServing = false;
    [SerializeField] private Serve myServe;

    void Start()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    void Update()
    {
        if (!isServing)
        {
            transform.rotation = originalRot;
        }

        //if (Input.GetKeyDown(KeyCode.Space) && !isServing)
        if (Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame && !isServing && ServeManager.Instance.currentPossesion==myServe)
        {
            StartCoroutine(RealJumpServeSequence());
        }
    }

    IEnumerator RealJumpServeSequence()
    {
        isServing = true;

        Vector3 randomTargetPos = new Vector3(
            Random.Range(courtMinX, courtMaxX),
            0f,
            Random.Range(courtMinZ, courtMaxZ)
        );
        Vector3 dirToTarget = (randomTargetPos - originalPos);
        dirToTarget.y = 0;
        dirToTarget.Normalize();

        Vector3 hitPoint = originalPos + dirToTarget * hitPointForwardOffset + Vector3.up * hitPointHeightOffset;

        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        // ==========================================
        // ★追加：ドローンとボールの物理的な衝突判定を無視する
        // ==========================================
        Collider ballCollider = ball.GetComponent<Collider>();
        Collider droneCollider = GetComponent<Collider>();

        if (ballCollider != null && droneCollider != null)
        {
            Physics.IgnoreCollision(droneCollider, ballCollider, true);
        }
        // ==========================================

        if (ballRb != null)
        {
            float gravity = Physics.gravity.y;
            Vector3 diff = hitPoint - spawnPos;
            float vx = diff.x / timeToReachHitPoint;
            float vz = diff.z / timeToReachHitPoint;
            float vy = (diff.y - 0.5f * gravity * timeToReachHitPoint * timeToReachHitPoint) / timeToReachHitPoint;
            ballRb.linearVelocity = new Vector3(vx, vy, vz);
        }

        // 【ドローンの上昇】
        yield return new WaitForSeconds(timeToReachHitPoint * 0.3f);

        float jumpDuration = timeToReachHitPoint * 0.6f;
        float elapsed = 0f;
        Vector3 jumpTargetPos = originalPos + Vector3.up * hitPointHeightOffset;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            float tSin = Mathf.Sin(t * Mathf.PI * 0.5f);

            float nextY = Mathf.Lerp(originalPos.y, jumpTargetPos.y, tSin);
            transform.position = new Vector3(originalPos.x, nextY, originalPos.z);
            transform.rotation = originalRot;
            yield return null;
        }
        transform.position = jumpTargetPos;
        transform.rotation = originalRot;

        // 【同期待ち】
        float attackDuration = 0.05f;
        float remainingTime = timeToReachHitPoint - (timeToReachHitPoint * 0.3f + jumpDuration) - attackDuration;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // 【スパイク（衝突判定は無視されているので邪魔されない！）】
        if (ballRb != null)
        {
            Vector3 currentBallPos = ball.transform.position;

            Vector3 targetDiff = randomTargetPos - currentBallPos;
            float spvx = targetDiff.x / serveFlightTime;
            float spvz = targetDiff.z / serveFlightTime;
            float g = Physics.gravity.y;
            float spvy = (targetDiff.y - 0.5f * g * serveFlightTime * serveFlightTime) / serveFlightTime;
            Vector3 spikeVelocity = new Vector3(spvx, spvy, spvz);

            Vector3 attackStartPos = transform.position;
            Vector3 attackDirection = spikeVelocity.normalized;
            Vector3 attackEndPos = currentBallPos - attackDirection * 0.3f;

            elapsed = 0f;
            while (elapsed < attackDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(attackStartPos, attackEndPos, elapsed / attackDuration);
                transform.rotation = originalRot;
                yield return null;
            }
            transform.position = attackEndPos;
            transform.rotation = originalRot;

            // ここで確実にプログラムの速度が乗る！
            ballRb.linearVelocity = spikeVelocity;
            ball.name = "injectionball(Clone)";
        }

        // 【着地と復帰】
        yield return new WaitForSeconds(0.4f);

        elapsed = 0f;
        Vector3 currentPos = transform.position;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(currentPos, originalPos, elapsed / 0.5f);
            transform.rotation = originalRot;
            yield return null;
        }
        transform.position = originalPos;
        transform.rotation = originalRot;

        isServing = false;
    }
}