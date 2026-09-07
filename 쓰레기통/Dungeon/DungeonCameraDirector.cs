using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum DungeonCameraFollowMode {
    [InspectorName("플레이어 추적")] FollowPlayer,
    [InspectorName("고정점 응시")] FixedPoint
}

// 방 하나 또는 방 안 구역 하나가 카메라에 요구하는 상태 한 건.
// bounds 가 null 이거나 orthographicSize / damping 이 0 이면 "아래 레이어 값을 그대로 물려받음" 을 뜻한다.
public struct DungeonCameraRequest {
    public DungeonCameraFollowMode mode;
    public Transform fixedPoint;       // mode 가 FixedPoint 일 때 카메라가 응시할 지점.
    public Collider2D bounds;          // 카메라 이동 범위(Confiner2D). null 이면 유지.
    public float orthographicSize;     // 줌. 0 이면 유지. 작을수록 확대.
    public Vector2 targetOffset;       // 화면 중심 오프셋.
    public float damping;              // 0 이면 유지.
    public float transitionDuration;   // 0 이면 기본값(0.5초).
}

// 던전 방·구역이 요구하는 카메라 상태를 한 곳으로 모아 Player_Camera 에 적용하는 조정자.
//
// 방과 구역이 각자 코루틴으로 Lens/Confiner 를 만지면 전환이 서로를 덮어써서 줌이 튄다.
// 그래서 상태를 "방 1개(최하위 레이어) + 구역 스택" 으로 쌓아 두고, 아래에서 위로 훑어 최종 상태 하나를
// 만든 뒤 여기서만 트랜지션을 돌린다. 전환 감각은 _Player/Camera/Player_AreaCameraController 와 맞춘다.
public class DungeonCameraDirector : MonoBehaviour {
    #region 싱글턴

    public static DungeonCameraDirector Instance { get; private set; }

    // 던전 입장 시점에 처음 필요해지므로 그때 만든다. 씬에 미리 배치해 두면 그 인스턴스를 그대로 쓴다.
    public static DungeonCameraDirector GetOrCreate() {
        if (Instance != null) return Instance;

        var go = new GameObject("DungeonCameraDirector");
        Instance = go.AddComponent<DungeonCameraDirector>();
        return Instance;
    }

    #endregion
    #region 컴포넌트 변수

    CinemachineCamera cam;
    CinemachinePositionComposer composer;
    CinemachineConfiner2D confiner;
    CinemachineImpulseSource impulseSource; // 던전 입장 연출용 흔들림. HitFeedback과 같은 방식(소스 위치와 무관하게 화면 전체가 흔들림)으로 쓴다.
    Volume distortionVolume; // 입장 연출 전용 화면 외곡(Lens Distortion). 코드로만 생성해 씬에 미리 배치할 필요가 없다.
    LensDistortion lensDistortion;

    // 던전 입장 전 상태. 던전을 빠져나갈 때 이 값으로 되돌린다.
    Transform defaultFollow;
    Collider2D defaultBounds;
    float defaultOrthographicSize;
    Vector3 defaultOffset;
    Vector3 defaultDamping;
    bool captured;

    #endregion
    #region 상태 스택

    DungeonRoom currentRoom;
    DungeonCameraRequest roomRequest;
    bool hasRoomRequest;

    // key = 요청을 올린 컴포넌트(방/구역). 이것으로 정확히 짚어 뺀다.
    readonly List<Component> zoneKeys = new();
    readonly List<DungeonCameraRequest> zoneRequests = new();

    Coroutine transition;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Bind();

        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null) impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();

        EnsureDistortionVolume();
    }

    // Lens Distortion 하나만 담은 전역 Volume을 코드로 만든다. 씬에 미리 배치해 둘 필요가 없고,
    // intensity 기본값이 0이라 평소(연출이 없을 때)에는 화면에 아무 영향도 주지 않는다.
    // **Main Camera의 URP 카메라 데이터에서 Render Post-Processing이 켜져 있어야 실제로 보인다.**
    void EnsureDistortionVolume() {
        if (distortionVolume != null) return;

        var go = new GameObject("DungeonEnterDistortionVolume");
        go.transform.SetParent(transform, false);

        distortionVolume = go.AddComponent<Volume>();
        distortionVolume.isGlobal = true;
        distortionVolume.priority = 100f;
        distortionVolume.weight = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        lensDistortion = profile.Add<LensDistortion>(true);
        lensDistortion.intensity.overrideState = true;
        lensDistortion.intensity.value = 0f;
        lensDistortion.scale.overrideState = true;
        lensDistortion.scale.value = 1f;

        distortionVolume.profile = profile;
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // Player_Camera 를 찾아 컴포넌트를 잡고, 입장 전 상태를 한 번만 캡처한다.
    void Bind() {
        var go = GameObject.Find("Player_Camera");
        if (go == null) {
            Debug.LogError("[DungeonCameraDirector] 씬에서 'Player_Camera' 를 찾지 못했습니다. 카메라 전환이 동작하지 않습니다.");
            return;
        }

        cam = go.GetComponent<CinemachineCamera>();
        composer = go.GetComponent<CinemachinePositionComposer>();
        confiner = go.GetComponent<CinemachineConfiner2D>();

        if (cam != null && !captured) {
            defaultFollow = cam.Follow;
            defaultOrthographicSize = cam.Lens.OrthographicSize;
            defaultBounds = confiner != null ? confiner.BoundingShape2D as Collider2D : null;
            defaultOffset = composer != null ? composer.TargetOffset : Vector3.zero;
            defaultDamping = composer != null ? composer.Damping : Vector3.zero;
            captured = true;
        }
    }

    #endregion
    #region 방 · 구역 등록

    // 플레이어가 새 방에 들어오면 그 방 기본 카메라를 최하위 레이어로 세운다. 이전 방 것은 자동으로 밀려난다.
    public void SetRoom(DungeonRoom room, DungeonCameraRequest request) {
        currentRoom = room;
        roomRequest = request;
        hasRoomRequest = true;
        Reapply();
    }

    // 방을 벗어날 때. 이미 다른 방으로 넘어간 뒤 늦게 도착한 호출은 무시한다.
    public void ClearRoom(DungeonRoom room) {
        if (currentRoom != room) return;

        currentRoom = null;
        hasRoomRequest = false;
        Reapply();
    }

    public void PushZone(Component key, DungeonCameraRequest request) {
        int i = zoneKeys.IndexOf(key);
        if (i >= 0) {
            zoneRequests[i] = request; // 같은 구역이 재진입하면 갱신만 한다.
        }
        else {
            zoneKeys.Add(key);
            zoneRequests.Add(request);
        }
        Reapply();
    }

    public void PopZone(Component key) {
        int i = zoneKeys.IndexOf(key);
        if (i < 0) return;

        zoneKeys.RemoveAt(i);
        zoneRequests.RemoveAt(i);
        Reapply();
    }

    // 던전을 완전히 빠져나갈 때. 쌓인 요청을 전부 비우고 입장 전 상태로 되돌린다.
    public void ResetToDefault() {
        currentRoom = null;
        hasRoomRequest = false;
        zoneKeys.Clear();
        zoneRequests.Clear();
        Reapply();
    }

    #endregion
    #region 입장 연출

    // 던전 게이트가 생성 직전에 호출한다. 화면이 플레이어를 향해 확대되며 흔들림·외곡이 강해졌다가 잦아들고,
    // duration이 끝나면 targetOrthographicSize에 딱 멈춰 선다(그 뒤 DungeonGate가 실제 생성·워프를 진행하고
    // SetRoom으로 다시 축소시킨다). 호출부에서 yield return으로 끝날 때까지 기다려야 한다.
    public Coroutine PlayEnterZoom(float duration, float targetOrthographicSize, float maxShakeForce, float maxDistortion) {
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(EnterZoomRoutine(Mathf.Max(0.01f, duration), targetOrthographicSize, maxShakeForce, maxDistortion));
        return transition;
    }

    // 흔들림·외곡 세기 곡선. 0→1 구간(riseEnd 비율)에서 강해졌다가, 남은 구간에서 0까지 잦아든다.
    // 확대가 끝나는 순간(=화면이 가장 덮인 순간)에 정확히 0으로 떨어지도록 맞춰서, 실제 생성·워프가
    // 일어나는 뒤가 고요한 정지 화면처럼 보이게 한다.
    static float ShakeEnvelope(float k) {
        const float riseEnd = 0.6f;
        return k < riseEnd
            ? Mathf.SmoothStep(0f, 1f, k / riseEnd)
            : Mathf.SmoothStep(1f, 0f, (k - riseEnd) / (1f - riseEnd));
    }

    IEnumerator EnterZoomRoutine(float duration, float targetOrtho, float maxShakeForce, float maxDistortion) {
        if (cam == null) yield break;

        const float shakeInterval = 0.07f; // 이 간격으로 임펄스를 계속 쏴서 끊기지 않는 떨림처럼 보이게 한다.
        float startOrtho = cam.Lens.OrthographicSize;
        // 플레이어를 향해 확대되도록, 진입 시점에 걸려 있던 화면 오프셋(Player_AreaCameraController 등이
        // 남겨둔 값)을 0(=플레이어가 화면 정중앙)으로 되돌리며 확대한다. 안 그러면 오프셋만큼 벗어난
        // 허공을 향해 확대돼 버린다.
        Vector3 startOffset = composer != null ? composer.TargetOffset : Vector3.zero;
        // Damping(예: Y가 2로 걸려 있는 상태)이 남아있으면 카메라가 플레이어를 뒤늦게 쫓아가서 확대·재중앙
        // 이 흐물흐물하게 보인다. 포탈로 빨려들어가는 동안만 0으로 눌러 즉시 반응하게 한다.
        Vector3 startDamping = composer != null ? composer.Damping : Vector3.zero;
        float t = 0f;
        float nextShakeAt = 0f;

        while (t < duration) {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float zoomK = Mathf.SmoothStep(0f, 1f, k);
            float shakeK = ShakeEnvelope(k);

            var lens = cam.Lens;
            lens.OrthographicSize = Mathf.Lerp(startOrtho, targetOrtho, zoomK);
            cam.Lens = lens;

            if (composer != null) {
                composer.TargetOffset = Vector3.Lerp(startOffset, Vector3.zero, zoomK);
                composer.Damping = Vector3.Lerp(startDamping, Vector3.zero, zoomK);
            }

            if (impulseSource != null && maxShakeForce > 0f && t >= nextShakeAt) {
                nextShakeAt = t + shakeInterval;
                impulseSource.GenerateImpulseWithForce(maxShakeForce * shakeK);
            }

            if (lensDistortion != null) lensDistortion.intensity.value = -maxDistortion * shakeK;

            yield return null;
        }

        var finalLens = cam.Lens;
        finalLens.OrthographicSize = targetOrtho;
        cam.Lens = finalLens;
        if (composer != null) {
            composer.TargetOffset = Vector3.zero;
            composer.Damping = Vector3.zero;
        }
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        transition = null;
    }

    #endregion
    #region 해석 · 적용

    void Reapply() {
        if (cam == null) return;

        DungeonCameraFollowMode mode = DungeonCameraFollowMode.FollowPlayer;
        Transform fixedPoint = null;
        Collider2D bounds = defaultBounds;
        float ortho = defaultOrthographicSize;
        Vector2 offset = defaultOffset;
        float damping = defaultDamping.x;
        float duration = 0.5f;
        bool any = false;

        // 최하위(방) → 구역 스택 순으로 훑으며 지정된 값만 덮어쓴다.
        if (hasRoomRequest) {
            Merge(roomRequest, ref mode, ref fixedPoint, ref bounds, ref ortho, ref offset, ref damping, ref duration);
            any = true;
        }
        for (int i = 0; i < zoneRequests.Count; i++) {
            Merge(zoneRequests[i], ref mode, ref fixedPoint, ref bounds, ref ortho, ref offset, ref damping, ref duration);
            any = true;
        }

        if (!any) {
            // 던전 밖: 캡처해 둔 원본으로 되돌린다.
            ApplyImmediateTargets(defaultFollow, defaultBounds);
            StartTransition(defaultOrthographicSize, defaultOffset, defaultDamping, 0.5f);
            return;
        }

        Transform follow = mode == DungeonCameraFollowMode.FixedPoint && fixedPoint != null ? fixedPoint : defaultFollow;
        ApplyImmediateTargets(follow, bounds);
        StartTransition(ortho,
            new Vector3(offset.x, offset.y, defaultOffset.z),
            new Vector3(damping, damping, defaultDamping.z),
            duration);
    }

    void Merge(DungeonCameraRequest r, ref DungeonCameraFollowMode mode, ref Transform fixedPoint, ref Collider2D bounds,
               ref float ortho, ref Vector2 offset, ref float damping, ref float duration) {
        mode = r.mode;
        fixedPoint = r.fixedPoint;
        offset = r.targetOffset;
        if (r.bounds != null) bounds = r.bounds;
        if (r.orthographicSize > 0f) ortho = r.orthographicSize;
        if (r.damping > 0f) damping = r.damping;
        if (r.transitionDuration > 0f) duration = r.transitionDuration;
    }

    // Follow 대상과 Confiner 경계는 보간할 수 없으므로 즉시 교체한다.
    void ApplyImmediateTargets(Transform follow, Collider2D bounds) {
        if (cam.Follow != follow) cam.Follow = follow;

        if (confiner != null && confiner.BoundingShape2D as Collider2D != bounds) {
            confiner.BoundingShape2D = bounds;
            confiner.InvalidateBoundingShapeCache();
        }
    }

    void StartTransition(float ortho, Vector3 offset, Vector3 damping, float duration) {
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(TransitionRoutine(ortho, offset, damping, Mathf.Max(0.01f, duration)));
    }

    // Player_AreaCameraController.TransitionRoutine 과 같은 SmoothStep 보간. 히트스톱 중에는 함께 멈추도록 Time.deltaTime 기준.
    IEnumerator TransitionRoutine(float targetOrtho, Vector3 targetOffset, Vector3 targetDamping, float duration) {
        float startOrtho = cam.Lens.OrthographicSize;
        Vector3 startOffset = composer != null ? composer.TargetOffset : Vector3.zero;
        Vector3 startDamping = composer != null ? composer.Damping : Vector3.zero;
        float t = 0f;

        while (t < duration) {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);

            var lens = cam.Lens;
            lens.OrthographicSize = Mathf.Lerp(startOrtho, targetOrtho, k);
            cam.Lens = lens;

            if (composer != null) {
                composer.TargetOffset = Vector3.Lerp(startOffset, targetOffset, k);
                composer.Damping = Vector3.Lerp(startDamping, targetDamping, k);
            }
            yield return null;
        }

        var finalLens = cam.Lens;
        finalLens.OrthographicSize = targetOrtho;
        cam.Lens = finalLens;

        if (composer != null) {
            composer.TargetOffset = targetOffset;
            composer.Damping = targetDamping;
        }
        transition = null;
    }

    #endregion
}
