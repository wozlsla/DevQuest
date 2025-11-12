using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int enemiesToKillForVictory = 3; // 승리를 위해 처치해야 할 Enemy 수

    [Header("Game State")]
    private int totalEnemies; // 게임 시작 시 총 Enemy 수
    private int enemiesKilled; // 처치한 Enemy 수
    private int enemiesAlive; // 현재 살아있는 Enemy 수

    private bool gameOver = false; // 게임 종료 여부 (승리 또는 패배)

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지
        }
        else
        {
            Destroy(gameObject); // 중복 인스턴스 제거
            return;
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    // 게임 초기화
    private void InitializeGame()
    {
        // 씬에 있는 모든 Enemy 개체 수 카운트
        // Enemy[] enemies = FindObjectsByType<Enemy>;
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        totalEnemies = enemies.Length;
        enemiesAlive = totalEnemies;
        enemiesKilled = 0;
        gameOver = false;

        Debug.Log($"[GameManager] 게임 시작! 총 Enemy 수: {totalEnemies}");
        Debug.Log($"[GameManager] 승리 조건: {enemiesToKillForVictory}개 처치");

        UpdateUI();
    }

    // Enemy가 죽을 때 호출되는 함수
    public void OnEnemyDeath()
    {
        if (gameOver) return; // 게임이 이미 끝났으면 무시

        enemiesKilled++;
        enemiesAlive--;

        Debug.Log($"[GameManager] Enemy 처치! 처치 수: {enemiesKilled}/{enemiesToKillForVictory}, 남은 수: {enemiesAlive}");

        UpdateUI();
        CheckVictory();
    }

    // 승리 조건 체크
    private void CheckVictory()
    {
        if (enemiesKilled >= enemiesToKillForVictory)
        {
            Victory();
        }
    }

    // 승리 처리
    public void Victory()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("=================================");
        Debug.Log("        VICTORY!");
        Debug.Log($"        {enemiesKilled}개의 Enemy를 처치했습니다!");
        Debug.Log("        R키를 눌러 재시작");
        Debug.Log("=================================");

        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictory(enemiesKilled);
        }

        // 게임 멈추기
        Time.timeScale = 0f;
    }

    // 패배 처리 (Player가 죽었을 때)
    public void GameOver()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("=================================");
        Debug.Log("        GAME OVER!");
        Debug.Log("        R키를 눌러 재시작");
        Debug.Log("=================================");

        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }

        // 게임 멈추기
        Time.timeScale = 0f;
    }

    // UI 업데이트
    private void UpdateUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateEnemyCount(enemiesKilled, enemiesAlive, enemiesToKillForVictory);
        }
    }

    // R키로 재시작
    private void Update()
    {
        if (gameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    // 게임 재시작
    private void RestartGame()
    {
        // 게임 속도 복구
        Time.timeScale = 1f;

        // 현재 활성화된 씬 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // 게임 재초기화
        InitializeGame();
    }

    // Getter 함수들 (UI 표시용)
    public int GetEnemiesKilled()
    {
        return enemiesKilled;
    }

    public int GetEnemiesAlive()
    {
        return enemiesAlive;
    }

    public int GetTotalEnemies()
    {
        return totalEnemies;
    }

    public int GetEnemiesToKillForVictory()
    {
        return enemiesToKillForVictory;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }
}

