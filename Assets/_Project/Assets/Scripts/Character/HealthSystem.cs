using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    #region 인스펙터 변수

    [Header("체력")]
    public int maxHealth = 100;

    [Header("피격 무적")]
    // 피격 직후 무적 시간. **몬스터는 0, 플레이어는 0.9 정도로 설정하세요.**
    // 몬스터에 무적을 주면 공격 쿨타임보다 길어져 때려도 반응이 없는 것처럼 보인다.
    public float invincibleTime = 0f;

    #endregion
    #region 이벤트

    public event Action<int, Vector2> OnDamaged; // 데미지가 실제로 적용된 순간 (피해량, 공격 원점) 전달. HitReactor/HitFlash/HealthBar가 구독.
    public event Action<Vector2> OnDeath; // 사망한 순간 (마지막 공격 원점) 전달. HitReactor가 구독해 사망 연출을 재생.

    #endregion
    #region 컴포넌트 변수

    public int currentHealth = 100;

    bool isInvincible;
    float invincibleTimer;
    bool isDead; // 같은 프레임에 여러 타격이 들어와도 Die()가 두 번 돌지 않게 막는 가드.

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    #endregion
    #region 유니티 라이프 사이클

    void Start() {
        currentHealth = maxHealth;
    }

    void Update() {
        if (!isInvincible) return;

        invincibleTimer -= Time.deltaTime;
        if (invincibleTimer <= 0f) {
            isInvincible = false;
        }
    }

    #endregion
    #region 데미지 처리

    public void TakeDamage(int damage, Vector2 sourcePosition) {
        if (isDead || isInvincible) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        // 치명타여도 항상 발행한다. 마지막 일격에서도 스파크/데미지 숫자/플래시가 나와야 하기 때문.
        // 구독자는 CurrentHealth <= 0 으로 처치 여부를 판단한다.
        OnDamaged?.Invoke(damage, sourcePosition);

        if (currentHealth <= 0) {
            Die(sourcePosition);
            return;
        }

        // invincibleTime이 0이면 무적을 아예 걸지 않는다. 한 프레임짜리 무적도 남기지 않아 모든 타격이 확실히 들어간다.
        if (invincibleTime <= 0f) return;

        isInvincible = true;
        invincibleTimer = invincibleTime;
    }

    void Die(Vector2 sourcePosition) {
        isDead = true;
        OnDeath?.Invoke(sourcePosition); // 오브젝트가 파괴되기 전에 사망 연출을 띄울 기회를 준다.
        Destroy(gameObject);
    }

    #endregion
    #region 체력 회복

    // 데미지가 아닌 경로(세이브 복원, 거울에서의 회복, 기억 조각 섭취)로 체력을 바꿀 때 쓴다.
    // 0을 넣어도 Die()는 돌지 않는다 — 사망은 TakeDamage를 통해서만 일어나야 연출이 한 번만 재생된다.
    // HealthBar는 매 프레임 CurrentHealth를 읽어 그리므로 별도 이벤트 없이도 표시가 따라온다.
    public void SetHealth(int value) {
        if (isDead) return;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    // 거울(체크포인트)에서 자아 게이지를 가득 채울 때 사용.
    public void RestoreFull() {
        SetHealth(maxHealth);
    }

    #endregion
}
