using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 미션이 완료되면 인스펙터에 넣어둔 오브젝트들을 치운다.
// (길을 막던 문, 대화가 끝난 NPC, 연출용 소품처럼 미션이 끝나면 남아 있으면 안 되는 것들)
//
// 완료 판정은 하지 않고 ObjectiveManager의 완료 신호만 듣는다 — 판정이 두 곳에 생기면
// 세이브를 불러왔을 때 어느 쪽이 맞는지 알 수 없게 된다.
//
// **씬 어디에 붙여도 됩니다.** 치울 오브젝트를 targets에 직접 끌어다 넣으세요.
// 미션 하나에 여러 개를 붙여도 되고(구역별로 나눠 관리), 하나에 몰아 넣어도 된다.
public class MissionCompleteDespawner : MonoBehaviour {
    #region 인스펙터 변수

    [Header("지켜볼 미션")]
    // **이 미션이 완료되는 순간 아래 오브젝트들이 사라집니다.**
    // ObjectiveManager의 missions 목록에도 들어 있는 미션이어야 한다.
    public Mission mission;

    [Header("치울 오브젝트")]
    // **여기에 씬 오브젝트를 직접 끌어다 넣으세요.** 비어 있는 칸은 그냥 넘어간다.
    public List<GameObject> targets = new();

    [Header("방식")]
    // 켜면 Destroy, 끄면 SetActive(false). 기본은 끄기다 —
    // 세이브를 불러 이미 끝난 미션을 정리할 때 같은 처리를 다시 해야 하는데,
    // 꺼두기만 하면 씬을 다시 열어도 결과가 같고 디버그로 되살려 볼 수도 있다.
    public bool destroyTargets;

    // 완료 연출(체크 표시·효과음)이 보이기도 전에 오브젝트가 사라지면 뚝 끊긴 것처럼 보인다.
    // 그럴 때만 조금 늦춘다. **세이브 복원처럼 "이미 끝난 미션"을 정리할 때는 무시하고 즉시 치운다.**
    [Min(0f)]
    public float delaySeconds;

    #endregion
    #region 런타임 변수

    // 구독한 매니저를 그대로 들고 있다가 같은 것에서 해제한다. 이 컴포넌트는 씬 오브젝트이고
    // 매니저는 DontDestroyOnLoad라, 씬을 다시 열면 Instance가 다른 것으로 바뀌어 있을 수 있다.
    ObjectiveManager manager;

    #endregion
    #region 유니티 라이프 사이클

    // Awake가 아니라 Start인 이유: ObjectiveManager.Instance는 매니저의 Awake에서 채워지는데
    // 실행 순서가 보장되지 않아 Awake 시점에는 아직 null일 수 있다.
    void Start() {
        if (mission == null) {
            Debug.LogWarning($"[MissionCompleteDespawner] '{name}'에 지켜볼 미션이 비어 있습니다.", this);
            return;
        }

        // 파괴된 뒤에도 C# 참조가 남을 수 있어 ?. 대신 != null 로 Unity의 == 오버로드를 탄다.
        if (ObjectiveManager.Instance == null) {
            Debug.LogWarning($"[MissionCompleteDespawner] '{name}' — 씬에 ObjectiveManager가 없습니다.", this);
            return;
        }

        manager = ObjectiveManager.Instance;
        manager.OnMissionCompleted += HandleMissionCompleted;
        manager.OnStateRestored += DespawnIfAlreadyCompleted;

        // 세이브를 불러 이미 끝난 미션으로 시작하는 경우. 이걸 빼먹으면 이어하기를 했을 때
        // 지워졌어야 할 문이 다시 서 있게 된다.
        DespawnIfAlreadyCompleted();
    }

    void OnDestroy() {
        if (manager == null) return;

        manager.OnMissionCompleted -= HandleMissionCompleted;
        manager.OnStateRestored -= DespawnIfAlreadyCompleted;
    }

    #endregion
    #region 완료 처리

    void HandleMissionCompleted(MissionRuntime completed) {
        if (completed.Id != mission.MissionId) return; // 다른 미션이 끝난 것.

        if (delaySeconds > 0f) StartCoroutine(DespawnAfterDelay());
        else Despawn();
    }

    IEnumerator DespawnAfterDelay() {
        // 미션 완료 직후에는 히트스톱이 걸려 있을 수 있다. Time.timeScale에 끌려가면
        // 인스펙터에 적은 초와 실제 체감이 달라지므로 unscaled로 센다.
        yield return new WaitForSecondsRealtime(delaySeconds);
        Despawn();
    }

    // 이미 완료된 미션이면 지금 치운다. 지연은 두지 않는다 —
    // 불러오자마자 몇 초 동안 사라졌어야 할 오브젝트가 보이면 안 되기 때문.
    void DespawnIfAlreadyCompleted() {
        MissionRuntime runtime = manager.FindMission(mission.MissionId);
        if (runtime == null || !runtime.IsCompleted) return;

        Despawn();
    }

    void Despawn() {
        foreach (GameObject target in targets) {
            if (target == null) continue; // 인스펙터에서 비워둔 칸이거나 이미 파괴된 것.

            if (destroyTargets) Destroy(target);
            else target.SetActive(false);
        }
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    // 어떤 오브젝트가 이 컴포넌트에 묶여 있는지 씬 뷰에서 바로 보이게 선으로 잇는다.
    // 목록이 길어지면 인스펙터만으로는 어느 문이 걸려 있는지 찾기 어렵다.
    void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.9f);

        foreach (GameObject target in targets) {
            if (target == null) continue;

            Gizmos.DrawLine(transform.position, target.transform.position);
            Gizmos.DrawWireSphere(target.transform.position, 0.3f);
        }
    }
#endif

    #endregion
}
