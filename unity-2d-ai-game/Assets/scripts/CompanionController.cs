using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Otonom companion: normalde oyuncuyu takip eder, emir gelince NavMesh ile hedefe gider ve etkileşime girer.
// Karar mantığı bir durum makinesidir: Follow -> MoveToTarget -> Interact -> Follow
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CompanionSensor))]
public class CompanionController : MonoBehaviour
{
    [Header("Takip")]
    public Transform player;              // Boşsa "Player" tag'li obje otomatik bulunur
    public float followDistance = 1.2f;   // Oyuncuya bu kadar yaklaşınca durur
    public float repathInterval = 0.25f;  // Takipte yolu kaç sn'de bir yeniden hesaplar

    [Header("Emir")]
    public float interactRange = 0.4f;    // Hedefe bu kadar yaklaşınca "vardım" sayılır
    public float interactDuration = 0.6f; // Plakanın üstünde kaç sn durur
    public float commandTimeout = 20f;    // Bu sürede varamazsa emir başarısız olur
    public float stuckSeconds = 3f;       // Bu sürede hiç yaklaşamazsa emir başarısız olur

    [Header("Görünüm / Debug")]
    public bool spriteFacesRight = true;
    public bool logStateChanges = true;

    NavMeshAgent agent;
    CompanionSensor sensor;
    Animator animator;
    SpriteRenderer spriteRenderer;
    ICompanionState currentState;

    // Animator'da olmayan parametreler için hata/uyarı basmamak için
    readonly HashSet<string> animatorParams = new HashSet<string>();

    public FollowState Follow { get; private set; }
    public MoveToTargetState MovingToTarget { get; private set; }
    public InteractState Interacting { get; private set; }

    public NavMeshAgent Agent { get { return agent; } }
    public CompanionSensor Sensor { get { return sensor; } }
    public Transform Player { get { return player; } }
    public IInteractable CurrentTarget { get; private set; }
    public string CurrentStateName { get { return currentState != null ? currentState.Name : "-"; } }
    public bool IsBusy { get { return currentState == MovingToTarget || currentState == Interacting; } }

    // Emir bitince tetiklenir (true: başarılı, false: başarısız). Test modu bunu dinleyecek.
    public event System.Action<bool> CommandFinished;
    public event System.Action<string> StateChanged;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<CompanionSensor>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 2D için: ajan objeyi döndürmesin / yukarı eksene yatırmasın
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        Follow = new FollowState();
        MovingToTarget = new MoveToTargetState();
        Interacting = new InteractState();

        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                animatorParams.Add(p.name);
            }
        }
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Companion: 'Player' tag'li obje bulunamadı, takip edemez.", this);
            }
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Companion NavMesh üzerinde değil. NavMesh bake edildi mi ve companion mavi alanın üstünde mi?", this);
        }

        // Slime Animator'ını kullanıyorsak 'isAlive' varsayılanı false olduğu için ölüm animasyonuna girmesin
        SetBool("isAlive", true);

        ChangeState(Follow);
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.Tick(this);
        }

        UpdateAnimation();
    }

    // Oyuncunun emri: hedefe git ve etkileşime gir. Kabul edildiyse true döner.
    public bool GiveCommand(IInteractable target)
    {
        if (target == null || !target.CanInteract || IsBusy) return false;

        CurrentTarget = target;
        ChangeState(MovingToTarget);
        return true;
    }

    // Durumlar emrin bittiğini buradan bildirir; companion takibe döner.
    public void FinishCommand(bool success)
    {
        CurrentTarget = null;

        if (CommandFinished != null)
        {
            CommandFinished(success);
        }

        ChangeState(Follow);
    }

    public void ChangeState(ICompanionState next)
    {
        if (next == null || next == currentState) return;

        if (currentState != null)
        {
            currentState.Exit(this);
        }

        currentState = next;

        if (logStateChanges)
        {
            Debug.Log("[Companion] Durum: " + next.Name, this);
        }

        if (StateChanged != null)
        {
            StateChanged(next.Name);
        }

        next.Enter(this);
    }

    void UpdateAnimation()
    {
        Vector2 velocity = agent.velocity;

        SetBool("isMoving", velocity.sqrMagnitude > 0.01f);

        if (spriteRenderer != null && Mathf.Abs(velocity.x) > 0.05f)
        {
            bool movingLeft = velocity.x < 0;
            spriteRenderer.flipX = spriteFacesRight ? movingLeft : !movingLeft;
        }
    }

    void SetBool(string paramName, bool value)
    {
        if (animator != null && animatorParams.Contains(paramName))
        {
            animator.SetBool(paramName, value);
        }
    }
}
