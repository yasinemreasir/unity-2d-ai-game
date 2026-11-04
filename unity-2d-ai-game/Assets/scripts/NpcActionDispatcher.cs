using System;
using UnityEngine;

// NpcBrain'in seçtiği eylemi (NpcAction) oyun dünyasındaki nesnelere bağlar.
// Örn. RemoveLogs -> üzerinde FenceGate (IActivatable) olan "kütükler" objesini açar.
// Böylece LLM çıktısı ile oyun durumu arasındaki bağ Inspector'da, kodsuz ve kontrollü kurulur.
public class NpcActionDispatcher : MonoBehaviour
{
    [Serializable]
    public class Binding
    {
        public NpcAction action;
        [Tooltip("Eylem gerçekleşince Activate() çağrılacak objeler (IActivatable içermeli, örn. FenceGate).")]
        public GameObject[] targets;
    }

    [SerializeField] NpcBrain brain;
    [SerializeField] Binding[] bindings;

    void OnEnable()
    {
        if (brain != null)
        {
            brain.ActionTriggered += HandleAction;
        }
    }

    void OnDisable()
    {
        if (brain != null)
        {
            brain.ActionTriggered -= HandleAction;
        }
    }

    void HandleAction(NpcAction action)
    {
        if (bindings == null) return;

        foreach (Binding binding in bindings)
        {
            if (binding.action != action || binding.targets == null) continue;

            foreach (GameObject target in binding.targets)
            {
                if (target == null) continue;

                foreach (IActivatable activatable in target.GetComponents<IActivatable>())
                {
                    activatable.Activate();
                }
            }
        }

        Debug.Log("[Eylem] Ağaç eylemi gerçekleştirdi: " + action, this);
    }
}
