// Scripts/MergeManager.cs
using UnityEngine;

public class MergeManager : MonoBehaviour
{
    [SerializeField] private GameObject[] fruitPrefabs; // ★ Grade0~10 순
    [SerializeField] private int mergeBonus = 10;

    public void TryMerge(Fruit a, Fruit b, Vector2 contactPoint)
    {
        if (a == null || b == null) return;
        if (a.isMerging || b.isMerging) return;
        if (a.grade != b.grade) return;

        a.isMerging = b.isMerging = true;
        int next = a.grade + 1;

        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (next < fruitPrefabs.Length && fruitPrefabs[next] != null)
        {
            var merged = Instantiate(fruitPrefabs[next], contactPoint, Quaternion.identity);
            var rb = merged.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(Random.insideUnitCircle * 0.5f, ForceMode2D.Impulse);
        }

        // ★ 병합 효과음 재생
        AudioManager.Instance?.PlayMerge();

        GameManager.Instance?.AddScore(mergeBonus * (next + 1));
    }
}
