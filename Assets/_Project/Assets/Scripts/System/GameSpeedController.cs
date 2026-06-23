using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    void Update()
    {
        // 1번 누르면 슬로우 모션 (0.5배속)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetGameSpeed(0.5f);
        }
        
        // 2번 누르면 정상 속도 (1배속)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetGameSpeed(1.0f);
        }

        // 3번 누르면 배속 (2배속)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetGameSpeed(2.0f);
        }
        
        // 스페이스바 누르면 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.timeScale == 0f)
            {
                SetGameSpeed(1.0f); // 다시 재생
            }
            else
            {
                SetGameSpeed(0f); // 일시정지
            }
        }
    }

    void SetGameSpeed(float speed)
    {
        Time.timeScale = speed;
        
        // 오디오 피치(배속)도 게임 속도에 맞추고 싶다면 아래 주석을 해제해봐!
        // AudioListener.pitch = speed; 
        
        Debug.Log($"현재 게임 속도: {speed}배속");
    }
}