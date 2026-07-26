using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class Player_AreaCameraController : MonoBehaviour {
    public enum CameraAreaType {
        [InspectorName("플레이어 위치 기반")] PlayerFollow,
        [InspectorName("객체 고정")] FixedTarget
    }

    [Header("Camera Area Type")]
    public CameraAreaType areaType = CameraAreaType.PlayerFollow;
    [Tooltip("areaType이 '객체 고정'일 때 카메라가 고정될 대상")]
    public Transform fixedTarget;

    [Header("Camera Settings")]
    public float X_TargetOffset = 0f;
    public float Y_TargetOffset = 0f;
    public float LensSize = 5f;
    public float Damping = 5f;
    public float TransitionDuration = 0.5f;

    public bool useFacingRotation = false; // 플레이어가 바라보는 방향을 정하는 방식이 회전인지 스케일인지 여부. true면 회전, false면 스케일로 방향 전환.


    [Header("References")]
    [Tooltip("비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private CinemachineCamera targetCamera;
    [SerializeField] private GameObject player; // 플레이어 오브젝트 참조 (필요한 경우)
    [SerializeField] private string playerTag = "Player";

    private CinemachinePositionComposer _positionComposer;
    private Vector3 _originalOffset;
    private Vector3 _originalDamping;
    private float _originalLensSize;

    private Coroutine _transitionCoroutine;

    private Player_move _playerMove;
    private bool _originalPlayerFacingRotation;
    private Transform _originalFollow;

    private void Awake() {
        if (targetCamera == null) {
            var go = GameObject.Find("Player_Camera");
            if (go != null)
                targetCamera = go.GetComponent<CinemachineCamera>();
        }

        if (player == null) {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj;
        }

        if (player != null)
            _playerMove = player.GetComponent<Player_move>();

        if (targetCamera == null) {
            Debug.LogWarning($"[{name}] 'Player_Camera' GameObject를 찾을 수 없습니다.");
            return;
        }

        _positionComposer = targetCamera.GetComponent<CinemachinePositionComposer>();
        if (_positionComposer == null) {
            Debug.LogWarning($"[{name}] CinemachinePositionComposer를 찾을 수 없습니다.");
            return;
        }

        _originalOffset = _positionComposer.TargetOffset;
        _originalDamping = _positionComposer.Damping;
        _originalLensSize = targetCamera.Lens.OrthographicSize;
        _originalFollow = targetCamera.Follow;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag(playerTag) || _positionComposer == null) return;

        if (useFacingRotation && _playerMove != null) {
            _originalPlayerFacingRotation = _playerMove.useFacingRotation;
            _playerMove.useFacingRotation = true;
            _playerMove.ApplyFacing(); // 모드 전환 즉시 트랜스폼을 새 모드에 동기화 (이전 모드의 스케일/회전 잔여값 제거)
        }

        if (areaType == CameraAreaType.FixedTarget) {
            if (fixedTarget != null) {
                targetCamera.Follow = fixedTarget;
            } else {
                Debug.LogWarning($"[{name}] areaType이 '객체 고정'이지만 fixedTarget이 비어있습니다.");
            }
        }

        Vector3 targetOffset = new Vector3(X_TargetOffset, Y_TargetOffset, _originalOffset.z);
        float lensSize = LensSize > 0 ? LensSize : _originalLensSize;
        Vector3 targetDamping = new Vector3(Damping, Damping, _originalDamping.z); // Z는 카메라 거리축이라 원본 유지
        StartTransition(targetOffset, lensSize, targetDamping);
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag(playerTag) || _positionComposer == null) return;

        if (useFacingRotation && _playerMove != null) {
            _playerMove.useFacingRotation = _originalPlayerFacingRotation;
            _playerMove.ApplyFacing(); // 모드 전환 즉시 트랜스폼을 새 모드에 동기화 (이전 모드의 스케일/회전 잔여값 제거)
        }

        if (areaType == CameraAreaType.FixedTarget) {
            targetCamera.Follow = _originalFollow;
        }

        StartTransition(_originalOffset, _originalLensSize, _originalDamping);
    }

    private void StartTransition(Vector3 targetOffset, float lensSize, Vector3 targetDamping) {
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(TransitionRoutine(targetOffset, lensSize, targetDamping));
    }

    private IEnumerator TransitionRoutine(Vector3 targetOffset, float targetLensSize, Vector3 targetDamping) {
        Vector3 startOffset = _positionComposer.TargetOffset;
        Vector3 startDamping = _positionComposer.Damping;
        float startLensSize = targetCamera.Lens.OrthographicSize;
        float elapsed = 0f;

        while (elapsed < TransitionDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / TransitionDuration);

            _positionComposer.TargetOffset = Vector3.Lerp(startOffset, targetOffset, t);
            _positionComposer.Damping = Vector3.Lerp(startDamping, targetDamping, t);

            var lens = targetCamera.Lens;
            lens.OrthographicSize = Mathf.Lerp(startLensSize, targetLensSize, t);
            targetCamera.Lens = lens;

            yield return null;
        }

        _positionComposer.TargetOffset = targetOffset;
        _positionComposer.Damping = targetDamping;

        var finalLens = targetCamera.Lens;
        finalLens.OrthographicSize = targetLensSize;
        targetCamera.Lens = finalLens;
    }



#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (!TryGetComponent<Collider2D>(out var col)) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif
}
