using UnityEngine;

// 타격/사망 순간의 임팩트 파티클을 재생하는 싱글턴.
// 파티클 시스템을 런타임에 코드로 구성하므로 별도의 이펙트 에셋이 필요 없다.
// HitReactor가 PlaySpark()/PlayDeath()를 호출한다.
public class HitVfx : MonoBehaviour {
    public static HitVfx Instance;

    #region 인스펙터 변수

    [Header("스파크")]
    public int sparkCount = 14; // 한 번 타격할 때 튀는 파편 수.
    public float sparkSpeed = 9f; // 파편이 튀어나가는 속도.
    public float sparkSpeedRandom = 4f; // 속도에 더해지는 랜덤 폭.
    public float sparkSize = 0.22f;
    public float sparkLifetime = 0.35f;
    public float sparkSpreadAngle = 70f; // 타격 방향을 중심으로 파편이 퍼지는 각도.
    public float sparkGravity = 0.6f; // 파편이 아래로 떨어지는 정도. 카툰 톤에서는 살짝 있는 편이 좋다.

    [Header("스파크 색상")]
    public Color sparkCoreColor = Color.white;
    public Color sparkMidColor = new(1f, 0.85f, 0.25f, 1f);
    public Color sparkEndColor = new(1f, 0.35f, 0.05f, 0f);

    [Header("임팩트 플래시")]
    public float flashSize = 1.6f; // 타격 지점에 팡 하고 터지는 빛의 크기.
    public float flashLifetime = 0.12f;
    public Color flashColor = new(1f, 1f, 0.85f, 1f);

    [Header("사망 파열")]
    public int deathBurstCount = 30;
    public float deathBurstSpeed = 12f;
    public float deathBurstSize = 0.3f;
    public float deathBurstLifetime = 0.6f;

    [Header("렌더링")]
    public int sortingOrder = 60; // 캐릭터보다 위, HealthBar(100)보다 아래.

    #endregion
    #region 컴포넌트 변수

    ParticleSystem sparkSystem; // 타격 방향으로 부채꼴로 튀는 파편.
    ParticleSystem flashSystem; // 타격 지점에서 한 번 터지는 빛.
    ParticleSystem deathSystem; // 사망 시 사방으로 퍼지는 파열.

    static Texture2D dotTexture;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(this);
            return;
        }

        BuildSystems();
        ApplySettings();
    }

#if UNITY_EDITOR
    // 플레이 중 인스펙터에서 수치를 바꾸면 바로 반영되도록. 파티클은 눈으로 보면서 맞춰야 해서 필요하다.
    void OnValidate() {
        if (Application.isPlaying && sparkSystem != null) ApplySettings();
    }
#endif

    #endregion
    #region 이펙트 재생

    // point: 타격 지점. dir: 공격자 → 피격자 방향(정규화). 파편이 이 방향으로 부채꼴로 튄다.
    public void PlaySpark(Vector2 point, Vector2 dir) {
        if (sparkSystem == null) return;

        // Circle 셰이프의 arc는 로컬 +X에서 반시계로 열리므로, 부채꼴의 중심이 dir을 향하도록 절반만큼 되돌린다.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        sparkSystem.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f, angle - sparkSpreadAngle * 0.5f));
        sparkSystem.Emit(sparkCount);

        flashSystem.transform.position = point;
        flashSystem.Emit(1);
    }

    public void PlayDeath(Vector2 point) {
        if (deathSystem == null) return;

        deathSystem.transform.position = point;
        deathSystem.Emit(deathBurstCount);

        flashSystem.transform.position = point;
        flashSystem.Emit(1);
    }

    #endregion
    #region 파티클 시스템 생성

    void BuildSystems() {
        Material material = new Material(Shader.Find("Sprites/Default")) { mainTexture = GetDotTexture() };

        sparkSystem = CreateSystem("Spark", material, ParticleSystemRenderMode.Stretch);
        flashSystem = CreateSystem("Flash", material, ParticleSystemRenderMode.Billboard);
        deathSystem = CreateSystem("DeathBurst", material, ParticleSystemRenderMode.Billboard);
    }

    ParticleSystem CreateSystem(string systemName, Material material, ParticleSystemRenderMode renderMode) {
        GameObject obj = new GameObject(systemName);
        obj.transform.SetParent(transform);

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        // rateOverTime을 0으로 두고 계속 재생 상태로 유지한 뒤 Emit()으로 필요할 때만 뿜는다.
        // 이렇게 해야 여러 타격이 겹쳐도 앞선 파티클이 끊기지 않는다.
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;

        ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = material;
        psRenderer.renderMode = renderMode;
        if (renderMode == ParticleSystemRenderMode.Stretch) {
            psRenderer.velocityScale = 0.06f; // 속도 방향으로 늘려 카툰풍 속도선처럼 보이게 한다.
            psRenderer.lengthScale = 2f;
        }

        return ps;
    }

    void ApplySettings() {
        ConfigureSpark();
        ConfigureFlash();
        ConfigureDeath();
    }

    void ConfigureSpark() {
        ParticleSystem.MainModule main = ConfigureMain(sparkSystem, sparkLifetime, sparkSize, sparkGravity);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkSpeed, sparkSpeed + sparkSpeedRandom);
        main.startColor = Color.white; // 색은 전부 colorOverLifetime 그라디언트가 담당한다(둘은 곱해진다).

        ParticleSystem.ShapeModule shape = sparkSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;
        shape.radiusThickness = 0f; // 테두리에서만 방출해 방향이 깔끔하게 방사형이 되도록.
        shape.arc = sparkSpreadAngle;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

        SetColorOverLifetime(sparkSystem, BuildGradient(sparkCoreColor, sparkMidColor, sparkEndColor));
        SetShrinkOverLifetime(sparkSystem);

        sparkSystem.GetComponent<ParticleSystemRenderer>().sortingOrder = sortingOrder;
    }

    void ConfigureFlash() {
        ParticleSystem.MainModule main = ConfigureMain(flashSystem, flashLifetime, flashSize, 0f);
        main.startSpeed = 0f;
        main.startColor = flashColor;
        main.startRotation = 0f;

        ParticleSystem.ShapeModule shape = flashSystem.shape;
        shape.enabled = false; // 타격 지점 한 곳에서만 터지면 되므로 셰이프가 필요 없다.

        SetColorOverLifetime(flashSystem, BuildFadeGradient());
        SetShrinkOverLifetime(flashSystem);

        flashSystem.GetComponent<ParticleSystemRenderer>().sortingOrder = sortingOrder + 1;
    }

    void ConfigureDeath() {
        ParticleSystem.MainModule main = ConfigureMain(deathSystem, deathBurstLifetime, deathBurstSize, sparkGravity);
        main.startSpeed = new ParticleSystem.MinMaxCurve(deathBurstSpeed * 0.4f, deathBurstSpeed);
        main.startColor = Color.white;

        ParticleSystem.ShapeModule shape = deathSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;
        shape.radiusThickness = 0f;
        shape.arc = 360f; // 사방으로 퍼진다.
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

        SetColorOverLifetime(deathSystem, BuildGradient(sparkCoreColor, sparkMidColor, sparkEndColor));
        SetShrinkOverLifetime(deathSystem);

        deathSystem.GetComponent<ParticleSystemRenderer>().sortingOrder = sortingOrder;
    }

    ParticleSystem.MainModule ConfigureMain(ParticleSystem ps, float lifetime, float size, float gravity) {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true; // 재생 상태를 유지한 채 Emit()으로만 뿜기 위해.
        main.playOnAwake = true;
        // duration은 건드리지 않는다. 버스트 없이 Emit()으로만 뿜으므로 의미가 없고,
        // 재생 중에 대입하면 "Setting the duration while system is still playing" assert가 뜬다.

        // 히트스톱 중에는 Time.timeScale이 0.02까지 떨어진다. 이걸 켜지 않으면 이펙트가 그대로 얼어붙는다.
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 뿌려진 자리에 남아야 하므로.

        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); // 스크립트에서는 라디안 단위.
        main.gravityModifier = gravity;
        main.maxParticles = 400;

        ps.Play();
        return main;
    }

    static void SetColorOverLifetime(ParticleSystem ps, Gradient gradient) {
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    // 시간이 지날수록 작아지며 사라지게 한다.
    static void SetShrinkOverLifetime(ParticleSystem ps) {
        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
    }

    static Gradient BuildGradient(Color core, Color mid, Color end) {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(core, 0f),
                new GradientColorKey(mid, 0.35f),
                new GradientColorKey(end, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(core.a, 0f),
                new GradientAlphaKey(mid.a, 0.35f),
                new GradientAlphaKey(end.a, 1f)
            });
        return gradient;
    }

    static Gradient BuildFadeGradient() {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    // 가장자리로 갈수록 흐려지는 원형 텍스처를 만들어 별도 이미지 에셋 없이 파티클을 그린다.
    // AttackRangeIndicator/HealthBar가 스프라이트를 만드는 방식과 같다.
    static Texture2D GetDotTexture() {
        if (dotTexture != null) return dotTexture;

        const int resolution = 32;
        dotTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        dotTexture.filterMode = FilterMode.Bilinear;
        dotTexture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(resolution - 1, resolution - 1) * 0.5f;
        float radius = resolution * 0.5f;

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - dist / radius);
                dotTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha)); // 제곱해서 가장자리를 더 부드럽게.
            }
        }
        dotTexture.Apply();

        return dotTexture;
    }

    #endregion
}
