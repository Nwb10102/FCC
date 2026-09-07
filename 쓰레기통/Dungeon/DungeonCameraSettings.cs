using System;
using UnityEngine;

// 방(DungeonRoomCamera)과 방 안 구역(DungeonCameraZone)이 공유하는 카메라 설정 묶음.
// 두 곳이 같은 필드를 갖고 같은 방식으로 요청을 만들기 때문에 한 곳에 모은다.
//
// bounds 를 비우거나 orthographicSize / damping 을 0 으로 두면 "상위 레이어(방→구역) 값을 그대로 물려받음"
// 을 뜻한다. 그래서 구역은 "줌만 바꾸고 경계는 방 것을 그대로 쓰기" 같은 부분 오버라이드가 가능하다.
[Serializable]
public class DungeonCameraSettings {
    [Tooltip("플레이어 추적 / 고정점 응시")]
    public DungeonCameraFollowMode mode = DungeonCameraFollowMode.FollowPlayer;

    // **mode 가 '고정점 응시' 일 때 카메라가 바라볼 빈 오브젝트를 연결하세요.**
    public Transform fixedPoint;

    // 카메라가 어디에서 어디까지 움직일지. Confiner2D 경계. **비우면 상위 값 유지.**
    public Collider2D bounds;

    [Min(0f)]
    public float orthographicSize = 0f; // 줌. 0 이면 상위 값 유지. 값이 작을수록 화면이 확대된다.

    public Vector2 targetOffset = Vector2.zero; // 화면 중심을 플레이어에서 얼마나 밀어낼지.

    [Min(0f)]
    public float damping = 0f; // 따라오는 속도. 0 이면 상위 값 유지. 클수록 느리고 부드럽다.

    [Min(0f)]
    public float transitionDuration = 0.5f; // 이 상태로 넘어가는 데 걸리는 시간.

    public DungeonCameraRequest ToRequest() {
        return new DungeonCameraRequest {
            mode = mode,
            fixedPoint = fixedPoint,
            bounds = bounds,
            orthographicSize = orthographicSize,
            targetOffset = targetOffset,
            damping = damping,
            transitionDuration = transitionDuration,
        };
    }
}
