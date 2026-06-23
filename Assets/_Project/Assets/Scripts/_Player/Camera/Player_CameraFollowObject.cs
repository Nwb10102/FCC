// using UnityEngine;

// public class Player_CameraFollowObject : MonoBehaviour
// {
//     [Header("설정")]
//     [SerializeField] private Transform _playerTransform; // 플레이어의 트랜스폼.

//     [Header("플립 로테이션 상태")]
//     [SerializeField] private float _flipYRotationTime = 0.5f; // 플레이어가 바라보는 방향이 바뀔 때 카메라가 회전하는 각도.
//     private Coroutine _turneCorutine;
//     private PlayerPrefs player;

//     private bool _isFacingRight; // 플레이어가 오른쪽을 바라보고 있는지 여부.


//     private void Awake() {
//         player = _playerTransform.gameObject.GetComponent<PlayerPrefs>();
//         _isFacingRight = player.IsFacingRight;
//     }

//     private void CallTurn() {
//         _turneCorutine = StartCoroutine(Turn());
//     }
// }


// 4:20