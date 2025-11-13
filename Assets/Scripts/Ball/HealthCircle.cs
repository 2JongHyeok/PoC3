using UnityEngine;

public class HealthCircle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer circleRenderer;

    private Material healthMaterial;
    private int maxHealth;
    private int currentHealth;

    void Awake()
    {
        healthMaterial = circleRenderer.material;
    }

    public void Setup(int maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
        UpdateHealthDisplay();
        // 초기화 시 발광 효과 끄기 (선택 사항)
        SetPulseEffect(false); // SetShineEffect 대신 SetPulseEffect 호출
    }

    public void UpdateHealth(int newHealth)
    {
        currentHealth = newHealth;
        UpdateHealthDisplay();
    }

    private void UpdateHealthDisplay()
    {
        if (maxHealth <= 0) return;

        float fillAmount = (float)currentHealth / maxHealth;
        healthMaterial.SetFloat("_FillAmount", fillAmount);
    }

    /// <summary>
    /// 숨 쉬는 듯한 발광 효과를 켜거나 끕니다.
    /// </summary>
    /// <param name="isOn">true이면 켜고, false이면 끕니다.</param>
    public void SetPulseEffect(bool isOn) // 메소드 이름 변경
    {
        if (healthMaterial != null)
        {
            float toggle = isOn ? 1.0f : 0.0f;
            healthMaterial.SetFloat("_PulseToggle", toggle); // _ShineToggle 대신 _PulseToggle 사용
        }
    }
}
