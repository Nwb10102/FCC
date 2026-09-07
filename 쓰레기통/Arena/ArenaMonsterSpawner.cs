using System;
using System.Collections.Generic;
using UnityEngine;

// 방 프리팹 하나에 종속되는 스폰·전멸감지 컴포넌트. arenaId나 던전 전체 진행 상황은 모르고,
// "이 방에 뿌린 몬스터를 전부 잡았는가"만 판정한다(판정 로직을 한 곳에만 두기 위함).
public class ArenaMonsterSpawner : MonoBehaviour {
    #region 인스펙터 변수

    [Header("스폰 목록")]
    public List<Transform> spawnPoints; // spawnPoints[i] 위치에 monsterPrefabs[i]를 스폰한다.
    public List<GameObject> monsterPrefabs; // **Health가 붙은 몬스터 프리팹만 넣으세요.**

    #endregion
    #region 이벤트

    public event Action OnAllMonstersDefeated;

    #endregion
    #region 런타임 변수

    int aliveCount;

    #endregion
    #region 스폰

    // 이미 생존한 개체가 있으면(aliveCount > 0) 아무 것도 하지 않는다 — 던전 이탈 없이 중복 호출돼도
    // 몬스터가 겹쳐 늘어나지 않도록 막는 가드.
    public void SpawnAll() {
        if (aliveCount > 0) return;

        int count = Mathf.Min(spawnPoints.Count, monsterPrefabs.Count);
        for (int i = 0; i < count; i++) {
            if (spawnPoints[i] == null || monsterPrefabs[i] == null) continue;

            GameObject monster = Instantiate(monsterPrefabs[i], spawnPoints[i].position, spawnPoints[i].rotation);
            if (!monster.TryGetComponent(out Health health)) {
                Debug.LogWarning($"[ArenaMonsterSpawner] '{monsterPrefabs[i].name}'에 Health가 없어 전멸 판정에서 셀 수 없습니다.", this);
                continue;
            }

            aliveCount++;
            health.OnDeath += HandleMonsterDeath;
        }
    }

    #endregion
    #region 전멸 판정

    // Die()가 이벤트 발행 직후 Destroy(gameObject)를 호출하므로 별도 despawn 처리는 필요 없다.
    void HandleMonsterDeath(Vector2 sourcePosition) {
        aliveCount = Mathf.Max(0, aliveCount - 1);
        if (aliveCount == 0) OnAllMonstersDefeated?.Invoke();
    }

    #endregion
}
