using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Fruit : MonoBehaviour
{
    [Tooltip("0=가장 작은 등급")]
    public int grade = 0;

    [Tooltip("병합 시 부여할 기본 점수(원하면 나중에 테이블화)")]
    public int scoreValue = 10;

    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public bool isMerging;

    void Awake() => rb = GetComponent<Rigidbody2D>();
}
