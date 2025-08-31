// Scripts/GameOverLine.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GameOverLine : MonoBehaviour
{
    [Header("Rule")]
    [SerializeField] private string fruitTag = "Fruit";
    [SerializeField] private float contactThreshold = 5f;    // 연속 접촉 시간(초)
    [SerializeField] private bool requireDynamicBody = true; // true면 RigidbodyType2D.Dynamic만 허용
    [SerializeField] private bool debugLogs = false;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer barBg;   // 진행 표시 배경(선택)
    [SerializeField] private SpriteRenderer barFill; // 진행 표시(선택)
    [SerializeField] private Color safeColor = new Color(0.2f, 0.9f, 0.3f, 0.85f);
    [SerializeField] private Color warnColor = new Color(0.95f, 0.15f, 0.15f, 0.95f);
    [SerializeField, Range(0f, 1f)] private float pulseStart = 0.8f; // 80%부터 펄스
    [SerializeField] private float pulseSpeed = 6f; // 펄스 속도

    // 접촉 시작 시각(언스케일드 타임) 기록
    private readonly Dictionary<Collider2D, float> contactStartUnscaled = new();

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        if (barFill) SetFill(0f);
        SetColor(0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (!IsValidFruit(other)) return;

        if (!contactStartUnscaled.ContainsKey(other))
            contactStartUnscaled[other] = Time.unscaledTime; // 언스케일드로 기록
        if (debugLogs) Debug.Log($"[OverLine] Enter {other.name}");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (contactStartUnscaled.ContainsKey(other))
        {
            contactStartUnscaled.Remove(other);
            if (debugLogs) Debug.Log($"[OverLine] Exit {other.name}");
        }
    }

    void Update()
    {
        // 게임오버면 추가 연산 중지
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // 죽은/비활성 콜라이더 정리 + 유효성 재검증
        if (contactStartUnscaled.Count > 0)
        {
            var dead = ListCache<Collider2D>.Get();
            foreach (var kv in contactStartUnscaled)
            {
                var c = kv.Key;
                if (c == null || !c.gameObject.activeInHierarchy || !IsValidFruit(c))
                    dead.Add(c);
            }
            foreach (var d in dead) contactStartUnscaled.Remove(d);
            ListCache<Collider2D>.Release(dead);
        }

        // 현재 닿아있는 과일들 중 "최대 경과 시간(언스케일드)"
        float maxElapsed = 0f;
        float now = Time.unscaledTime;
        foreach (var kv in contactStartUnscaled)
        {
            float elapsed = now - kv.Value;
            if (elapsed > maxElapsed) maxElapsed = elapsed;
        }

        // 시각 피드백
        float t = Mathf.Clamp01(maxElapsed / contactThreshold); // 0..1
        SetFill(t);
        SetColor(t);

        // 임계치 도달 시 즉시 종료
        if (maxElapsed >= contactThreshold && contactStartUnscaled.Count > 0)
        {
            if (debugLogs) Debug.Log($"[OverLine] Threshold reached ({maxElapsed:F2}s) → GameOverImmediate");
            contactStartUnscaled.Clear();          // 중복 호출 방지
            GameManager.Instance?.GameOverImmediate();
        }
    }

    // --- Visual helpers ---
    void SetFill(float t)
    {
        if (!barFill) return;

        // 기본 비율
        float baseX = Mathf.Max(0.0001f, t);

        // 1.0에 가까울수록 최대 1.15배 확대
        float extraScale = Mathf.Lerp(1f, 1.15f, t);
        float scaledX = baseX * extraScale;

        var s = barFill.transform.localScale;
        s.x = scaledX;
        barFill.transform.localScale = s;
    }

    void SetColor(float t)
    {
        var col = Color.Lerp(safeColor, warnColor, t);
        if (t >= pulseStart)
        {
            float p = (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f); // 언스케일드로 펄스
            float extra = Mathf.Lerp(0f, 0.25f, (t - pulseStart) / Mathf.Max(0.0001f, 1f - pulseStart));
            col.a = Mathf.Clamp01(col.a * (1f - extra * p)); // 알파 펄스
        }

        if (barBg) barBg.color = new Color(col.r, col.g, col.b, Mathf.Clamp01(0.35f + 0.25f * t));
        if (barFill) barFill.color = col;
    }

    bool IsValidFruit(Collider2D col)
    {
        if (col == null) return false;
        if (!col.CompareTag(fruitTag)) return false;

        var rb = col.attachedRigidbody;
        if (rb == null) return false;

        // 들고있는 상태(Kinematic) 제외하고 싶다면 requireDynamicBody를 true로 유지
        if (requireDynamicBody && rb.bodyType != RigidbodyType2D.Dynamic) return false;

        // 물리가 비활성(simulated=false)이면 제외
        if (!rb.simulated) return false;

        // Collider가 꺼져있으면 제외
        if (!col.enabled) return false;

        return true;
    }
}

// 간단 리스트 캐시 (가비지 줄이기)
static class ListCache<T>
{
    static readonly System.Collections.Generic.Stack<System.Collections.Generic.List<T>> pool = new();
    public static System.Collections.Generic.List<T> Get() => pool.Count > 0 ? pool.Pop() : new System.Collections.Generic.List<T>(8);
    public static void Release(System.Collections.Generic.List<T> list) { list.Clear(); pool.Push(list); }
}
