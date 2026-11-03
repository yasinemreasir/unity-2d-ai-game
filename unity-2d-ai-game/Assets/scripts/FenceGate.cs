using UnityEngine;
using UnityEngine.Events;

// Plaka tarafından açılan çit. Belirlenen objeleri (çit parçaları, görünmez engel collider'ı vb.) kapatır.
public class FenceGate : MonoBehaviour, IActivatable
{
    [Tooltip("Açılınca SetActive(false) yapılacak objeler. Boşsa bu objenin kendisi kapatılır.")]
    [SerializeField] GameObject[] objectsToDisable;

    [Tooltip("Çit açılınca ek bir şey tetiklemek için (ses, efekt, kamera vb.).")]
    public UnityEvent onOpened;

    bool opened;

    public bool IsOpen { get { return opened; } }

    public void Activate()
    {
        if (opened) return;
        opened = true;

        Debug.Log("[Çit] Çit açıldı.", this);

        if (onOpened != null)
        {
            onOpened.Invoke();
        }

        if (objectsToDisable == null || objectsToDisable.Length == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        foreach (GameObject go in objectsToDisable)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }
    }
}
