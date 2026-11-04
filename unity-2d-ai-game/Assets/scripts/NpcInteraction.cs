using UnityEngine;
using UnityEngine.InputSystem;

// Ağacın yanındaki "konuşma bölgesi". Oyuncu bölgedeyken F ile diyalog açılır, Esc ile kapanır.
// Diyalog açıkken oyuncunun hareketi ve saldırısı kilitlenir (yazı yazarken karakter yürümesin).
[RequireComponent(typeof(Collider2D))]
public class NpcInteraction : MonoBehaviour
{
    [SerializeField] NpcBrain brain;
    [SerializeField] DialogueUI dialogueUI;

    [SerializeField] string npcName = "Yaşlı Ağaç";
    [Tooltip("Bölgedeyken gösterilecek ipucu objesi (opsiyonel, örn. 'F: Konuş' yazısı).")]
    [SerializeField] GameObject hint;

    [SerializeField] Key talkKey = Key.F;
    [SerializeField] Key closeKey = Key.Escape;

    PlayerController player;
    bool playerInside;
    bool talking;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetHint(false);
    }

    void OnEnable()
    {
        if (brain != null)
        {
            brain.Replied += HandleReplied;
            brain.Failed += HandleFailed;
            brain.BusyChanged += HandleBusyChanged;
        }

        if (dialogueUI != null)
        {
            dialogueUI.MessageSubmitted += HandleMessageSubmitted;
        }
    }

    void OnDisable()
    {
        if (brain != null)
        {
            brain.Replied -= HandleReplied;
            brain.Failed -= HandleFailed;
            brain.BusyChanged -= HandleBusyChanged;
        }

        if (dialogueUI != null)
        {
            dialogueUI.MessageSubmitted -= HandleMessageSubmitted;
        }

        if (talking)
        {
            EndConversation();
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (talking)
        {
            if (keyboard[closeKey].wasPressedThisFrame)
            {
                EndConversation();
            }
            return;
        }

        SetHint(playerInside && player != null && player.IsAlive);

        if (playerInside && player != null && player.IsAlive && keyboard[talkKey].wasPressedThisFrame)
        {
            BeginConversation();
        }
    }

    void BeginConversation()
    {
        if (brain == null || dialogueUI == null) return;

        talking = true;
        SetHint(false);

        player.SetInputLocked(true);
        dialogueUI.Show();
        dialogueUI.AppendLine(npcName, brain.Greeting);
    }

    void EndConversation()
    {
        talking = false;

        if (brain != null) brain.CancelPending();
        if (dialogueUI != null) dialogueUI.Hide();
        if (player != null) player.SetInputLocked(false);
    }

    void HandleMessageSubmitted(string text)
    {
        if (!talking || brain == null) return;

        // Task'ı beklemiyoruz (ana akış bloklanmaz); sonuç olaylarla gelir.
        _ = brain.SendPlayerMessageAsync(text);
    }

    void HandleReplied(string text)
    {
        if (talking && dialogueUI != null) dialogueUI.AppendLine(npcName, text);
    }

    void HandleFailed(string message)
    {
        if (talking && dialogueUI != null) dialogueUI.AppendSystemLine(message);
    }

    void HandleBusyChanged(bool busy)
    {
        if (talking && dialogueUI != null) dialogueUI.SetBusy(busy);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        player = other.GetComponentInParent<PlayerController>();
        playerInside = player != null;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        SetHint(false);
    }

    void SetHint(bool show)
    {
        if (hint != null && hint.activeSelf != show)
        {
            hint.SetActive(show);
        }
    }
}
