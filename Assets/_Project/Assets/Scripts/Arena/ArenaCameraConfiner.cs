using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// Player_Camera에 부착되어 던전 방 전환 시 Confiner 경계 교체를 완충한다.
// Confiner의 Damping은 경계가 바뀐 첫 프레임의 보정은 완충하지 못하고(이전 프레임 보정값이 0에
// 가까워 코너-각도 완충 분기가 발동하지 않음) 이후의 연속 보정만 완충하므로, 경계를 바꾼 직후
// 잠깐 Confiner를 꺼 뒀다가(그동안은 PositionComposer의 팔로우 댐핑만으로 카메라가 자연스럽게
// 새 경계 쪽으로 다가감) 다시 켜는 방식으로 스냅을 줄인다.
public class ArenaCameraConfiner : MonoBehaviour {
    #region 인스펙터 변수

    [Header("전환 속도")]
    [Range(0f, 1.5f)] public float reengageDelay = 0.35f; // 방 전환 후 카메라를 다시 가두기까지 걸리는 시간. 클수록 부드럽다.

    #endregion
    #region 컴포넌트 변수

    CinemachineConfiner2D confiner;
    Coroutine transitionRoutine;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        confiner = GetComponent<CinemachineConfiner2D>();
    }

    #endregion
    #region 경계 전환

    public void SetBounds(Collider2D bounds) {
        if (confiner == null || bounds == null) return;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(SwapRoutine(bounds));
    }

    IEnumerator SwapRoutine(Collider2D bounds) {
        confiner.enabled = false;
        confiner.BoundingShape2D = bounds;

        yield return new WaitForSeconds(reengageDelay);

        confiner.enabled = true;
        confiner.InvalidateBoundingShapeCache();
        transitionRoutine = null;
    }

    #endregion
}
