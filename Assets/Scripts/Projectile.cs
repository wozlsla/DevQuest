using System.Collections;
using UnityEngine;


public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 30f; // 발사체 속도
    [SerializeField] private float lifeTime = 5f; // 생존 시간 (초)
    [SerializeField] private bool useGravity = false; // 중력 사용 여부

    private Rigidbody rb;
    private bool hasHit = false;
    private float spawnTime;
    private ProjectilePool pool; // Object Pool 참조
    private Coroutine lifeTimeCoroutine; // 생존 시간 코루틴 참조

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Rigidbody 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 설정 적용
        rb.useGravity = useGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 빠른 오브젝트용
    }

    private void OnEnable()
    {
        // 발사체가 풀에서 활성화될 때마다 초기화
        hasHit = false;
        spawnTime = Time.time;

        // 생존 시간 후 자동으로 풀로 반환
        if (lifeTimeCoroutine != null)
        {
            StopCoroutine(lifeTimeCoroutine);
        }
        lifeTimeCoroutine = StartCoroutine(ReturnToPoolAfterLifetime());
    }

    private void OnDisable()
    {
        // 비활성화 시 코루틴 정리
        if (lifeTimeCoroutine != null)
        {
            StopCoroutine(lifeTimeCoroutine);
            lifeTimeCoroutine = null;
        }
    }

    /// <summary>
    /// 생존 시간이 지나면 풀로 반환
    /// </summary>
    private IEnumerator ReturnToPoolAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    /// <summary>
    /// 풀 참조 설정 (ProjectilePool에서 호출)
    /// </summary>
    public void SetPool(ProjectilePool projectilePool)
    {
        pool = projectilePool;
    }

    // 발사체 속도 설정
    public void SetVelocity(Vector3 velocity)
    {
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }

    // 발사체 속도 직접 설정
    public void Launch(Vector3 direction, float customSpeed = -1f)
    {
        float actualSpeed = customSpeed > 0 ? customSpeed : speed;
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * actualSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 발사 직후 0.1초 동안은 충돌 무시 (발사한 Player와의 충돌 방지)
        if (Time.time - spawnTime < 0.1f) return;

        // 이미 맞았으면 무시 (다중 충돌 방지 -> 첫 충돌만 처리)
        if (hasHit) return;
        hasHit = true;

        Debug.Log($"HIT: {collision.gameObject.name}");

        // Destroy 대신 풀로 반환
        ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - spawnTime < 0.1f) return;

        // Trigger 충돌 처리
        if (hasHit) return;
        hasHit = true;

        Debug.Log($"PASS: {other.name}");

        // Destroy 대신 풀로 반환
        ReturnToPool();
    }

    /// <summary>
    /// 발사체를 풀로 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (pool != null)
        {
            pool.ReturnProjectile(gameObject);
        }
        else
        {
            // 풀이 없으면 기존 방식으로 파괴 (하위 호환성)
            Debug.LogWarning("ProjectilePool이 설정되지 않아 Destroy로 처리합니다.");
            Destroy(gameObject);
        }
    }
}

