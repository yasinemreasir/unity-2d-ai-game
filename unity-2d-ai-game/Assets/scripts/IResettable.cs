// Başlangıç durumuna döndürülebilen nesneler (basınç plakası, çit kapısı...).
// Test modları her denemeden önce bu arayüz üzerinden sahneyi sıfırlar.
public interface IResettable
{
    void ResetState();
}
