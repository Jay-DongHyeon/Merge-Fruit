using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Fruit : MonoBehaviour
{
    [Tooltip("0=���� ���� ���")]
    public int grade = 0;

    [Tooltip("���� �� �ο��� �⺻ ����(���ϸ� ���߿� ���̺�ȭ)")]
    public int scoreValue = 10;

    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public bool isMerging;

    void Awake() => rb = GetComponent<Rigidbody2D>();
}
