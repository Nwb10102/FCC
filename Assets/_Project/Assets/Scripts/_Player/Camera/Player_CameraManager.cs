// using UnityEngine;
// using System;
// using Unity.VisualScripting;
// using Unity.Cinemachine;

// public class Player_CameraManager : MonoBehaviour
// {
//     public static Player_CameraManager instance;

//     [SerializeField] private CinemachineVirtualCamera[] allVirtualCameras; // 씬에 존재하는 모든 가상 카메라들을 담는 배열.

//     [Header("플레이어가 떨어지거나 점프뛸때 카메라가 가속")]
//     [SerializeField] private float fallPanAmount = 0.25f;
//     [SerializeField] private float jumpPanAmount = 0.25f;
//     public float fallSpeedYDampingChangeThreshold = -15f;

//     //읽기 쓰기를 다르게 해주는 프로퍼티 문법
//     //get;: 밖에서 이 변수의 값이 현재 true인지 false인지 읽어갈 수 있게 해줌
//     //private set;: 하지만 이 값을 수정(변경)하는 것은 오직 이 코드가 작성된 스크립트 안에서만 가능하도록 해둠
//     public bool IsLerpingYDamping { get; private set; }
//     public bool LerpedFromPlayerFalling { get; set; }

//     private Coroutine lerpYPanCoroutine;

//     private CinemachineVirtualCamera currentCamera;
//     private CinemachinePositionComposer currentCameraTransposer;

//     private float normYPanAmount;

//     private void Awake() {
//         if (instance == null) {
//             instance = this;
//         }

//         for (int i = 0; i<allVirtualCameras.Length; i++) {
//             if (allVirtualCameras[i].isActiveAndEnabled) {
//                 //활성화된 카메라로 설정
//                 currentCamera = allVirtualCameras[i];

//                 //활성화된 카메라의 트랜스포저 컴포넌트를 가져옴
//                 currentCameraTransposer = currentCamera.GetCinemachineComponent<CinemachinePositionComposer>();
//             }
//         }
//     }

//     #region Y축 관련 함수

//     public void LerpYDaming(bool isPlayerFalling) {
//         lerpYPanCoroutine = StartCoroutine();
//     }

//     private IEnumeratir LerpYAction(bool isPlayerFalling) {
//         IsLerpingYDamping = true;

//         float startDampAmount
//     }



//     #endregion
// }



// 6:56