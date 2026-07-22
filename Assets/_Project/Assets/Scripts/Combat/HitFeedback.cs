using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// 타격 시 공통으로 재생되는 피드백(히트스톱 + 카메라 쉐이크)을 담당하는 싱글턴.
// HitReactor가 피격 이벤트를 받을 때마다 PlayHit()을 호출한다.
public class HitFeedback : MonoBehaviour {
    public static HitFeedback Instance;

    [Header("히트스톱")]
    public float hitStopDuration = 0.05f; // 정지 지속 시간 (Realtime 기준, timeScale 영향 없음).
    [Range(0f, 1f)]
    public float hitStopTimeScale = 0.02f; // 정지 중 적용할 타임스케일.

    [Header("카메라 쉐이크")]
    public float shakeForce = 1.5f; // CinemachineImpulseSource에 전달할 힘.

    CinemachineImpulseSource impulseSource;
    Coroutine hitStopRoutine;
    float cachedTimeScale = 1f; // 히트스톱 시작 전의 timeScale. GameSpeedController 등 외부에서 바꾼 배속을 그대로 복원하기 위해 캐싱.

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }

        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void PlayHit() {
        TriggerHitStop();
        TriggerShake();
    }

    void TriggerHitStop() {
        if (hitStopRoutine == null) {
            cachedTimeScale = Time.timeScale; // 이미 정지 중이 아닐 때만 원래 배속을 새로 캐싱.
        }
        else {
            StopCoroutine(hitStopRoutine);
        }

        hitStopRoutine = StartCoroutine(HitStopRoutine());
    }

    IEnumerator HitStopRoutine() {
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = cachedTimeScale;
        hitStopRoutine = null;
    }

    void TriggerShake() {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(shakeForce);
    }
}
