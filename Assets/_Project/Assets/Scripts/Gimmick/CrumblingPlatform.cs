using System.Collections;
using UnityEngine;

// 밟으면 잠시 후 무너져 사라졌다가, 시간이 지나면 다시 나타나는 발판.
public class CrumblingPlatform : MonoBehaviour {
    #region 인스펙터 변수

    [Header("타이밍")]
    public float fallDelay = 0.4f;   // 밟은 뒤 무너지기까지.
    public float respawnDelay = 2f;  // 다시 나타나기까지.

    [Header("감지")]
    // 다른 트리거 볼륨(DungeonRoom·DungeonFallZone·DialogueTriggerZone 등)은 전부 태그를 필드로 노출한다.
    // 여기만 리터럴로 박혀 있어서, 플레이어 태그를 바꾸면 이 발판만 조용히 반응하지 않았다.
    public string playerTag = "Player";

    #endregion
    #region 컴포넌트 변수

    Collider2D col;
    // SpriteRenderer 가 아니라 Renderer 로 받는다. 그레이박스 지형은 Quad(MeshRenderer)라 SpriteRenderer 로
    // 찾으면 못 잡고, 그러면 콜라이더만 사라져 "보이는데 밟히지 않는" 발판이 되어 버린다.
    Renderer visual;
    bool triggered;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        col = GetComponent<Collider2D>();
        visual = GetComponentInChildren<Renderer>();
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (triggered || !collision.collider.CompareTag(playerTag)) return;
        StartCoroutine(CrumbleRoutine());
    }

    #endregion
    #region 무너짐 · 재생성

    IEnumerator CrumbleRoutine() {
        triggered = true;

        yield return new WaitForSeconds(fallDelay);
        col.enabled = false;
        if (visual != null) visual.enabled = false;

        yield return new WaitForSeconds(respawnDelay);
        col.enabled = true;
        if (visual != null) visual.enabled = true;
        triggered = false;
    }

    #endregion
}
