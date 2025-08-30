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
    [SerializeField] private SpriteRenderer barBg;   // 라인 배경(선택)
    [SerializeField] private SpriteRenderer barFill; // 진행 게이지(선택)
    [SerializeField] private Color safeColor = new Color(0.2f, 0.9f, 0.3f, 0.85f);
    [SerializeField] private Color warnColor = new Color(0.95f, 0.15f, 0.15f, 0.95f);
    [SerializeField, Range(0f, 1f)] private float pulseStart = 0.8f; // 경고 펄스 시작 지점(=80%)
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
        // barFill이 있으면 시작 시 비움
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
            GameManager.Instance?.GameOver();
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

        // 기본적으로는 0..1 사이 비율
        float baseX = Mathf.Max(0.0001f, t);

        // 게이지가 100% 가까워질수록 최대 1.15까지 확대
        float extraScale = Mathf.Lerp(1f, 1.15f, t); // t=0일 땐 1, t=1일 땐 1.15
        float scaledX = baseX * extraScale;

        var s = barFill.transform.localScale;
        s.x = scaledX;
        barFill.transform.localScale = s;
    }


    void SetColor(float t)
    {
        // 색상은 안전→경고로 보간 + 막바지에는 펄스 효과
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
        return rb != null && rb.bodyType == RigidbodyType2D.Dynamic; // 들고있는 미리보기(Kinematic) 제외
    }
}

// 가비지 줄이기용 간단 캐시 (없어도 동작)
static class ListCache<T>
{
    static readonly System.Collections.Generic.Stack<System.Collections.Generic.List<T>> pool = new();
    public static System.Collections.Generic.List<T> Get() => pool.Count > 0 ? pool.Pop() : new System.Collections.Generic.List<T>(8);
    public static void Release(System.Collections.Generic.List<T> list) { list.Clear(); pool.Push(list); }
}
