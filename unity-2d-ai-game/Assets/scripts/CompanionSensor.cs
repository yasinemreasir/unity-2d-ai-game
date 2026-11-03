using System.Collections.Generic;
using UnityEngine;

// Sensörün bir anda topladığı gözlem: ajanın dünyayı "gördüğü" hali.
public struct CompanionObservation
{
    public Vector2 Position;      // Ajanın konumu
    public float TargetDistance;  // Hedefe düz mesafe
    public float TargetAngle;     // Hedefin açısı (derece, 0 = sağ, saat yönünün tersi pozitif)
    public bool TargetVisible;    // Hedefe giden doğrudan hat açık mı (arada duvar/çit yok mu)
    public float[] RayDistances;  // Çevredeki ışınların engele mesafesi (engel yoksa max mesafe). Kopya değil, sonraki Observe'a kadar geçerli.

    public override string ToString()
    {
        return string.Format(
            "poz=({0:F1},{1:F1}) hedefMesafe={2:F2} hedefAçı={3:F0} görünür={4}",
            Position.x, Position.y, TargetDistance, TargetAngle, TargetVisible);
    }
}

// Raycast tabanlı sensör: ajanın konumunu, hedefe mesafe/açıyı ve çevredeki engellere
// olan mesafeyi ölçer. Karar mantığı (durum makinesi) bu gözlemi kullanır.
public class CompanionSensor : MonoBehaviour
{
    [SerializeField] int rayCount = 8;
    [SerializeField] float maxRayDistance = 2f;
    [Tooltip("Hangi katmanlar engel sayılsın. Karakterler (dinamik Rigidbody) ve trigger'lar zaten engel sayılmaz.")]
    [SerializeField] LayerMask obstacleMask = ~0;
    [SerializeField] bool drawGizmos = true;

    ContactFilter2D filter;
    readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    float[] rayDistances;

    void Awake()
    {
        rayCount = Mathf.Max(1, rayCount);
        rayDistances = new float[rayCount];

        filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(obstacleMask);
    }

    public CompanionObservation Observe(Vector2 targetPosition)
    {
        Vector2 origin = transform.position;

        for (int i = 0; i < rayCount; i++)
        {
            rayDistances[i] = DistanceToObstacle(origin, Direction(i, rayCount), maxRayDistance);
        }

        Vector2 toTarget = targetPosition - origin;

        CompanionObservation observation = new CompanionObservation();
        observation.Position = origin;
        observation.TargetDistance = toTarget.magnitude;
        observation.TargetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        observation.TargetVisible = HasLineOfSight(targetPosition);
        observation.RayDistances = rayDistances;
        return observation;
    }

    // Ajan ile hedef arasında duvar/çit gibi bir engel var mı?
    public bool HasLineOfSight(Vector2 target)
    {
        Vector2 origin = transform.position;
        Physics2D.Linecast(origin, target, filter, hits);

        for (int i = 0; i < hits.Count; i++)
        {
            if (IsObstacle(hits[i])) return false;
        }
        return true;
    }

    float DistanceToObstacle(Vector2 origin, Vector2 direction, float maxDistance)
    {
        Physics2D.Raycast(origin, direction, filter, hits, maxDistance);

        float nearest = maxDistance;
        for (int i = 0; i < hits.Count; i++)
        {
            if (IsObstacle(hits[i]) && hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
            }
        }
        return nearest;
    }

    bool IsObstacle(RaycastHit2D hit)
    {
        Collider2D col = hit.collider;
        if (col == null || col.isTrigger) return false;

        // Kendi collider'ımız engel değil
        if (col.transform.IsChildOf(transform)) return false;

        // Hareketli karakterler (Player, Slime) engel sayılmaz; duvar/çit gibi statik şeyler sayılır
        Rigidbody2D body = col.attachedRigidbody;
        if (body != null && body.bodyType != RigidbodyType2D.Static) return false;

        return true;
    }

    static Vector2 Direction(int index, int count)
    {
        float angle = index * Mathf.PI * 2f / count;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // Companion seçiliyken Scene ekranında ışınları gösterir (oyun çalışırken)
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !Application.isPlaying || rayDistances == null) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position;
        for (int i = 0; i < rayDistances.Length; i++)
        {
            Vector2 dir = Direction(i, rayDistances.Length);
            Gizmos.DrawLine(origin, origin + (Vector3)(dir * rayDistances[i]));
        }
    }
}
