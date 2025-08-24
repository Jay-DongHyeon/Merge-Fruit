// Scripts/Spawner.cs
using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;          // 화면 위 고정 Y
    [SerializeField] private GameObject[] fruitPrefabs;     // grade 순
    [SerializeField] private float horizontalLimit = 3.0f;  // 좌우 한계
    [SerializeField] private float holdY = 6.0f;            // 들고 있을 때 Y
    [SerializeField] private bool disableColliderWhileHold = true;
    [SerializeField] private float dropCooldown = 0.4f;     // 드롭 쿨타임
    [SerializeField] private float spawnDelay = 0.4f;       // 생성 지연 시간

    private Camera cam;
    private GameObject heldFruit;       // 들고 있는 과일 인스턴스
    private Rigidbody2D heldRB;
    private Collider2D heldCol;
    private int nextGrade;
    private float lastDropTime = -999f;
    private bool isSpawning = false;    // 생성 지연 중인지 여부

    void Start()
    {
        cam = Camera.main;
        RollNext();
        SpawnHeld();
    }

    void Update()
    {
        if (heldFruit != null)
        {
            // 마우스 X를 월드 좌표로 변환해 X만 따라감
            Vector3 screen = Input.mousePosition;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            float clampedX = Mathf.Clamp(world.x, -horizontalLimit, horizontalLimit);

            Vector3 pos = heldFruit.transform.position;
            pos.x = clampedX;
            pos.y = holdY;               // Y 고정
            heldFruit.transform.position = pos;

            // 좌클릭 + 쿨타임 체크 후 드롭
            if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime >= dropCooldown)
            {
                DropHeld();
                lastDropTime = Time.time;
                StartCoroutine(SpawnNextWithDelay()); // ★ 일정 시간 후 새 과일 생성
            }
        }
    }

    IEnumerator SpawnNextWithDelay()
    {
        if (isSpawning) yield break;
        isSpawning = true;

        yield return new WaitForSeconds(spawnDelay);

        RollNext();
        SpawnHeld();

        isSpawning = false;
    }

    void SpawnHeld()
    {
        // 미리보기 과일 생성(중력/충돌 비활성)
        heldFruit = Instantiate(
            fruitPrefabs[nextGrade],
            new Vector3(spawnPoint.position.x, holdY, 0f),
            Quaternion.identity
        );

        heldRB = heldFruit.GetComponent<Rigidbody2D>();
        heldCol = heldFruit.GetComponent<Collider2D>();

        if (heldRB != null)
        {
            heldRB.linearVelocity = Vector2.zero;
            heldRB.angularVelocity = 0f;
            heldRB.bodyType = RigidbodyType2D.Kinematic; // 물리 영향 안 받음
            heldRB.gravityScale = 0f;
        }
        if (heldCol != null && disableColliderWhileHold)
        {
            heldCol.enabled = false; // 상단에서 충돌 방지
        }
    }

    void DropHeld()
    {
        if (heldFruit == null) return;

        // 물리 활성화해서 떨어뜨리기
        if (heldRB != null)
        {
            heldRB.bodyType = RigidbodyType2D.Dynamic;
            heldRB.gravityScale = 1f;
        }
        if (heldCol != null && disableColliderWhileHold)
        {
            heldCol.enabled = true;
        }

        heldFruit = null;
        heldRB = null;
        heldCol = null;
    }

    void RollNext()
    {
        int max = Mathf.Min(3, fruitPrefabs.Length); // 초기엔 작은 등급 위주
        nextGrade = Random.Range(0, max);
    }
}
