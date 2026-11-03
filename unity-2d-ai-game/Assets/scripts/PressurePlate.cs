using UnityEngine;

// Companion'ın gidip bastığı basınç plakası. Basılınca bağlı IActivatable'ları (örn. FenceGate) açar.
public class PressurePlate : MonoBehaviour, IInteractable
{
    [Tooltip("Plaka basılınca açılacak objeler (üzerlerinde FenceGate gibi IActivatable bileşeni olmalı).")]
    [SerializeField] GameObject[] targets;

    [Tooltip("Companion'ın duracağı nokta. Boşsa plakanın kendi konumu kullanılır.")]
    [SerializeField] Transform standPoint;

    [Header("Görünüm (opsiyonel)")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite pressedSprite;

    bool pressed;

    public bool CanInteract { get { return !pressed; } }

    public Vector2 InteractPosition
    {
        get { return standPoint != null ? (Vector2)standPoint.position : (Vector2)transform.position; }
    }

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Interact(GameObject actor)
    {
        if (pressed) return;
        pressed = true;

        Debug.Log("[Plaka] Basıldı: " + actor.name, this);

        if (spriteRenderer != null && pressedSprite != null)
        {
            spriteRenderer.sprite = pressedSprite;
        }

        if (targets == null) return;

        foreach (GameObject target in targets)
        {
            if (target == null) continue;

            foreach (IActivatable activatable in target.GetComponents<IActivatable>())
            {
                activatable.Activate();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(InteractPosition, 0.2f);
    }
}
