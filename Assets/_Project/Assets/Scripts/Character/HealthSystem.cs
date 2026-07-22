using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public event Action<int, Vector2> OnDamaged; // 데미지가 실제로 적용된 순간 (피해량, 공격 원점) 전달. HitReactor가 구독.

    int baseHealth = 100;
    public int currentHealth = 100;
    float invincibleTime = 2f;

    bool isInvincible;
    float invincibleTimer;

    public int MaxHealth => baseHealth;
    public int CurrentHealth => currentHealth;

    void Start() {
        currentHealth = baseHealth;
    }

    void Update() {
        if (!isInvincible) return;

        invincibleTimer -= Time.deltaTime;
        if (invincibleTimer <= 0f) {
            isInvincible = false;
        }
    }

public void TakeDamage(int damage, Vector2 sourcePosition) {
        if (isInvincible) return;

        currentHealth -= damage;
        OnDamaged?.Invoke(damage, sourcePosition);

        if (currentHealth <= 0) {
            Die();
            return;
        }

        isInvincible = true;
        invincibleTimer = invincibleTime;
    }

    void Die() {
        Destroy(gameObject);
    }
}
