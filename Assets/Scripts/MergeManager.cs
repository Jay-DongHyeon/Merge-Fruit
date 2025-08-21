using UnityEngine;

public class MergeManager : MonoBehaviour
{
    [Header("grade �ε��� ������ �Ҵ�")]
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
            // �ʿ� �� �ʱ� �ݵ�/ȿ�� �ο� ����:
            var rb = merged.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(Random.insideUnitCircle * 0.5f, ForceMode2D.Impulse);
        }

        GameManager.Instance?.AddScore(mergeBonus * (next + 1));
    }
}
