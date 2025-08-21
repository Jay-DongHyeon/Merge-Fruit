using UnityEngine;

public class MergeManager : MonoBehaviour
{
    [Header("grade 인덱스 순으로 할당")]
    [SerializeField] private GameObject[] fruitPrefabs;

    [SerializeField] private int mergeBonus = 10;

    public void TryMerge(Fruit a, Fruit b, Vector2 contactPoint)
    {
        if (a.isMerging || b.isMerging) return;
        if (a.grade != b.grade) return;

        a.isMerging = b.isMerging = true;

        int next = a.grade + 1;
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (next < fruitPrefabs.Length)
        {
            var merged = Instantiate(fruitPrefabs[next], contactPoint, Quaternion.identity);
            // 필요 시 초기 반동/효과 부여 가능:
            var rb = merged.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(Random.insideUnitCircle * 0.5f, ForceMode2D.Impulse);
        }

        GameManager.Instance?.AddScore(mergeBonus * (next + 1));
    }
}
