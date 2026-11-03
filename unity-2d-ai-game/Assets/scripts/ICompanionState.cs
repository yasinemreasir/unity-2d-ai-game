// Companion durum makinesinin (State Pattern) her durumu bu arayüzü uygular.
// Yeni bir davranış eklemek için CompanionController'a dokunmadan yeni bir durum yazmak yeterlidir (Open/Closed).
public interface ICompanionState
{
    string Name { get; }

    void Enter(CompanionController companion);
    void Tick(CompanionController companion);
    void Exit(CompanionController companion);
}
