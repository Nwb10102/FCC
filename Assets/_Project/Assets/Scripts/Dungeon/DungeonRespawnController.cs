using System;
using UnityEngine;

// 던전 입장 동안만 존재하는 컨트롤러. DungeonGate 가 입장 시 Begin 으로 만들고, 퇴장·사망 시 정리된다.
//
// 던전 안에서는 두 가지가 본편과 다르게 처리된다.
//  - 낙사 : DungeonFallZone 또는 fallYThreshold 통과 → 가장 최근에 지나온 방의 리스폰 지점으로 복귀.
//           페널티로 자아 게이지를 깎고 잠시 무적을 걸어 준다(사망 연출도, 게임오버도 없다).
//  - 사망 : 자아 게이지가 0이 되면 Health.DeathInterceptor 로 Die() 를 가로채, 던전을 해체하고 게이트 앞으로 되돌린다.
public class DungeonRespawnController : MonoBehaviour {
    #region 싱글턴

    public static DungeonRespawnController Instance { get; private set; }

    public static DungeonRespawnController Begin(GameObject player, float fallYThreshold, int fallDamage,
        float fallInvincibleTime, Action onPlayerDeath) {
        if (Instance != null) Instance.Dispose(); // 이전 것이 남아 있으면 먼저 정리.

        var go = new GameObject("DungeonRespawnController");
        var controller = go.AddComponent<DungeonRespawnController>();
        controller.Setup(player, fallYThreshold, fallDamage, fallInvincibleTime, onPlayerDeath);
        return controller;
    }

    #endregion
    #region 상태

    GameObject player;
    Health playerHealth;
    Rigidbody2D playerBody;
    DungeonRoom currentRoom;

    float fallYThreshold;
    int fallDamage;
    float fallInvincibleTime;
    Action onPlayerDeath;
    bool disposed;

    #endregion
    #region 준비 · 정리

    void Setup(GameObject player, float fallYThreshold, int fallDamage, float fallInvincibleTime, Action onPlayerDeath) {
        Instance = this;

        this.player = player;
        this.fallYThreshold = fallYThreshold;
        this.fallDamage = Mathf.Max(0, fallDamage);
        this.fallInvincibleTime = Mathf.Max(0f, fallInvincibleTime);
        this.onPlayerDeath = onPlayerDeath;

        if (player != null) {
            player.TryGetComponent(out playerHealth);
            player.TryGetComponent(out playerBody);
        }

        if (playerHealth != null) playerHealth.DeathInterceptor = HandleLethal;
    }

    // 훅 해제 + 인스턴스 정리. 사망·퇴장 어느 쪽으로 들어와도 한 번만 실행된다.
    public void Dispose() {
        if (disposed) return;
        disposed = true;

        if (playerHealth != null && playerHealth.DeathInterceptor == HandleLethal) {
            playerHealth.DeathInterceptor = null;
        }
        if (Instance == this) Instance = null;

        if (this != null) Destroy(gameObject);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    #endregion
    #region 방 추적 · 낙사

    // DungeonRoom 이 플레이어 진입 시 호출한다.
    public void SetCurrentRoom(DungeonRoom room) {
        currentRoom = room;
    }

    void Update() {
        if (disposed || player == null) return;
        if (player.transform.position.y < fallYThreshold) RespawnAtCurrentRoom();
    }

    // DungeonFallZone 트리거와 fallYThreshold 양쪽이 부른다.
    public void RespawnAtCurrentRoom() {
        if (currentRoom == null || player == null) return;

        Transform anchor = currentRoom.RespawnAnchor;
        if (anchor == null) return;

        player.transform.position = anchor.position;
        if (playerBody != null) playerBody.linearVelocity = Vector2.zero;

        if (playerHealth == null) return;

        // 낙사 페널티. 이 데미지로 죽으면 HandleLethal 이 이어받아 밖으로 되돌린다(의도된 흐름).
        if (fallDamage > 0) playerHealth.TakeDamage(fallDamage, anchor.position);

        // 복귀 직후 잠깐 무적. 리스폰 지점 근처에 몬스터가 붙어 있어도 손 쓸 새 없이 연달아 맞는 것을 막는다.
        // 반드시 데미지를 준 뒤에 걸어야 한다 — 먼저 걸면 TakeDamage 가 무적에 막혀 이번 낙사 피해가 사라진다.
        playerHealth.SetInvincible(fallInvincibleTime);
    }

    #endregion
    #region 사망 가로채기

    // Health.DeathInterceptor. true 를 반환해 Die() 를 취소하고, 던전 밖으로 되돌리는 처리를 게이트에 넘긴다.
    bool HandleLethal(Vector2 sourcePosition) {
        if (playerHealth != null) {
            playerHealth.DeathInterceptor = null; // 정리 도중 또 데미지가 들어와도 다시 여기로 오지 않도록 먼저 뗀다.
            playerHealth.SetHealth(1);            // 죽지 않은 상태로 되돌린다. 최종 회복량은 게이트가 정한다.
        }

        Action callback = onPlayerDeath;
        Dispose();
        callback?.Invoke();
        return true;
    }

    #endregion
}
