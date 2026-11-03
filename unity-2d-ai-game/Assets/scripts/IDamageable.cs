// Hasar alabilen her şeyin (oyuncu, slime, boss) uyguladığı ortak arayüz.
// SwordAttack gibi sınıflar artık somut Enemy sınıfına değil bu arayüze bağımlıdır
// (SOLID: Dependency Inversion). Yeni bir hasar alan tür eklemek için kılıç koduna dokunmak gerekmez.
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(float amount);
}
