using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pooling을 사용하여 발사체를 효율적으로 관리하는 클래스
/// Instantiate/Destroy의 성능 오버헤드를 줄이기 위해 재사용 가능한 오브젝트 풀 관리
/// </summary>
public class ProjectilePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject projectilePrefab; // 풀링할 발사체 프리팹
    [SerializeField] private int initialPoolSize = 15; // 초기 풀 크기 -> '동시에 화면에 있을 최대 개수'를 기준으로 설정??
    [SerializeField] private int maxPoolSize = 30; // 최대 풀 크기
    [SerializeField] private bool autoExpand = true; // 풀이 부족할 때 자동 확장 여부

    private Queue<GameObject> pool = new Queue<GameObject>(); // (사용 가능) 발사체 큐
    private HashSet<GameObject> activeProjectiles = new HashSet<GameObject>(); // (현재 활성화된) 발사체 추적 -> 해시셋

    /* 싱글톤 패턴(Singleton Pattern) 
       - 클래스의 인스턴스가 오직 하나만 존재하도록 보장하는 디자인 패턴
       -> 게임 전체에서 ProjectilePool이 단 하나만 존재하도록 보장 */
    private static ProjectilePool instance;
    public static ProjectilePool Instance
    {
        // Lazy Initialization
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ProjectilePool>();

                if (instance == null)
                {
                    GameObject poolObj = new GameObject("ProjectilePool");
                    instance = poolObj.AddComponent<ProjectilePool>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 싱글톤 설정
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return; // 파괴 후 즉시 종료
        }

        instance = this;

        InitializePool(); // 초기화
    }

    /// <summary>
    /// 초기 풀 생성
    /// </summary>
    private void InitializePool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("ProjectilePool: projectilePrefab이 설정되지 않았습니다.");
            return;
        }

        int successCount = 0;
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject projectile = CreateNewProjectile();
            if (projectile != null)
            {
                successCount++;
            }
        }

        Debug.Log($"ProjectilePool 초기화 완료: {successCount}/{initialPoolSize}개의 발사체 생성 됨.");
    }

    /// <summary>
    /// 새로운 발사체 '1 개'를 생성하고 풀에 추가.
    /// 1) 게임 시작 시 - 초기화
    /// 2) 게임 실행 중 - 풀 고갈 시
    /// </summary>
    private GameObject CreateNewProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("ProjectilePool: projectilePrefab이 설정되지 않았습니다. Inspector에서 프리팹을 할당하세요.");
            return null;
            /* Q. InitializePool이 이미 검사했으니 중복 아닌가?
               A. Defensive Programming
               CreateNewProjectile()이 projectilePrefab을 직접 사용
               -> projectilePrefab이 null일 때의 예외 처리는 CreateNewProjectile()에서 책임지고 방어 */
        }

        GameObject projectile = Instantiate(projectilePrefab, transform);
        projectile.SetActive(false); // 부모의 transform (ProjectilePool 오브젝트)을 전달하여 Hierarchy 관리 및 상태 관리

        // Projectile 스크립트에 풀 참조 전달
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetPool(this); // 발사체 소멸 시 풀로 반환되도록
        }
        else
        {
            Debug.LogError($"ProjectilePool: 'projectilePrefab'에 'Projectile' 스크립트가 없습니다.", projectilePrefab);
            Destroy(projectile); // 잘못된 객체는 즉시 파괴
            return null; // 생성 실패 알림
        }

        pool.Enqueue(projectile);
        return projectile;
    }

    /// <summary>
    /// 풀에서 발사체를 가져옴
    /// </summary>
    public GameObject GetProjectile()
    {
        GameObject projectile = null;

        if (pool.Count > 0)
        {
            // 풀에 사용 가능한 발사체가 있으면 가져옴
            projectile = pool.Dequeue();
        }
        else
        {
            if (autoExpand && activeProjectiles.Count < maxPoolSize)
            {
                // 자동 확장이 활성화되어 있고 최대 크기를 초과하지 않으면 새로 생성
                Debug.Log("ProjectilePool: 풀이 부족하여 새 발사체 생성");
                GameObject newProjectile = CreateNewProjectile();
                if (newProjectile == null)
                {
                    Debug.LogError("ProjectilePool: 새 발사체 생성 실패");
                    return null;
                }
                projectile = pool.Dequeue();
            }
            else
            {
                Debug.LogWarning("ProjectilePool: 사용 가능한 발사체가 없습니다");
                return null;
            }
        }

        // 발사체 활성화 및 추적
        if (projectile != null)
        {
            projectile.SetActive(true);
            activeProjectiles.Add(projectile);
        }

        return projectile;
    }

    /// <summary>
    /// 발사체를 풀로 반환
    /// </summary>
    public void ReturnProjectile(GameObject projectile)
    {
        if (projectile == null) return;

        // 이미 풀에 있는지 확인 (중복 반환 방지)
        if (!activeProjectiles.Contains(projectile))
        {
            Debug.LogWarning("ProjectilePool: 이미 풀에 반환된 발사체입니다.");
            return;
        }

        // 발사체 비활성화 및 초기화
        projectile.SetActive(false);
        projectile.transform.position = Vector3.zero;
        projectile.transform.rotation = Quaternion.identity; // 회전 없음 (기본 상태)

        // Rigidbody 초기화
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 추적에서 제거하고 풀에 다시 추가
        activeProjectiles.Remove(projectile);
        pool.Enqueue(projectile);
    }

    /// <summary>
    /// 풀의 현재 상태를 반환 (디버깅용)
    /// </summary>
    public void GetPoolStats(out int available, out int active, out int total)
    {
        available = pool.Count;
        active = activeProjectiles.Count;
        total = available + active;
    }

    /// <summary>
    /// Inspector에서 풀 상태를 확인할 수 있도록 표시
    /// </summary>
    private void OnGUI()
    {
        if (Application.isPlaying && Debug.isDebugBuild)
        {
            int available, active, total;
            GetPoolStats(out available, out active, out total);

            GUI.Label(new Rect(10, 10, 300, 20), $"발사체 풀: 사용 가능 {available} | 활성 {active} | 총 {total}");
        }
    }
}

