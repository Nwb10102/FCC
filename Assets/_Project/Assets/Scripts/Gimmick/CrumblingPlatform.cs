using System.Collections;
using UnityEngine;

// 밟으면 잠시 후 무너져 사라졌다가, 시간이 지나면 다시 나타나는 발판.
public class CrumblingPlatform : MonoBehaviour {
    #region 인스펙터 변수

    [Header("타이밍")]
    public float fallDelay = 0.4f;   // 밟은 뒤 무너지기까지.
    public float respawnDelay = 2f;  // 다시 나타나기까지.

    #endregion
    #region 컴포넌트 변수

    Collider2D col;
    SpriteRenderer sr; // 있으면 함께 숨긴다. 그레이박스 단계에서는 비어 있을 수 있다.
    bool triggered;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (triggered || !collision.collider.CompareTag("Player")) return;
        StartCoroutine(CrumbleRoutine());
    }

    #endregion
    #region 무너짐 · 재생성

    IEnumerator CrumbleRoutine() {
        triggered = true;

        yield return new WaitForSeconds(fallDelay);
        col.enabled = false;
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);
        col.enabled = true;
        if (sr != null) sr.enabled = true;
        triggered = false;
    }

    #endregion
}
