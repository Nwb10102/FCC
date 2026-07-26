using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// 타격 시 공통으로 재생되는 피드백(히트스톱 + 카메라 쉐이크)을 담당하는 싱글턴.
// HitReactor가 피격/사망 이벤트를 받을 때마다 PlayHit()을 호출한다.
public class HitFeedback : MonoBehaviour {
    public static HitFeedback Instance;

    #region 인스펙터 변수

    [Header("히트스톱")]
    public float minHitStopDuration = 0.02f; // 약한 타격의 정지 시간 (Realtime 기준, timeScale 영향 없음).
    public float maxHitStopDuration = 0.05f; // referenceDamage 이상일 때의 정지 시간.
    public float killHitStopDuration = 0.14f; // 처치했을 때 화면을 확 붙잡는 시간.
    public int referenceDamage = 50; // 이 피해량에서 maxHitStopDuration에 도달한다.
    [Range(0f, 1f)]
    public float hitStopTimeScale = 0.02f; // 정지 중 적용할 타임스케일.

    [Header("카메라 쉐이크")]
    public float shakeForce = 0.8f; // 일반 타격 시 CinemachineImpulseSource에 전달할 힘.
    public float killShakeForce = 1.6f; // 처치 시의 힘.

    #endregion
    #region 컴포넌트 변수

    CinemachineImpulseSource impulseSource;
    Coroutine hitStopRoutine;
    float cachedTimeScale = 1f; // 히트스톱 시작 전의 timeScale. GameSpeedController 등 외부에서 바꾼 배속을 그대로 복원하기 위해 캐싱.

    #endregion
    #region 유니티 라이프 사이클

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

    #endregion
    #region 피드백 재생

    // hitPoint: 타격이 발생한 월드 좌표. hitDir: 공격자 → 피격자 방향(정규화).
    public void PlayHit(Vector2 hitPoint, Vector2 hitDir, int damage, bool isLethal) {
        TriggerHitStop(GetHitStopDuration(damage, isLethal));
        TriggerShake(hitPoint, hitDir, isLethal ? killShakeForce : shakeForce);
    }

    float GetHitStopDuration(int damage, bool isLethal) {
        if (isLethal) return killHitStopDuration;

        float t = referenceDamage > 0 ? Mathf.Clamp01((float)damage / referenceDamage) : 1f;
        return Mathf.Lerp(minHitStopDuration, maxHitStopDuration, t);
    }

    void TriggerHitStop(float duration) {
        if (hitStopRoutine == null) {
            cachedTimeScale = Time.timeScale; // 이미 정지 중이 아닐 때만 원래 배속을 새로 캐싱.
        }
        else {
            StopCoroutine(hitStopRoutine);
        }

        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    IEnumerator HitStopRoutine(float duration) {
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = cachedTimeScale;
        hitStopRoutine = null;
    }

    // 타격 방향으로 화면이 밀리도록 방향성 임펄스를 발생시킨다. 무지향 흔들림보다 타격이 훨씬 시원하게 읽힌다.
    void TriggerShake(Vector2 hitPoint, Vector2 hitDir, float force) {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulseAtPositionWithVelocity(hitPoint, hitDir * force);
    }

    #endregion
}
