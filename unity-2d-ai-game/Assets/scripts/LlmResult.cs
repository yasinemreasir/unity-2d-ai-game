// Bir LLM isteğinin neden başarısız olduğunu ayırt etmek için hata türleri.
// Kullanıcıya gösterilecek anlamlı mesaj bu türe göre seçilir.
public enum LlmErrorKind
{
    None,
    ServerUnreachable,  // Ollama kapalı / adres yanlış
    Timeout,            // Zaman aşımı
    ModelNotFound,      // Model yüklü değil (ollama pull yapılmamış)
    HttpError,          // Sunucu başka bir HTTP hatası döndü
    InvalidResponse,    // Cevap okunamadı / boş
    Cancelled           // İstek bilerek iptal edildi (diyalog kapandı)
}

// ChatAsync'in sonucu: ya başarılı bir metin ya da hata türü + ayrıntı. İstemci asla exception fırlatmaz.
public class LlmResult
{
    public bool Success { get; private set; }
    public string Content { get; private set; }
    public LlmErrorKind Error { get; private set; }
    public string Detail { get; private set; }

    public static LlmResult Ok(string content)
    {
        LlmResult result = new LlmResult();
        result.Success = true;
        result.Content = content;
        result.Error = LlmErrorKind.None;
        return result;
    }

    public static LlmResult Fail(LlmErrorKind error, string detail)
    {
        LlmResult result = new LlmResult();
        result.Success = false;
        result.Error = error;
        result.Detail = detail;
        return result;
    }
}
