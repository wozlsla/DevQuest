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

        // 생성 시간 기록
        spawnTime = Time.time;
    }

    private void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifeTime);
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

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - spawnTime < 0.1f) return;

        // Trigger 충돌 처리 (옵션)
        if (hasHit) return;
        hasHit = true;

        Debug.Log($"발사체가 {other.name}을 통과!");

        Destroy(gameObject);
    }
}

