using UnityEngine;

public class MergeManager : MonoBehaviour
{
    [SerializeField] private GameObject[] fruitPrefabs; // �� Grade0~10 ��
    [SerializeField] private int mergeBonus = 10;       // (���� �̻��, ������)

    public void TryMerge(Fruit a, Fruit b, Vector2 contactPoint)
    {
        if (a == null || b == null) return;
        if (a.isMerging || b.isMerging) return;
        if (a.grade != b.grade) return;

        a.isMerging = b.isMerging = true;
        int next = a.grade + 1; // ���� ��� grade

        // ���� ����
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        // ���� ���� ����
        if (next < fruitPrefabs.Length && fruitPrefabs[next] != null)
        {
            var merged = Instantiate(fruitPrefabs[next], contactPoint, Quaternion.identity);

            // ��¦ ƨ��
            var rb = merged.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(Random.insideUnitCircle * 0.5f, ForceMode2D.Impulse);

            // === ���� ó�� ===
            // �ܰ� n = grade(0-base) + 1  �� ���⼭�� next + 1
            int n = next + 1;
            if (GameManager.Instance != null)
            {
                int add = GameManager.Instance.ComputeMergeScoreForStageN(n); // 3*(n-1) + ���� �� 3^n
                GameManager.Instance.AddScore(add);
            }
        }

        // ���� ȿ����
        AudioManager.Instance?.PlayMerge();
    }
}
