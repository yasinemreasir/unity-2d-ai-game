using UnityEngine;

// Companion'ın gidip etkileşime girebileceği her şey (basınç plakası, şalter, vb.).
public interface IInteractable
{
    // Şu an etkileşime girilebilir mi? (örn. plaka zaten basılıysa false)
    bool CanInteract { get; }

    // Companion'ın NavMesh üzerinde gideceği nokta.
    Vector2 InteractPosition { get; }

    void Interact(GameObject actor);
}
