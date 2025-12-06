using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Damage/Heal Settings")]
    public int damageAmount = 10;
    public int healAmount = 5;

    [Header("References (Optional)")]
    public PlayerMovement player;

    [Header("Death Behavior")]
    public bool destroyOnDeath = false;
    public float destroyDelay = 0f;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int> OnDamaged;
    public UnityEvent<int> OnHealed;

    public int CurrentHealth => currentHealth;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsFullHealth => currentHealth >= maxHealth;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void Heal(int amount = -1)
    {
        if (IsDead) return;
        int healValue = amount > 0 ? amount : healAmount;
        currentHealth = Mathf.Min(currentHealth + healValue, maxHealth);
        OnHealed?.Invoke(healValue);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
        if (currentHealth <= 0) Die();
    }

    public void FullHeal()
    {
        if (IsDead) return;
        int healValue = maxHealth - currentHealth;
        currentHealth = maxHealth;
        OnHealed?.Invoke(healValue);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void Revive(int healthAmount = -1)
    {
        if (!IsDead) return;
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        int reviveHealth = healthAmount > 0 ? healthAmount : maxHealth;
        currentHealth = Mathf.Clamp(reviveHealth, 1, maxHealth);
        if (player != null) player.isDead = false;
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        if (player != null) player.isDead = true;
        OnDeath?.Invoke();
        if (destroyOnDeath)
        {
            if (destroyDelay > 0)
                Destroy(gameObject, destroyDelay);
            else
                Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
