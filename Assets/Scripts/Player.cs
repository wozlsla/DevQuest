using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float maxHP = 100f; // 최대 체력
    private float currentHP; // 현재 체력

    private bool isDead = false;

    private void Start()
    {
        // 체력 초기화
        currentHP = maxHP;
        Debug.Log($"[Player] 체력 초기화: {currentHP}/{maxHP}");
    }

    // 데미지를 받는 함수 (적의 공격에서 호출됨)
    public void TakeDamage(float damage)
    {
        // 이미 죽은 상태면 데미지 무시
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"[Player] {damage} 데미지 받음! 남은 체력: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 플레이어 사망 처리
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("[Player] 사망! 게임오버!");

        // MoveControl 비활성화 (더 이상 움직이지 못하게)
        MoveControl moveControl = GetComponent<MoveControl>();
        if (moveControl != null)
        {
            moveControl.enabled = false;
        }

        // CCInputManager 비활성화 (총 발사 못하게)
        CCinputManager inputManager = GetComponent<CCinputManager>();
        if (inputManager != null)
        {
            inputManager.enabled = false;
        }

        GameOver();
    }

    // 게임오버
    private void GameOver()
    {
        Debug.Log("=================================");
        Debug.Log("        GAME OVER!");
        Debug.Log("        R키를 눌러 재시작");
        Debug.Log("=================================");

        // 게임 완전히 멈추기
        Time.timeScale = 0f;

        // 선택: 3초 후 재시작
        // Invoke("RestartGame", 3f);
    }

    private void Update()
    {
        // 게임오버 후 R키로 재시작
        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    private void RestartGame()
    {
        // 게임 속도 복구
        Time.timeScale = 1f;

        // 현재 활성화된 씬 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 체력 회복
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHP += amount;
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }

        Debug.Log($"[Player] {amount} 회복! 현재 체력: {currentHP}/{maxHP}");
    }

    // 현재 체력 반환 (UI 표시용 - 아직 미구현)
    public float GetCurrentHP()
    {
        return currentHP;
    }

    // 최대 체력 반환 (UI 표시용 - 아직 미구현)
    public float GetMaxHP()
    {
        return maxHP;
    }

    // 죽었는지 확인
    public bool IsDead()
    {
        return isDead;
    }
}

