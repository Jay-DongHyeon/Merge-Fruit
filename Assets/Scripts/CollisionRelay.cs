using UnityEngine;

[RequireComponent(typeof(Fruit))]
public class CollisionRelay : MonoBehaviour
{
    private Fruit me;
    private MergeManager mgr;

    void Awake()
    {
        me = GetComponent<Fruit>();
        mgr = FindFirstObjectByType<MergeManager>();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!enabled || mgr == null) return;

        var other = col.collider.GetComponent<Fruit>();
        if (other == null) return;
        if (me.grade != other.grade) return;
        if (me.isMerging || other.isMerging) return;

        Vector2 contact = col.GetContact(0).point;
        mgr.TryMerge(me, other, contact);
    }
}
