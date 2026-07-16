using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player_move))]
public class Player_Combat : MonoBehaviour {
    #region 인스펙터 변수

    [Header("공격 판정")]
    public Transform attackPoint; // **플레이어 공격 판정용 빈 오브젝트를 만드세요.**
    public float attackRange = 1f; // 공격이 닿는 범위.
    public LayerMask enemyLayer; // 몬스터 레이어.

    [Header("공격 타이밍")]
    public float attackDelay = 0.15f; // 판정이 들어가기 전 선딜레이.
    public float attackCooldown = 0.4f; // 공격 후 다음 공격까지의 대기 시간.

    [Header("공격 데미지")]
    public int attackDamage = 10;

    [Header("공격 범위 표시")]
    public Color rangeIndicatorColor = new(1f, 0f, 0f, 0.85f); // 선딜레이 동안 보여줄 판정 범위 색상.

    #endregion
    #region 컴포넌트 변수

    Player_move moveSystem;
    AttackState state = AttackState.Idle;
    float stateTimer;
    SpriteRenderer rangeIndicator;

    static Sprite ringSprite;

    #endregion

    enum AttackState { Idle, Windup, Cooldown }

    #region 유니티 라이프 사이클

    void Awake() {
        moveSystem = GetComponent<Player_move>();
        BuildRangeIndicator();
    }

    void Update() {
        switch (state) {
            case AttackState.Windup:
                TickWindup();
                break;
            case AttackState.Cooldown:
                TickCooldown();
                break;
        }
    }

    #endregion
    #region 입력 처리

    void OnAttack(InputValue value) {
        if (!value.isPressed) return;
        if (state != AttackState.Idle) return;

        StartWindup();
    }

    #endregion
    #region 공격 관련 함수

    void StartWindup() {
        state = AttackState.Windup;
        stateTimer = attackDelay;
        moveSystem.isMovementLocked = true; // 선딜레이 동안 제자리에서 공격.
        ShowRangeIndicator();
    }

    void TickWindup() {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        DealDamage();
        StartCooldown();
    }

    void DealDamage() {
        if (attackPoint == null) return;

        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, attackRange, enemyLayer);
        if (hit == null) return;

        Health enemyHealth = hit.GetComponentInParent<Health>();
        if (enemyHealth != null) {
            enemyHealth.TakeDamage(attackDamage);
        }
    }

    void StartCooldown() {
        state = AttackState.Cooldown;
        stateTimer = attackCooldown;
        moveSystem.isMovementLocked = false;
        HideRangeIndicator();
    }

    void TickCooldown() {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) {
            state = AttackState.Idle;
        }
    }

    // 공격 판정 범위 표시 (에디터 전용)
    void OnDrawGizmos() {
        if (attackPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    #endregion
    #region 공격 범위 표시

    void BuildRangeIndicator() {
        if (attackPoint == null) return;

        GameObject obj = new GameObject("AttackRangeIndicator");
        obj.transform.SetParent(attackPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = new Vector3(attackRange * 2f, attackRange * 2f, 1f);

        rangeIndicator = obj.AddComponent<SpriteRenderer>();
        rangeIndicator.sprite = GetRingSprite();
        rangeIndicator.color = rangeIndicatorColor;
        rangeIndicator.sortingOrder = 50;
        obj.SetActive(false); // 선딜레이가 시작될 때만 보여준다.
    }

    void ShowRangeIndicator() {
        if (rangeIndicator != null) rangeIndicator.gameObject.SetActive(true);
    }

    void HideRangeIndicator() {
        if (rangeIndicator != null) rangeIndicator.gameObject.SetActive(false);
    }

    static Sprite GetRingSprite() {
        if (ringSprite == null) ringSprite = CreateRingSprite(64, 0.15f);
        return ringSprite;
    }

    // 가장자리만 칠해진 원형 텍스처를 만들어 별도 이미지 에셋 없이 판정 범위 링을 그린다.
    static Sprite CreateRingSprite(int resolution, float thicknessRatio) {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(resolution - 1, resolution - 1) * 0.5f;
        float outerRadius = resolution * 0.5f;
        float innerRadius = outerRadius * (1f - thicknessRatio);

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                bool onRing = dist <= outerRadius && dist >= innerRadius;
                texture.SetPixel(x, y, onRing ? Color.white : Color.clear);
            }
        }
        texture.Apply();

        // pixelsPerUnit == resolution: 스케일 1일 때 스프라이트 크기가 정확히 1 월드 유닛이 되어,
        // transform.localScale로 지름을 그대로 지정할 수 있다.
        return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }

    #endregion
}
