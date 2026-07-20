using System;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player_move : MonoBehaviour
{
    #region 인스펙터 변수

    [Header("무브먼트 설정")]
    public float speed = 6f; // 플레이어 이동 스피드.
    public float maxSpeed = 5f; // 플레이어가 걸을 수 있는 최대 속도.
    public float jumpForce = 10f; // 점프 파워.

    private float koyoteTime = 0.2f; // 고요테 타임 설정
    private float koyoteTimeCounter; // 고요테 타임 카운터


    public bool useFacingRotation = false; // 플레이어가 바라보는 방향을 정하는 방식이 회전인지 스케일인지 여부. true면 회전, false면 스케일로 방향 전환.

    private bool IsFacingRight; // 플레이어가 오른쪽을 바라보고 있는지 여부.

    [Header("Ground Check")]
    public Transform groundCheck; // **'Player_Controller' 발밑에 빈 오브젝트를 만드세요.**
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer; // Ground 레이어 설정 필요

    private bool isGrounded;

    [Header("Coyote Time")]
    public float coyoteDuration = 0.15f; // 코요테 점프 허용 시간 (초)
    public float coyoteCounter; // 남은 코요테 시간을 체크할 타이머

    [Header("Jump Buffer")]
    public float jumpBufferDuration = 0.15f; // 착지 전에 미리 누른 점프 입력을 기억해두는 시간 (초)
    private float jumpBufferCounter; // 남은 점프 버퍼 시간을 체크할 타이머

    #endregion
    #region 외부 제어용 변수

    [HideInInspector]
    public bool isMovementLocked; // Player_Combat 등 외부 시스템이 공격 중 이동을 멈출 때 사용.

    #endregion
    #region 컴포넌트 변수
    Rigidbody2D rigid;
    Vector2 moveInput;

    #endregion

    #region 유니티 라이프 사이클
    void Awake() {
        rigid = GetComponent<Rigidbody2D>();
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;

        // IsFacingRight의 초기값이 실제 스프라이트/스케일의 방향과 어긋나면
        // 처음 반대 방향키를 눌렀을 때 방향 전환이 씹히는 문제가 있어, 시작 시 실제 상태와 동기화한다.
        IsFacingRight = transform.localScale.x > 0f;
    }

    void OnMove(InputValue value) {
        moveInput = value.Get<Vector2>();
    }

    void Update() {
        if (moveInput.x != 0) {
            TurnCheck();
        }

        SaveLoadSystem(); // 저장 & 불러오기
    }

    void FixedUpdate() {
        // 접지 판정/코요테타임/점프버퍼는 물리 힘이 실제로 적용되는 시점(FixedUpdate)과 맞춰야 한다.
        // Rigidbody2D의 Interpolation 때문에 Update()에서 읽는 transform 위치는 화면 표시용으로
        // 보간된 값이라 실제 물리 위치와 어긋날 수 있고, 이 오차가 점프 이륙/착지 순간의
        // isGrounded 판정을 한두 프레임 틀리게 만들어 점프가 살짝 걸리는 느낌으로 체감됐다.
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        coyoteJumpTime(); // 코요테
        JumpBufferTime(); // 점프 버퍼 (착지 전 미리 누른 점프 입력 처리)

        // immediateMove();
        SmoothMove();
    }

    #endregion

    #region 플레이어 움직임 관련 함수
    // 즉각적인 움직임 함수.
    void ImmediateMove() {
        rigid.linearVelocity = new Vector2(moveInput.x * speed, rigid.linearVelocity.y);
    }

    // 부드러운 움직임 함수.
    void SmoothMove() {
        if (isMovementLocked) {
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocityY);
            return;
        }

        rigid.AddForce(moveInput, ForceMode2D.Impulse);
        // Debug.Log(moveInput);

        // 최고 속도 관리.
        if (rigid.linearVelocityX >= maxSpeed) {
            rigid.linearVelocity = new Vector2(maxSpeed, rigid.linearVelocityY);
        }
        else if (rigid.linearVelocityX <= maxSpeed * (-1)) {
            rigid.linearVelocity = new Vector2(maxSpeed * (-1), rigid.linearVelocityY);
        }
    }

// --TODO(박찬) : 점프 구현 (고요테 점프로 구현할 예정)
    void coyoteJumpTime() {
        if (isGrounded) {
            coyoteCounter = coyoteDuration;
        } else {
            coyoteCounter -= Time.fixedDeltaTime;
        }
    }

    void OnJump(InputValue value) {
        if (!value.isPressed) return;

        if (coyoteCounter > 0f) {
            ExecuteJump();
        } else {
            jumpBufferCounter = jumpBufferDuration; // 공중에서 미리 누른 점프 입력을 버퍼에 저장, 착지 시 바로 점프.
        }
    }

    void ExecuteJump() {
        isGrounded = false;
        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, 0f); // 기존 속도를 초기화하여 키 씹힘 방지.

        // rigid.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        rigid.AddForceY(jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f; // 타이머를 0으로 만들어 이단 점프 방지.
        jumpBufferCounter = 0f; // 버퍼 소모.
    }

    void JumpBufferTime() {
        if (jumpBufferCounter <= 0f) return;

        jumpBufferCounter -= Time.fixedDeltaTime;

        if (isGrounded) {
            ExecuteJump();
        }
    }

    // 점프 판정 범위
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }

    #endregion
    #region 카메라 관련 함수
    
    // --TODO(김지훈) : 카메라가 플레이어의 행동에 따라 움직이도록 하는 함수.
    void CameraControl() {
        
    }

    // 플레이어가 바라보는 방향을 체크해주고 방향이 바뀌면 플레이어의 방향을 바꿔주는 함수.
    void TurnCheck() {
        if (moveInput.x > 0 && !IsFacingRight) {
            Turn();
        }
        else if (moveInput.x < 0 && IsFacingRight) {
            Turn();
        }
    }

    private void Turn(){
        IsFacingRight = !IsFacingRight;
        ApplyFacing();
    }

    // 현재 IsFacingRight 상태를 현재 모드(회전/스케일)에 맞게 트랜스폼에 반영.
    // 모드가 전환되는 순간(카메라 영역 진입/이탈)에도 호출해서 반대 축에 남아있는
    // 이전 모드의 값(스케일 -1 또는 회전 180)이 겹쳐 방향이 어긋나는 것을 방지한다.
    public void ApplyFacing() {
        float yRotation = IsFacingRight ? 0f : 180f;
        float xScale = IsFacingRight ? 1f : -1f;

        if (useFacingRotation) {
            Vector3 rotator = new Vector3(transform.rotation.eulerAngles.x, yRotation, transform.rotation.eulerAngles.z);
            transform.rotation = Quaternion.Euler(rotator);

            // 스케일 모드에서 넘어왔을 때 남아있을 수 있는 음수 스케일을 중립화.
            Vector3 neutralScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            transform.localScale = neutralScale;
        } else {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            Vector3 newScale = new Vector3(xScale, transform.localScale.y, transform.localScale.z);
            transform.localScale = newScale;
        }
    }

    #endregion

    #region 플레이어 데이터 저장/ 불러오기 관련 함수

    public void SaveGame()
    {
        SaveData data = new SaveData();


        data.posX = transform.position.x;
        data.posY = transform.position.y;

        SaveManager.Instance.Save(data);
    }

    public void LoadGame()
    {
        SaveData data = SaveManager.Instance.Load();

        if (data == null)
            return;


        transform.position =
            new Vector2(data.posX, data.posY);
    }

    public void SaveLoadSystem()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveGame();
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            LoadGame();
        }
    }

    #endregion
}