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

    // === Next Preview 옵션 ===
    [Header("Next Preview")]
    [SerializeField] private bool showNextPreview = true;       // 미리보기 On/Off
    [SerializeField] private Transform previewAnchor;           // 미리보기 표시 위치(없으면 Spawner 하위 자동 생성)
    [SerializeField, Range(0.1f, 1.5f)] private float previewScale = 0.35f;
    [SerializeField, Range(0f, 1f)] private float previewAlpha = 1f;
    [SerializeField] private string previewSortingLayer = "UI";
    [SerializeField] private int previewSortingOrder = 1000;
    [SerializeField] private Vector3 previewLocalOffset = Vector3.zero;

    private Camera cam;
    private GameObject heldFruit;
    private Rigidbody2D heldRB;
    private Collider2D heldCol;

    // 들고 있는 과일 등급 / 다음 과일 등급(미리보기)
    private int currentGrade = -1;
    private int nextGrade = -1;

    private float lastDropTime = -999f;
    private bool isSpawning = false;

    // 미리보기용 SpriteRenderer (물리/충돌 X)
    private SpriteRenderer previewSR;

    // 외부에서 참조하고 싶으면 공개 프로퍼티로 노출
    public int NextIndex => nextGrade;

    void Start()
    {
        cam = Camera.main;
        if (spawnPoint == null) spawnPoint = transform;

        EnsurePreviewObjects();

        // 시작 시: 현재/다음 2개를 연속으로 뽑아둠
        currentGrade = RollNext();
        nextGrade = RollNext();

        SpawnHeld(currentGrade);
        UpdateNextPreview(); // 다음 것 미리보기 표시
    }

    void Update()
    {
        // ★ 게임오버면 입력/이동/드롭 모두 차단
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
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

        // 다음 단계로 밀기: next → current, 그리고 새로운 next 뽑기
        currentGrade = nextGrade;
        SpawnHeld(currentGrade);

        nextGrade = RollNext();
        UpdateNextPreview();

        isSpawning = false;
    }

    void SpawnHeld(int grade)
    {
        var prefab = GetPrefabSafely(grade);
        if (prefab == null) return;

        heldFruit = Instantiate(
            prefab,
            new Vector3(spawnPoint.position.x, holdY, 0f),
            Quaternion.identity
        );

        heldRB = heldFruit.GetComponent<Rigidbody2D>();
        heldCol = heldFruit.GetComponent<Collider2D>();

        // 들고 있는 동안 물리/충돌 비활성 (미리보기는 최고단계 계산 제외됨)
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

    void DropHeld()
    {
        // ★ 게임오버면 드롭 금지
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
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

        // 드롭 효과음 + (WebGL 등) 첫 입력 후 BGM 보장
        AudioManager.Instance?.PlayDrop();
        AudioManager.Instance?.EnsureBgmPlaying();

        heldFruit = null;
        heldRB = null;
        heldCol = null;
    }

    // --- 스폰 규칙 ---
    // - 최고 등급 >= 2: cap = 최고-1, 0..cap "균등" 랜덤
    // - 최고 등급 <= 1(비었음/0/1 포함): 0..min(3,maxIndex) "균등" 랜덤
    int RollNext()
    {
        int maxIndex = fruitPrefabs.Length - 1;
        if (maxIndex < 0) return -1;

        int highest = GetHighestGradeOnBoard(); // 보드 위 '실제 과일'(Dynamic + Tag)만 집계

        int cap;
        if (highest >= 2) cap = Mathf.Min(maxIndex, highest - 1);
        else cap = Mathf.Min(maxIndex, 3);

        int picked = Random.Range(0, cap + 1);  // 균등

        if (debugLogs)
            Debug.Log($"[Spawner] highest={highest}, cap={cap}, picked(next)={picked}");
        return picked;
    }

    GameObject GetPrefabSafely(int index)
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0) return null;
        if (index < 0 || index >= fruitPrefabs.Length) return null;
        return fruitPrefabs[index];
    }

    // Fruit 태그 + Rigidbody2D.Dynamic 만 최고등급 판정에 포함
    int GetHighestGradeOnBoard()
    {
        int highest = -1;
        var fruits = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        for (int i = 0; i < fruits.Length; i++)
        {
            var f = fruits[i];
            if (f == null || !f.isActiveAndEnabled) continue;

            if (!string.IsNullOrEmpty(fruitTag) && !f.CompareTag(fruitTag)) continue;

            var rb = f.rb != null ? f.rb : f.GetComponent<Rigidbody2D>();
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) continue;

            highest = Mathf.Max(highest, f.grade);
        }
        return highest;
    }

    // =========================
    //        Preview 파트
    // =========================
    void EnsurePreviewObjects()
    {
        if (!showNextPreview) return;

        // 앵커 없으면 자동 생성 (Spawner 하위)
        if (previewAnchor == null)
        {
            var anchorGO = new GameObject("NextPreviewAnchor");
            anchorGO.transform.SetParent(transform, false);
            // 화면 우상단 느낌으로 약간 치우쳐 배치하고 싶다면 여기서 로컬 오프셋 조정
            anchorGO.transform.localPosition = new Vector3(3.8f, 7.5f, 0f);
            previewAnchor = anchorGO.transform;
        }

        // SpriteRenderer 준비
        if (previewSR == null)
        {
            var go = new GameObject("NextPreviewSprite");
            go.transform.SetParent(previewAnchor, false);
            previewSR = go.AddComponent<SpriteRenderer>();
        }

        // 정렬/스케일/투명도 세팅
        previewSR.sortingLayerName = previewSortingLayer;
        previewSR.sortingOrder = previewSortingOrder;
        previewSR.transform.localScale = Vector3.one * previewScale;
        previewSR.transform.localPosition = previewLocalOffset;

        var c = previewSR.color; c.a = Mathf.Clamp01(previewAlpha); previewSR.color = c;
    }

    void UpdateNextPreview()
    {
        if (!showNextPreview) return;
        EnsurePreviewObjects();

        var prefab = GetPrefabSafely(nextGrade);
        Sprite sprite = GetRepresentativeSprite(prefab);

        if (previewSR != null)
        {
            previewSR.sprite = sprite;
            // 안전 차원에서 매번 설정(인스펙터에서 값 바꿨을 수 있으니)
            previewSR.sortingLayerName = previewSortingLayer;
            previewSR.sortingOrder = previewSortingOrder;
            previewSR.transform.localScale = Vector3.one * previewScale;
            previewSR.transform.localPosition = previewLocalOffset;

            var c = previewSR.color;
            c.a = Mathf.Clamp01(previewAlpha);
            previewSR.color = c;
        }

        if (debugLogs)
        {
            string name = prefab != null ? prefab.name : "NULL";
            Debug.Log($"[Spawner-Preview] nextGrade={nextGrade}, prefab={name}, sprite={(sprite ? sprite.name : "NULL")}");
        }
    }

    Sprite GetRepresentativeSprite(GameObject prefab)
    {
        if (prefab == null) return null;

        // 루트에 SpriteRenderer가 있으면 우선 사용
        var rootSR = prefab.GetComponent<SpriteRenderer>();
        if (rootSR != null && rootSR.sprite != null) return rootSR.sprite;

        // 자식에 있으면 사용
        var childSR = prefab.GetComponentInChildren<SpriteRenderer>();
        if (childSR != null && childSR.sprite != null) return childSR.sprite;

        return null;
    }
}
