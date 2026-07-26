using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 타격 지점에 데미지 숫자를 띄우는 싱글턴. 월드 스페이스 TextMeshPro를 풀링해서 재사용한다.
// HitReactor가 Show()를 호출한다.
public class DamagePopup : MonoBehaviour {
    public static DamagePopup Instance;

    #region 인스펙터 변수

    [Header("폰트")]
    public TMP_FontAsset fontAsset; // 비워두면 TMP 기본 폰트를 사용합니다.
    public float fontSize = 5f;

    [Header("색상")]
    public Color smallDamageColor = new(1f, 0.95f, 0.7f, 1f); // 약한 타격.
    public Color bigDamageColor = new(1f, 0.65f, 0.15f, 1f); // referenceDamage 이상의 타격.
    public Color lethalColor = new(1f, 0.35f, 0.3f, 1f); // 처치한 타격.
    public int referenceDamage = 50; // 이 피해량에서 최대 크기/색에 도달한다.
    public Color outlineColor = Color.black;
    [Range(0f, 1f)]
    public float outlineWidth = 0.25f; // 배경과 겹쳐도 읽히도록.
    public int sortingOrder = 120; // HealthBar(100)보다 위.

    [Header("움직임")]
    public float lifetime = 0.65f;
    public float riseHeight = 1.2f; // 위로 떠오르는 높이.
    public float spreadX = 0.4f; // 좌우 랜덤 오프셋. 연타 시 숫자가 겹쳐 뭉치는 걸 막는다.
    public float wobbleAngle = 12f; // 등장할 때 기울었다가 바로 서는 각도.
    [Range(0f, 1f)]
    public float fadeStart = 0.55f; // 수명의 몇 지점부터 사라지기 시작할지.
    public float popDuration = 0.14f; // 등장 연출 길이.
    // 카툰풍으로 크게 부풀었다가 제 크기로 돌아오는 곡선.
    public AnimationCurve popCurve = new(new Keyframe(0f, 0.4f), new Keyframe(0.55f, 1.35f), new Keyframe(1f, 1f));

    #endregion
    #region 컴포넌트 변수

    readonly Stack<TextMeshPro> pool = new();

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(this);
        }
    }

    #endregion
    #region 숫자 표시

    public void Show(Vector2 point, int damage, bool isLethal) {
        TextMeshPro label = Rent();

        float ratio = referenceDamage > 0 ? Mathf.Clamp01((float)damage / referenceDamage) : 1f;
        Color color = isLethal ? lethalColor : Color.Lerp(smallDamageColor, bigDamageColor, ratio);
        float scale = Mathf.Lerp(0.8f, 1.25f, ratio) * (isLethal ? 1.3f : 1f);

        Vector3 start = new Vector3(point.x + Random.Range(-spreadX, spreadX), point.y, 0f);

        label.text = damage.ToString();
        label.color = color;
        label.transform.SetPositionAndRotation(start, Quaternion.identity);
        label.transform.localScale = Vector3.zero;
        label.gameObject.SetActive(true);

        StartCoroutine(AnimateRoutine(label, start, color, scale, Random.Range(-wobbleAngle, wobbleAngle)));
    }

    // 히트스톱 중에도 떠올라야 하므로 unscaledDeltaTime 기준으로 돈다.
    IEnumerator AnimateRoutine(TextMeshPro label, Vector3 start, Color color, float scale, float startAngle) {
        float elapsed = 0f;

        while (elapsed < lifetime) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            // 위로 튀어오르다 감속한다.
            label.transform.position = start + new Vector3(0f, riseHeight * Mathf.Sin(t * Mathf.PI * 0.5f), 0f);
            label.transform.localScale = Vector3.one * scale * popCurve.Evaluate(Mathf.Clamp01(elapsed / popDuration));
            label.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, 0f, t));

            color.a = 1f - Mathf.Clamp01((t - fadeStart) / (1f - fadeStart));
            label.color = color;

            yield return null;
        }

        Release(label);
    }

    #endregion
    #region 풀링

    TextMeshPro Rent() {
        return pool.Count > 0 ? pool.Pop() : CreateLabel();
    }

    void Release(TextMeshPro label) {
        label.gameObject.SetActive(false);
        pool.Push(label);
    }

    TextMeshPro CreateLabel() {
        // TextMeshPro는 RectTransform을 전제로 하므로 생성 시점부터 붙여둔다. 없으면 rectTransform이 null이 된다.
        GameObject obj = new GameObject("DamagePopup", typeof(RectTransform));
        obj.transform.SetParent(transform, false);

        TextMeshPro label = obj.AddComponent<TextMeshPro>();
        if (fontAsset != null) label.font = fontAsset; // 비워두면 TMP_Settings의 기본 폰트가 그대로 쓰인다.
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(6f, 2f); // 줄바꿈 없이 세 자리 숫자가 들어갈 만큼.

        label.outlineColor = outlineColor;
        label.outlineWidth = outlineWidth;

        obj.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;

        obj.SetActive(false);
        return label;
    }

    #endregion
}
