using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 싱글톤 패턴
    public static UIManager Instance { get; private set; }

    [Header("Player UI (Screen Space)")]
    [SerializeField] private TextMeshProUGUI hpText; // HP 텍스트
    [SerializeField] private Slider hpSlider; // HP 슬라이더
    [SerializeField] private TextMeshProUGUI enemyCountText; // 처치한 적 수 / 남은 적 수

    [Header("Game Result UI (Screen Space)")]
    [SerializeField] private GameObject gameResultPanel; // 승리/패배 패널
    [SerializeField] private TextMeshProUGUI gameResultTitle; // "VICTORY!" 또는 "GAME OVER"
    [SerializeField] private TextMeshProUGUI gameResultMessage; // 결과 메시지
    [SerializeField] private TextMeshProUGUI restartText; // "Press R to Restart"

    private Player player;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 플레이어 찾기
        player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            Debug.LogError("[UIManager] Player를 찾을 수 없습니다"); //debug
        }

        // 게임 결과 패널 숨기기
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(false);
        }

        // 초기 UI 업데이트
        UpdatePlayerHP();
        UpdateEnemyCount(0, 0, 0);
    }

    private void Update()
    {
        // 매 프레임마다 HP 업데이트
        UpdatePlayerHP();
    }

    // Player HP 업데이트
    public void UpdatePlayerHP()
    {
        if (player == null) return;

        float currentHP = player.GetCurrentHP();
        float maxHP = player.GetMaxHP();

        // HP 텍스트 업데이트
        if (hpText != null)
        {
            hpText.text = $"HP: {currentHP:F0}/{maxHP:F0}";

            // HP에 따라 색상 변경
            if (currentHP <= maxHP * 0.3f) // 30% 이하
            {
                hpText.color = Color.red;
            }
            else if (currentHP <= maxHP * 0.6f) // 60% 이하
            {
                hpText.color = Color.yellow;
            }
            else
            {
                hpText.color = Color.white;
            }
        }

        // HP 슬라이더 업데이트 (있다면)
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    // Enemy 카운트 업데이트
    public void UpdateEnemyCount(int killed, int alive, int killsNeeded)
    {
        if (enemyCountText != null)
        {
            enemyCountText.text = $"{killed}/{killsNeeded} | total: {alive}";
        }
    }

    // 게임 결과 표시 (승리)
    public void ShowVictory(int enemiesKilled)
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
        }

        if (gameResultTitle != null)
        {
            gameResultTitle.text = "VICTORY!";
            gameResultTitle.color = Color.green;
        }

        if (gameResultMessage != null)
        {
            gameResultMessage.text = $"{enemiesKilled}개의 Enemy를 처치했습니다"; // 사용X
        }

        if (restartText != null)
        {
            restartText.text = "Press R to Restart";
        }
    }

    // 게임 결과 표시 (패배)
    public void ShowGameOver()
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
        }

        if (gameResultTitle != null)
        {
            gameResultTitle.text = "GAME OVER";
            gameResultTitle.color = Color.red;
        }

        if (gameResultMessage != null)
        {
            gameResultMessage.text = "적에게 패배했습니다...";
        }

        if (restartText != null)
        {
            restartText.text = "Press R to Restart";
        }
    }
}

