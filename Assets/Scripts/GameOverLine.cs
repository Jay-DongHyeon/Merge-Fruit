// Scripts/GameOverLine.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GameOverLine : MonoBehaviour
{
    [Header("Rule")]
    [SerializeField] private string fruitTag = "Fruit";
    [SerializeField] private float contactThreshold = 5f;    // ���� ���� �ð�(��)
    [SerializeField] private bool requireDynamicBody = true; // true�� RigidbodyType2D.Dynamic�� ���
    [SerializeField] private bool debugLogs = false;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer barBg;   // ���� ǥ�� ���(����)
    [SerializeField] private SpriteRenderer barFill; // ���� ǥ��(����)
    [SerializeField] private Color safeColor = new Color(0.2f, 0.9f, 0.3f, 0.85f);
    [SerializeField] private Color warnColor = new Color(0.95f, 0.15f, 0.15f, 0.95f);
    [SerializeField, Range(0f, 1f)] private float pulseStart = 0.8f; // 80%���� �޽�
    [SerializeField] private float pulseSpeed = 6f; // �޽� �ӵ�

    // ���� ���� �ð�(�����ϵ� Ÿ��) ���
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
            contactStartUnscaled[other] = Time.unscaledTime; // �����ϵ�� ���
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
        // ���ӿ����� �߰� ���� ����
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // ����/��Ȱ�� �ݶ��̴� ���� + ��ȿ�� �����
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

        // ���� ����ִ� ���ϵ� �� "�ִ� ��� �ð�(�����ϵ�)"
        float maxElapsed = 0f;
        float now = Time.unscaledTime;
        foreach (var kv in contactStartUnscaled)
        {
            float elapsed = now - kv.Value;
            if (elapsed > maxElapsed) maxElapsed = elapsed;
        }

        // �ð� �ǵ��
        float t = Mathf.Clamp01(maxElapsed / contactThreshold); // 0..1
        SetFill(t);
        SetColor(t);

        // �Ӱ�ġ ���� �� ��� ����
        if (maxElapsed >= contactThreshold && contactStartUnscaled.Count > 0)
        {
            if (debugLogs) Debug.Log($"[OverLine] Threshold reached ({maxElapsed:F2}s) �� GameOverImmediate");
            contactStartUnscaled.Clear();          // �ߺ� ȣ�� ����
            GameManager.Instance?.GameOverImmediate();
        }
    }

    // --- Visual helpers ---
    void SetFill(float t)
    {
        if (!barFill) return;

        // �⺻ ����
        float baseX = Mathf.Max(0.0001f, t);

        // 1.0�� �������� �ִ� 1.15�� Ȯ��
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
            float p = (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f); // �����ϵ�� �޽�
            float extra = Mathf.Lerp(0f, 0.25f, (t - pulseStart) / Mathf.Max(0.0001f, 1f - pulseStart));
            col.a = Mathf.Clamp01(col.a * (1f - extra * p)); // ���� �޽�
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

        // ����ִ� ����(Kinematic) �����ϰ� �ʹٸ� requireDynamicBody�� true�� ����
        if (requireDynamicBody && rb.bodyType != RigidbodyType2D.Dynamic) return false;

        // ������ ��Ȱ��(simulated=false)�̸� ����
        if (!rb.simulated) return false;

        // Collider�� ���������� ����
        if (!col.enabled) return false;

        return true;
    }
}

// ���� ����Ʈ ĳ�� (������ ���̱�)
static class ListCache<T>
{
    static readonly System.Collections.Generic.Stack<System.Collections.Generic.List<T>> pool = new();
    public static System.Collections.Generic.List<T> Get() => pool.Count > 0 ? pool.Pop() : new System.Collections.Generic.List<T>(8);
    public static void Release(System.Collections.Generic.List<T> list) { list.Clear(); pool.Push(list); }
}
