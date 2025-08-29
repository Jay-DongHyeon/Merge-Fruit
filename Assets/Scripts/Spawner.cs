// Scripts/Spawner.cs
using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private Transform spawnPoint;              // 없으면 this.transform 사용

    [Header("Prefabs (Grade0..N 순서)")]
    [SerializeField] private GameObject[] fruitPrefabs;         // 반드시 0→1→… 순서!

    [Header("Placement")]
    [SerializeField] private float horizontalLimit = 3.0f;      // 좌우 이동 한계
    [SerializeField] private float holdY = 6.0f;                // 들고 있을 때 Y

    [Header("Timing")]
    [SerializeField] private float dropCooldown = 0.4f;         // 드롭 입력 쿨타임
    [SerializeField] private float spawnDelay = 0.4f;           // 다음 과일 생성 지연

    [Header("Hold State")]
    [SerializeField] private bool disableColliderWhileHold = true;

    [Header("Detection")]
    [SerializeField] private string fruitTag = "Fruit";         // 보드 과일 Tag
    [SerializeField] private bool debugLogs = false;

    private Camera cam;
    private GameObject heldFruit;
    private Rigidbody2D heldRB;
    private Collider2D heldCol;

    private int nextGrade;
    private float lastDropTime = -999f;
    private bool isSpawning = false;

    void Start()
    {
        cam = Camera.main;
        if (spawnPoint == null) spawnPoint = transform;

        RollNext();
        SpawnHeld();
    }

    void Update()
    {
        if (heldFruit == null) return;

        // 마우스/터치 X만 따라다니기
        Vector3 screen = Input.mousePosition;
        Vector3 world = cam != null
            ? cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z))
            : new Vector3(screen.x, screen.y, 0f);

        float x = Mathf.Clamp(world.x, -horizontalLimit, horizontalLimit);

        Vector3 pos = heldFruit.transform.position;
        pos.x = x;
        pos.y = holdY; // Y 고정
        heldFruit.transform.position = pos;

        // 좌클릭(or 터치) + 쿨타임 체크 → 드롭
        if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime >= dropCooldown)
        {
            DropHeld();
            lastDropTime = Time.time;
            StartCoroutine(SpawnNextWithDelay());
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
        heldFruit = Instantiate(
            fruitPrefabs[nextGrade],
            new Vector3(spawnPoint.position.x, holdY, 0f),
            Quaternion.identity
        );

        heldRB = heldFruit.GetComponent<Rigidbody2D>();
        heldCol = heldFruit.GetComponent<Collider2D>();

        // 들고 있는 동안 물리/충돌 비활성 (미리보기는 보드 최고 단계 계산에서 제외됨)
        if (heldRB != null)
        {
            heldRB.linearVelocity = Vector2.zero;   // Unity 6000.x API
            heldRB.angularVelocity = 0f;
            heldRB.bodyType = RigidbodyType2D.Kinematic;
            heldRB.gravityScale = 0f;
        }
        if (disableColliderWhileHold && heldCol != null)
        {
            heldCol.enabled = false;
        }
    }

    // ... 기존 using들 유지
    // DropHeld() 내부만 추가/수정
    void DropHeld()
    {
        if (heldFruit == null) return;

        if (heldRB != null)
        {
            heldRB.bodyType = RigidbodyType2D.Dynamic;
            heldRB.gravityScale = 1f;
        }
        if (disableColliderWhileHold && heldCol != null)
        {
            heldCol.enabled = true;
        }

        // ★ 효과음 & BGM 보장
        AudioManager.Instance?.PlayDrop();
        AudioManager.Instance?.EnsureBgmPlaying();

        heldFruit = null;
        heldRB = null;
        heldCol = null;
    }


    // --- 스폰 규칙 ---
    // - 최고 등급 >= 2: cap = 최고-1, 0..cap 균등
    // - 최고 등급 <= 1(비었음/0/1 포함): 0..min(3,maxIndex) 균등
    void RollNext()
    {
        int maxIndex = fruitPrefabs.Length - 1; // 예: 10 (0~10)
        int highest = GetHighestGradeOnBoard(); // '보드 위 실제 과일'만 고려(Dynamic + Tag)

        int cap;
        if (highest >= 2)
        {
            cap = Mathf.Min(maxIndex, highest - 1);
        }
        else
        {
            cap = Mathf.Min(maxIndex, 3); // 시작/낮은 단계 구간은 0~3
        }

        nextGrade = Random.Range(0, cap + 1);  // 균등

        if (debugLogs)
            Debug.Log($"[Spawner] highest={highest}, cap={cap}, nextGrade={nextGrade}");
    }

    // '보드 위 실제 과일'만 집계: Fruit Tag + Rigidbody2D.Dynamic 인 것만
    int GetHighestGradeOnBoard()
    {
        int highest = -1;
        var fruits = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        for (int i = 0; i < fruits.Length; i++)
        {
            var f = fruits[i];
            if (f == null || !f.isActiveAndEnabled) continue;

            // Tag 체크 (실수로 다른 Tag가 섞일 때 방지)
            if (!string.IsNullOrEmpty(fruitTag) && !f.CompareTag(fruitTag)) continue;

            // 미리보기(held)는 Kinematic, 실제 보드 과일은 Dynamic
            var rb = f.rb != null ? f.rb : f.GetComponent<Rigidbody2D>();
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) continue;

            highest = Mathf.Max(highest, f.grade);
        }
        return highest;
    }
}
