using UnityEngine;

public class Health : MonoBehaviour
{
    int baseHealth = 100;
    int currentHealth = 100;
    float invincibleTime = 2f;

    bool isInvincible;
    float invincibleTimer;

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

    public void TakeDamage(int damage) {
        if (isInvincible) return;

        currentHealth -= damage;
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
