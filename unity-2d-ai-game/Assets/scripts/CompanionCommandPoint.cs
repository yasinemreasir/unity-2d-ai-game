using UnityEngine;
using UnityEngine.InputSystem;

// Çitin yanındaki bölge. Oyuncu bölgedeyken E'ye basarsa companion'a "plakaya git" emri verilir.
[RequireComponent(typeof(Collider2D))]
public class CompanionCommandPoint : MonoBehaviour
{
    [SerializeField] CompanionController companion;

    [Tooltip("Emirde gidilecek hedef. IInteractable uygulayan bir bileşen (PressurePlate) sürüklenmeli.")]
    [SerializeField] MonoBehaviour targetBehaviour;

    [Tooltip("Bölgedeyken gösterilecek ipucu objesi (opsiyonel, örn. 'E: Emret' yazısı).")]
    [SerializeField] GameObject hint;

    [SerializeField] Key commandKey = Key.E;

    IInteractable target;
    bool playerInside;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        target = targetBehaviour as IInteractable;
        if (target == null)
        {
            Debug.LogError("CompanionCommandPoint: 'Target Behaviour' alanına IInteractable uygulayan bir bileşen (PressurePlate) sürüklenmeli.", this);
        }

        SetHint(false);
    }

    void Update()
    {
        if (target == null || companion == null) return;

        bool canCommand = playerInside && target.CanInteract && !companion.IsBusy;
        SetHint(canCommand);

        if (!canCommand) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[commandKey].wasPressedThisFrame)
        {
            companion.GiveCommand(target);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    void OnDisable()
    {
        playerInside = false;
    }

    void SetHint(bool show)
    {
        if (hint != null && hint.activeSelf != show)
        {
            hint.SetActive(show);
        }
    }
}
