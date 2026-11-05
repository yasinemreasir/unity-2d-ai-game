using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// NPC'nin "beyni": konuşma geçmişini tutar, ILlmClient ile modele sorar, cevabı JSON olarak ayrıştırır
// ve sonucu olaylar (event) ile duyurur. Arayüzden (UI) ve oyun nesnelerinden habersizdir (SRP).
public class NpcBrain : MonoBehaviour
{
    [Header("LLM bağlantısı")]
    [Tooltip("ILlmClient uygulayan bileşen (OllamaClient) sürüklenmeli.")]
    [SerializeField] MonoBehaviour llmClientBehaviour;

    [Header("Kişilik")]
    [Tooltip("NPC'nin system prompt'u (kişilik, amaç, kurallar, JSON formatı). AgacPrompt.txt dosyasını sürükle.")]
    [SerializeField] TextAsset systemPromptAsset;
    [TextArea(2, 4)]
    [SerializeField] string greeting = "Hmm... Bir yolcu. Kütüklerin önünde ne arıyorsun?";

    [Header("Davranış")]
    [Tooltip("Modele gönderilen en fazla mesaj sayısı (uzun geçmiş yavaşlatır).")]
    [SerializeField] int maxHistoryMessages = 8;
    [Tooltip("Model geçersiz JSON üretirse toplam kaç kez denensin.")]
    [SerializeField] int maxParseAttempts = 2;
    [Tooltip("Oyun başlarken modeli arka planda belleğe yükler (ilk cevap gecikmesini azaltır).")]
    [SerializeField] bool warmUpOnStart = true;
    [Tooltip("Konuşmaları ve cevap sürelerini Console'a yazar (test ve rapor için).")]
    [SerializeField] bool logConversation = true;

    const string FallbackPrompt =
        "You are a wise talking tree. Answer in Turkish with at most 2 short sentences. " +
        "Reply ONLY with JSON: {\"reply\": string, \"action\": \"none\" or \"remove_logs\"}.";

    ILlmClient client;
    CancellationTokenSource lifetimeCts;
    CancellationTokenSource requestCts;

    readonly List<ChatMessage> history = new List<ChatMessage>();
    readonly HashSet<NpcAction> consumedActions = new HashSet<NpcAction>();

    public bool IsBusy { get; private set; }
    public string Greeting { get { return greeting; } }

    // Cevap geldi (oyuncuya gösterilecek metin)
    public event Action<string> Replied;
    // Model bir oyun eylemi seçti (her eylem en fazla bir kez tetiklenir)
    public event Action<NpcAction> ActionTriggered;
    // Hata oldu (oyuncuya gösterilecek anlamlı mesaj)
    public event Action<string> Failed;
    // İstek başladı/bitti ("Düşünüyor..." göstergesi için)
    public event Action<bool> BusyChanged;

    void Awake()
    {
        lifetimeCts = new CancellationTokenSource();

        client = llmClientBehaviour as ILlmClient;
        if (client == null)
        {
            Debug.LogError("NpcBrain: 'Llm Client Behaviour' alanına ILlmClient uygulayan bir bileşen (OllamaClient) sürüklenmeli.", this);
        }

        if (systemPromptAsset == null)
        {
            Debug.LogWarning("NpcBrain: System Prompt Asset atanmamış, çok basit bir yedek prompt kullanılacak. AgacPrompt.txt'yi sürükle.", this);
        }
    }

    void Start()
    {
        if (warmUpOnStart && client != null)
        {
            _ = WarmUpAsync();
        }
    }

    void OnDestroy()
    {
        if (requestCts != null) requestCts.Cancel();
        if (lifetimeCts != null)
        {
            lifetimeCts.Cancel();
            lifetimeCts.Dispose();
        }
    }

    // Diyalog kapanırken bekleyen isteği iptal eder.
    public void CancelPending()
    {
        if (requestCts != null)
        {
            requestCts.Cancel();
        }
    }

    // Konuşma geçmişini ve "tetiklenmiş eylemler" kaydını sıfırlar (diyalog test modu her denemede kullanır).
    public void ResetConversation()
    {
        CancelPending();
        history.Clear();
        consumedActions.Clear();
    }

    // Oyuncunun yazdığı mesajı modele gönderir. Sonuç olaylarla bildirilir. Bu metot exception fırlatmaz.
    public async Task SendPlayerMessageAsync(string playerText)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(playerText)) return;

        if (client == null)
        {
            RaiseFailed("Ağaç bağlanamıyor: LLM istemcisi atanmamış (NpcBrain > Llm Client Behaviour).");
            return;
        }

        SetBusy(true);
        requestCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCts.Token);
        CancellationToken token = requestCts.Token;

        history.Add(new ChatMessage("user", playerText.Trim()));
        TrimHistory();

        try
        {
            int attempts = Mathf.Max(1, maxParseAttempts);

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                float startedAt = Time.realtimeSinceStartup;
                LlmResult result = await client.ChatAsync(BuildMessages(), NpcResponseParser.JsonSchema, token);
                float elapsed = Time.realtimeSinceStartup - startedAt;

                if (token.IsCancellationRequested || result.Error == LlmErrorKind.Cancelled)
                {
                    RollbackUserMessage();
                    return;
                }

                if (!result.Success)
                {
                    // Sunucu/ağ hatalarında tekrar denemenin anlamı yok; oyuncuya bildir.
                    Debug.LogWarning("[NpcBrain] LLM hatası (" + result.Error + "): " + result.Detail, this);
                    RollbackUserMessage();
                    RaiseFailed(DescribeError(result));
                    return;
                }

                NpcReply reply;
                if (NpcResponseParser.TryParse(result.Content, out reply))
                {
                    if (logConversation)
                    {
                        Debug.Log("[NpcBrain] Oyuncu: " + playerText.Trim()
                            + "\n[NpcBrain] Ağaç (" + elapsed.ToString("F1") + " sn, eylem=" + reply.Action + "): " + reply.Text, this);
                    }

                    Accept(result.Content, reply);
                    return;
                }

                // Model bozuk/geçersiz JSON üretti: bir kez daha dene
                Debug.LogWarning("[NpcBrain] Geçersiz JSON (deneme " + attempt + "/" + attempts + "): " + result.Content, this);
            }

            RollbackUserMessage();
            RaiseFailed("Ağaç söylediklerini toparlayamadı (anlaşılmaz cevap). Mesajını tekrar gönder.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            RollbackUserMessage();
            RaiseFailed("Ağaçla konuşurken beklenmedik bir sorun oldu. Tekrar dene.");
        }
        finally
        {
            if (requestCts != null)
            {
                requestCts.Dispose();
                requestCts = null;
            }
            SetBusy(false);
        }
    }

    void Accept(string rawContent, NpcReply reply)
    {
        // Geçmişe modelin ham JSON'unu koyuyoruz: model kendi formatını görerek tutarlı kalır.
        history.Add(new ChatMessage("assistant", rawContent));
        TrimHistory();

        if (Replied != null)
        {
            Replied(reply.Text);
        }

        // Her eylem oyunda en fazla bir kez gerçekleşir
        if (reply.Action != NpcAction.None && consumedActions.Add(reply.Action))
        {
            if (ActionTriggered != null)
            {
                ActionTriggered(reply.Action);
            }
        }
    }

    List<ChatMessage> BuildMessages()
    {
        List<ChatMessage> messages = new List<ChatMessage>(history.Count + 1);
        messages.Add(new ChatMessage("system", GetSystemPrompt()));
        messages.AddRange(history);
        return messages;
    }

    string GetSystemPrompt()
    {
        string prompt = systemPromptAsset != null ? systemPromptAsset.text : FallbackPrompt;

        // Oyun durumunu modele bildir: kütükler kalktıysa tekrar kaldırmaya çalışmasın
        if (consumedActions.Contains(NpcAction.RemoveLogs))
        {
            prompt += "\n\nCURRENT STATE: The logs have ALREADY been removed. Always use action \"none\" from now on.";
        }

        return prompt;
    }

    void TrimHistory()
    {
        while (history.Count > Mathf.Max(2, maxHistoryMessages))
        {
            history.RemoveAt(0);
        }

        // Geçmiş her zaman bir oyuncu mesajıyla başlasın
        while (history.Count > 0 && history[0].role != "user")
        {
            history.RemoveAt(0);
        }
    }

    // Başarısız istekte oyuncu mesajı geçmişte yetim kalmasın
    void RollbackUserMessage()
    {
        if (history.Count > 0 && history[history.Count - 1].role == "user")
        {
            history.RemoveAt(history.Count - 1);
        }
    }

    async Task WarmUpAsync()
    {
        try
        {
            List<ChatMessage> messages = new List<ChatMessage>();
            messages.Add(new ChatMessage("system", GetSystemPrompt()));
            messages.Add(new ChatMessage("user", "Merhaba"));

            float startedAt = Time.realtimeSinceStartup;
            LlmResult result = await client.ChatAsync(messages, NpcResponseParser.JsonSchema, lifetimeCts.Token);

            if (lifetimeCts.IsCancellationRequested) return;

            if (result.Success)
            {
                Debug.Log("[NpcBrain] Ollama hazır (ısınma " + (Time.realtimeSinceStartup - startedAt).ToString("F1") + " sn).", this);
            }
            else
            {
                Debug.LogWarning("[NpcBrain] Ollama ısınma isteği başarısız (" + result.Error + "): " + result.Detail
                    + "\nOyun çalışmaya devam eder; Ağaç ile konuşurken tekrar denenir.", this);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[NpcBrain] Isınma isteği hatası: " + exception.Message, this);
        }
    }

    static string DescribeError(LlmResult result)
    {
        switch (result.Error)
        {
            case LlmErrorKind.ServerUnreachable:
                return "Ağaç sessiz kaldı: Ollama sunucusuna ulaşılamadı. Ollama'nın açık olduğundan emin ol ve tekrar dene.";
            case LlmErrorKind.Timeout:
                return "Ağaç çok uzun süre düşündü ve cevap veremedi (zaman aşımı). Tekrar dene.";
            case LlmErrorKind.ModelNotFound:
                return "Ağaç konuşamıyor: model yüklü değil. Terminalde 'ollama pull <model adı>' komutunu çalıştır.";
            case LlmErrorKind.HttpError:
                return "Ağaç bir hata aldı (" + result.Detail + "). Tekrar dene.";
            default:
                return "Ağaçtan anlaşılmaz bir cevap geldi. Tekrar dene.";
        }
    }

    void RaiseFailed(string message)
    {
        if (lifetimeCts.IsCancellationRequested) return;

        if (Failed != null)
        {
            Failed(message);
        }
    }

    void SetBusy(bool busy)
    {
        IsBusy = busy;

        // Sahne kapanırken yok edilmiş arayüzlere olay göndermeyelim
        if (lifetimeCts != null && lifetimeCts.IsCancellationRequested) return;

        if (BusyChanged != null)
        {
            BusyChanged(busy);
        }
    }
}
