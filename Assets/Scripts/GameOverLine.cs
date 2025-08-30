// Scripts/GameOverLine.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GameOverLine : MonoBehaviour
{
    [Header("Rule")]
    [SerializeField] private string fruitTag = "Fruit";
    [SerializeField] private float contactThreshold = 5f; // 5초 연속 접촉 시 게임오버

    [Header("Visual")]
    [SerializeField] private SpriteRenderer barBg;   // 진행 표시 배경(선택)
    [SerializeField] private SpriteRenderer barFill; // 진행 표시(선택)
    [SerializeField] private Color safeColor = new Color(0.2f, 0.9f, 0.3f, 0.85f);
    [SerializeField] private Color warnColor = new Color(0.95f, 0.15f, 0.15f, 0.95f);
    [SerializeField, Range(0f, 1f)] private float pulseStart = 0.8f; // 80%부터 펄스
    [SerializeField] private float pulseSpeed = 6f; // 펄스 속도

    // 접촉 시작 시각 기록
    private readonly Dictionary<Collider2D, float> contactStart = new();

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
        if (!IsValidFruit(other)) return;
        if (!contactStart.ContainsKey(other))
            contactStart[other] = Time.time;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (contactStart.ContainsKey(other))
            contactStart.Remove(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsValidFruit(other)) return;
        if (!contactStart.ContainsKey(other))
            contactStart[other] = Time.time;

        float elapsed = Time.time - contactStart[other];
        if (elapsed >= contactThreshold)
        {
            contactStart.Clear();
            // ★ 즉시 종료 (클릭 필요 없음)
            GameManager.Instance?.GameOver(false);
        }
    }

    void Update()
    {
        // 죽은/비활성 콜라이더 정리
        if (contactStart.Count > 0)
        {
            var dead = ListCache<Collider2D>.Get();
            foreach (var kv in contactStart)
            {
                if (kv.Key == null || !kv.Key.gameObject.activeInHierarchy)
                    dead.Add(kv.Key);
            }
            foreach (var k in dead) contactStart.Remove(k);
            ListCache<Collider2D>.Release(dead);
        }

        // 진행도 계산: 현재 라인에 닿아있는 과일 중 "최대 경과 시간" 기준
        float maxElapsed = 0f;
        foreach (var kv in contactStart)
            maxElapsed = Mathf.Max(maxElapsed, Time.time - kv.Value);

        float t = Mathf.Clamp01(maxElapsed / contactThreshold); // 0..1
        SetFill(t);
        SetColor(t);
    }

    // --- Visual helpers ---
    void SetFill(float t)
    {
        if (!barFill) return;

        // 기본 비율
        float baseX = Mathf.Max(0.0001f, t);

        // 1.0에 가까울수록 최대 1.15배 확대 (요구사항)
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
            float p = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f); // 0..1
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
        return rb != null && rb.bodyType == RigidbodyType2D.Dynamic; // 들고있는/미리보기(Kinematic) 제외
    }
}

// 간단 리스트 캐시 (가비지 줄이기)
static class ListCache<T>
{
    static readonly System.Collections.Generic.Stack<System.Collections.Generic.List<T>> pool = new();
    public static System.Collections.Generic.List<T> Get() => pool.Count > 0 ? pool.Pop() : new System.Collections.Generic.List<T>(8);
    public static void Release(System.Collections.Generic.List<T> list) { list.Clear(); pool.Push(list); }
}
