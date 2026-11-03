using UnityEngine;
using UnityEngine.AI;

// Durum 1: Oyuncuyu takip et (varsayılan durum).
public class FollowState : ICompanionState
{
    float nextRepathTime;

    public string Name { get { return "Follow"; } }

    public void Enter(CompanionController c)
    {
        nextRepathTime = 0f;

        if (!c.Agent.isOnNavMesh) return;
        c.Agent.isStopped = false;
        c.Agent.stoppingDistance = c.followDistance;
    }

    public void Tick(CompanionController c)
    {
        if (c.Player == null || !c.Agent.isOnNavMesh) return;

        // Her karede değil, belirli aralıkla yeniden yol hesapla (performans)
        if (Time.time < nextRepathTime) return;
        nextRepathTime = Time.time + c.repathInterval;

        float distance = Vector2.Distance(c.transform.position, c.Player.position);
        if (distance > c.followDistance)
        {
            c.Agent.SetDestination(c.Player.position);
        }
        else
        {
            c.Agent.ResetPath();
        }
    }

    public void Exit(CompanionController c) { }
}

// Durum 2: Oyuncunun verdiği emirdeki hedefe NavMesh ile git.
public class MoveToTargetState : ICompanionState
{
    float startTime;
    float lastProgressTime;
    float bestRemaining;
    bool destinationSet;

    public string Name { get { return "MoveToTarget"; } }

    public void Enter(CompanionController c)
    {
        startTime = Time.time;
        lastProgressTime = Time.time;
        bestRemaining = float.MaxValue;
        destinationSet = false;

        if (c.CurrentTarget == null || !c.Agent.isOnNavMesh) return;

        c.Agent.isStopped = false;
        c.Agent.stoppingDistance = c.interactRange * 0.5f;
        destinationSet = c.Agent.SetDestination(c.CurrentTarget.InteractPosition);
    }

    public void Tick(CompanionController c)
    {
        if (!destinationSet)
        {
            Fail(c, "hedef belirlenemedi (companion NavMesh üzerinde mi?)");
            return;
        }

        if (Time.time - startTime > c.commandTimeout)
        {
            Fail(c, "zaman aşımı");
            return;
        }

        NavMeshAgent agent = c.Agent;
        if (agent.pathPending) return;

        // Hedefe tam bir yol yoksa (örn. çit tamamen kapalıysa) emir başarısız olur
        NavMeshPathStatus status = agent.pathStatus;
        if (status != NavMeshPathStatus.PathComplete)
        {
            Fail(c, "hedefe giden yol bulunamadı (" + status + ")");
            return;
        }

        float remaining = agent.remainingDistance;

        // Vardık mı? Sensörle hedefe giden hattın açık olduğunu da doğrula
        // (çitin diğer tarafından plakaya "basmış" sayılmasın)
        if (remaining <= c.interactRange)
        {
            CompanionObservation observation = c.Sensor.Observe(c.CurrentTarget.InteractPosition);
            if (observation.TargetVisible)
            {
                c.ChangeState(c.Interacting);
                return;
            }
        }

        // Takılma tespiti: uzun süre yaklaşamıyorsa pes et
        if (remaining < bestRemaining - 0.05f)
        {
            bestRemaining = remaining;
            lastProgressTime = Time.time;
        }
        else if (Time.time - lastProgressTime > c.stuckSeconds)
        {
            Fail(c, "ilerleyemiyor (takıldı)");
        }
    }

    public void Exit(CompanionController c) { }

    void Fail(CompanionController c, string reason)
    {
        Debug.LogWarning("[Companion] Emir başarısız: " + reason, c);
        c.FinishCommand(false);
    }
}

// Durum 3: Hedefe vardı, etkileşime gir (plakaya bas) ve kısa süre dur.
public class InteractState : ICompanionState
{
    float endTime;

    public string Name { get { return "Interact"; } }

    public void Enter(CompanionController c)
    {
        endTime = Time.time + c.interactDuration;

        if (c.Agent.isOnNavMesh)
        {
            c.Agent.ResetPath();
        }

        IInteractable target = c.CurrentTarget;
        if (target != null && target.CanInteract)
        {
            target.Interact(c.gameObject);
        }
    }

    public void Tick(CompanionController c)
    {
        if (Time.time >= endTime)
        {
            c.FinishCommand(true);
        }
    }

    public void Exit(CompanionController c) { }
}
