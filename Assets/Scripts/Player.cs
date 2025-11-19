using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField][Range(0f, 100f)] private float maxHP = 100f;
    private float currentHP;

    private bool isDead = false;

    private void Start()
    {
        currentHP = maxHP;
        Debug.Log($"[Player] HP: {currentHP}/{maxHP}");
    }

    // 데미지를 받는 함수 (적의 공격에서 호출됨)
    public void TakeDamage(float damage)
    {
        // 이미 죽은 상태면 데미지 무시
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"[Player] HP: -{damage}"); // currentHP/maxHP -> UI

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("[Player] Game Over");

        // // MoveControl 비활성화
        // MoveControl moveControl = GetComponent<MoveControl>();
        // if (moveControl != null)
        // {
        //     moveControl.enabled = false;
        // }

        // Player (CCInputManager) 비활성화
        CCinputManager inputManager = GetComponent<CCinputManager>();
        if (inputManager != null)
        {
            inputManager.enabled = false;
        }

        GameOver();
    }

    private void GameOver()
    {
        // GameManager를 통해 처리
        GameManager.Instance.GameOver();
    }

    /* Getter */
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
}

