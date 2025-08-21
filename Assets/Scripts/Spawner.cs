using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject[] fruitPrefabs; // grade 순서대로
    [SerializeField] private float horizontalLimit = 3.0f;
    [SerializeField] private float dropCooldown = 0.15f;

    private Camera cam;
    private float lastDropTime = -999f;
    private int nextGrade;

    void Start()
    {
        cam = Camera.main;
        RollNext();
    }

    void Update()
    {
        // 마우스 X를 월드로 변환해 Spawner 위치 보정
        Vector3 screen = Input.mousePosition;
        if (cam != null)
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(world.x, -horizontalLimit, horizontalLimit);
            transform.position = pos;
        }

        // 좌클릭/터치 드롭
        if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime > dropCooldown)
        {
            Drop();
            lastDropTime = Time.time;
        }
    }

    void Drop()
    {
        Instantiate(fruitPrefabs[nextGrade], spawnPoint.position, Quaternion.identity);
        RollNext();
    }

    void RollNext()
    {
        // 초기엔 작은 등급 위주 (0~2 범위, 존재 범위 내에서)
        int max = Mathf.Min(3, fruitPrefabs.Length);
        nextGrade = Random.Range(0, max);
    }
}
