using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용

public class EnemyHpBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider; // HP 슬라이더
    [SerializeField] private TextMeshProUGUI healthText; // HP 텍스트 (선택사항, TMP)
    [SerializeField] private Image fillImage; // 슬라이더 채우기 이미지

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0); // Enemy 머리 위 오프셋
    [SerializeField] private bool alwaysFaceCamera = true; // 항상 카메라를 바라봄

    private Enemy enemy; // Enemy 참조
    private Camera mainCamera;

    private void Start()
    {
        // Enemy 컴포넌트 가져오기
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("[EnemyHpBar] Enemy 컴포넌트를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[EnemyHpBar] Enemy 찾음! HP: {enemy.GetCurrentHP()}/{enemy.GetMaxHP()}");
        }

        // 메인 카메라 가져오기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[EnemyHpBar] 메인 카메라를 찾을 수 없습니다!");
        }

        // UI 컴포넌트 확인
        if (healthSlider == null) Debug.LogWarning("[EnemyHpBar] Health Slider가 연결되지 않음!");
        if (fillImage == null) Debug.LogWarning("[EnemyHpBar] Fill Image가 연결되지 않음!");

        // 초기 HP 업데이트
        UpdateHealthBar();

        Debug.Log($"[EnemyHpBar] 초기화 완료! 위치: {transform.position}");
    }

    private void LateUpdate()
    {
        // Enemy 위치를 따라다니기
        if (enemy != null)
        {
            transform.position = enemy.transform.position + offset;
        }

        // 항상 카메라를 바라보도록 회전
        if (alwaysFaceCamera && mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                            mainCamera.transform.rotation * Vector3.up);
        }

        // HP 업데이트
        UpdateHealthBar();
    }

    // HP 바 업데이트
    public void UpdateHealthBar()
    {
        if (enemy == null) return;

        float currentHP = enemy.GetCurrentHP();
        float maxHP = enemy.GetMaxHP();

        // 슬라이더 업데이트
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;
            healthSlider.value = currentHP;
        }

        // 텍스트 업데이트 (선택사항)
        if (healthText != null)
        {
            healthText.text = $"{currentHP:F0}/{maxHP:F0}";
        }

        // 체력에 따라 색상 변경
        if (fillImage != null)
        {
            float hpPercent = currentHP / maxHP;

            if (hpPercent > 0.6f)
            {
                fillImage.color = Color.green; // 체력 60% 이상
            }
            else if (hpPercent > 0.3f)
            {
                fillImage.color = Color.yellow; // 체력 30~60%
            }
            else
            {
                fillImage.color = Color.red; // 체력 30% 이하
            }
        }
    }
}

