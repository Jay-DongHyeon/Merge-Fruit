using UnityEngine;

public class MergeManager : MonoBehaviour
{
    [SerializeField] private GameObject[] fruitPrefabs; // ★ Grade0~10 순
    [SerializeField] private int mergeBonus = 10;       // (이제 미사용, 보존만)

    public void TryMerge(Fruit a, Fruit b, Vector2 contactPoint)
    {
        if (a == null || b == null) return;
        if (a.isMerging || b.isMerging) return;
        if (a.grade != b.grade) return;

        a.isMerging = b.isMerging = true;
        int next = a.grade + 1; // 병합 결과 grade

        // 원본 제거
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        // 병합 과일 생성
        if (next < fruitPrefabs.Length && fruitPrefabs[next] != null)
        {
            var merged = Instantiate(fruitPrefabs[next], contactPoint, Quaternion.identity);

            // 살짝 튕김
            var rb = merged.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(Random.insideUnitCircle * 0.5f, ForceMode2D.Impulse);

            // === 점수 처리 ===
            // 단계 n = grade(0-base) + 1  → 여기서는 next + 1
            int n = next + 1;
            if (GameManager.Instance != null)
            {
                int add = GameManager.Instance.ComputeMergeScoreForStageN(n); // 3*(n-1) + 최초 시 3^n
                GameManager.Instance.AddScore(add);
            }
        }

        // 병합 효과음
        AudioManager.Instance?.PlayMerge();
    }
}
