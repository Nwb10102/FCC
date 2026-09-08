using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// 던전 입구인 전신 거울이 "클리어하고 나온 뒤 부서지는" 연출을 재생한다.
// 잠깐 뜸을 들였다가 점점 심하게 흔들리고, 팡 터지면서 멀쩡한 거울이 꺼지고 부서진 거울로 바뀐다.
//
// 연출만 담당하고 "언제 재생할지"는 DungeonGate 가 정한다. 클리어 판정과 보상 지급이 이미 게이트에
// 모여 있어서, 여기서 클리어 여부를 다시 물으면 판정이 두 군데로 갈라진다.
//
// 화면 요소를 코드로 조립하지 않는다는 규칙대로, 멀쩡한 거울·부서진 거울·파티클은 전부 씬(프리팹)에
// 만들어 두고 인스펙터로 연결한다. 이 스크립트는 연결받은 것을 켜고 끄고 흔들 뿐이다.
public class DungeonGateMirror : MonoBehaviour {
    #region 인스펙터 변수

    [Header("연결")]
    // **멀쩡한 전신 거울 오브젝트를 연결하세요.** 이게 흔들리고, 연출이 끝나면 꺼진다.
    public GameObject intactVisual;
    // **부서진 전신 거울 오브젝트를 연결하세요.** 평소 꺼져 있다가 연출이 끝나면 켜진다. 비워도 된다.
    public GameObject brokenVisual;
    // 터지는 순간 재생할 파티클. 비우면 HitVfx 의 파열 이펙트를 대신 쓴다.
    public ParticleSystem burstEffect;
    // 터질 때 화면을 흔들 임펄스 소스. 비우면 화면은 흔들리지 않는다.
    public CinemachineImpulseSource impulseSource;

    [Header("타이밍")]
    public float delayBeforeShake = 0.7f; // 밖으로 나온 뒤 흔들리기까지. 플레이어가 거울 쪽을 볼 틈을 준다.
    public float shakeDuration = 1.2f;    // 흔들리는 시간.
    public float burstHold = 0.06f;       // 터지기 직전 아주 잠깐 멈춘다. 정적이 있어야 "팡" 이 세게 느껴진다.

    [Header("흔들림")]
    public float shakeStrength = 0.09f;   // 최대 흔들림 폭(유닛). 처음엔 미세하게 시작해 끝에서 이 값까지 커진다.
    public float shakeFrequency = 26f;    // 초당 떨리는 횟수.
    public float shakeTilt = 4f;          // 최대 기울기(도).

    [Header("파열")]
    public float burstScale = 1.18f;      // 터지기 직전 부풀어 오르는 배율.
    public float burstShakeForce = 0.8f;  // impulseSource 가 연결돼 있을 때 화면을 흔들 힘.

    #endregion
    #region 런타임 변수

    Vector3 baseLocalPosition;
    Quaternion baseLocalRotation;
    Vector3 baseLocalScale;
    Coroutine routine;

    public bool IsBroken { get; private set; }

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (intactVisual == null) {
            Debug.LogError($"[DungeonGateMirror] '{name}' — intactVisual 이 연결되지 않아 거울 연출을 재생할 수 없습니다.", this);
            return;
        }

        // 흔들기 전 자세를 기억해 둔다. 연출이 끝나거나 중간에 꺼져도 여기로 되돌린다.
        Transform t = intactVisual.transform;
        baseLocalPosition = t.localPosition;
        baseLocalRotation = t.localRotation;
        baseLocalScale = t.localScale;

        if (brokenVisual != null) brokenVisual.SetActive(false);
    }

    #endregion
    #region 재생

    // 클리어하고 밖으로 나온 순간 DungeonGate 가 부른다.
    public void PlayBreak() {
        if (IsBroken || routine != null) return; // 사망·걸어 나가기가 겹쳐 두 번 불려도 한 번만 재생한다.
        if (intactVisual == null) return;

        routine = StartCoroutine(BreakRoutine());
    }

    // 이미 클리어된 상태로 씬을 다시 시작했을 때. 연출 없이 결과만 맞춘다 —
    // 불러올 때마다 거울이 다시 터지면 방금 깬 것처럼 보인다.
    public void SetBrokenImmediate() {
        if (routine != null) {
            StopCoroutine(routine);
            routine = null;
        }

        RestorePose();
        ApplyBrokenState();
    }

    // 연출은 전부 unscaled 기준이다. 히트스톱이 걸린 채로 던전을 나와도 거울이 얼어붙으면 안 된다.
    IEnumerator BreakRoutine() {
        yield return new WaitForSecondsRealtime(delayBeforeShake);

        Transform t = intactVisual.transform;
        float elapsed = 0f;

        while (elapsed < shakeDuration) {
            elapsed += Time.unscaledDeltaTime;

            // 흔들림을 처음부터 세게 주면 "부서지기 직전" 이 아니라 그냥 진동하는 물건으로 보인다.
            // 세기를 0에서 1로 키워 가며 점점 못 버티는 느낌을 만든다.
            float ramp = Mathf.Clamp01(elapsed / shakeDuration);
            float amount = ramp * ramp; // 뒤로 갈수록 급격히 심해지도록 제곱.
            float wave = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f);

            t.localPosition = baseLocalPosition + new Vector3(wave * shakeStrength * amount, 0f, 0f);
            t.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, wave * shakeTilt * amount);
            t.localScale = baseLocalScale * Mathf.Lerp(1f, burstScale, amount);

            yield return null;
        }

        // 터지기 직전의 정적.
        t.localPosition = baseLocalPosition;
        t.localRotation = baseLocalRotation;
        yield return new WaitForSecondsRealtime(burstHold);

        Burst();
        ApplyBrokenState();
        RestorePose(); // 다음에 다시 켜질 일이 있어도 부풀어 오른 채로 남지 않도록.

        routine = null;
    }

    #endregion
    #region 파열 · 상태 전환

    void Burst() {
        if (burstEffect != null) {
            burstEffect.transform.position = intactVisual.transform.position;
            burstEffect.Play();
        }
        else if (HitVfx.Instance != null) {
            // 전용 파티클을 아직 안 만들었어도 연출이 비어 보이지 않도록, 사망 파열을 빌려 쓴다.
            HitVfx.Instance.PlayDeath(intactVisual.transform.position);
        }

        if (impulseSource != null) impulseSource.GenerateImpulseWithForce(burstShakeForce);
    }

    void ApplyBrokenState() {
        IsBroken = true;
        if (intactVisual != null) intactVisual.SetActive(false);
        if (brokenVisual != null) brokenVisual.SetActive(true);
    }

    void RestorePose() {
        if (intactVisual == null) return;

        Transform t = intactVisual.transform;
        t.localPosition = baseLocalPosition;
        t.localRotation = baseLocalRotation;
        t.localScale = baseLocalScale;
    }

    #endregion
}
