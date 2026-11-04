using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Yerel Ollama sunucusuyla (POST /api/chat) konuşan ILlmClient uygulaması.
// Tamamen asenkrondur: istek sürerken oyun donmaz.
public class OllamaClient : MonoBehaviour, ILlmClient
{
    [Header("Sunucu")]
    [SerializeField] string baseUrl = "http://127.0.0.1:11434";
    [Tooltip("Terminalde 'ollama list' ile gördüğün tam model adı.")]
    [SerializeField] string model = "llama3.2:3b";

    [Header("Dayanıklılık")]
    [Tooltip("Bu süre içinde cevap gelmezse istek iptal edilir. İlk istekte model belleğe yüklendiği için uzun tut.")]
    [SerializeField] int timeoutSeconds = 60;
    [Tooltip("Model son istekten sonra bellekte ne kadar kalsın (ilk cevap gecikmesini azaltır).")]
    [SerializeField] string keepAlive = "30m";

    [Header("Üretim")]
    [SerializeField, Range(0f, 1.5f)] float temperature = 0.4f;
    [Tooltip("Cevabın en fazla kaç token olacağı. Kısa tutmak gecikmeyi azaltır.")]
    [SerializeField] int maxTokens = 200;

    public async Task<LlmResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellationToken)
    {
        string url = baseUrl.TrimEnd('/') + "/api/chat";
        string body = BuildRequestJson(messages, jsonSchema);

        try
        {
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();

                // UnityWebRequest'i Task'a çeviriyoruz: bitince 'completed' olayı Task'ı tamamlar.
                TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
                operation.completed += _ => completion.TrySetResult(true);

                using (cancellationToken.Register(() => { if (!request.isDone) request.Abort(); }))
                {
                    await completion.Task;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return LlmResult.Fail(LlmErrorKind.Cancelled, "İstek iptal edildi.");
                }

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                        return ParseSuccess(request.downloadHandler.text);

                    case UnityWebRequest.Result.ProtocolError:
                        return MapHttpError(request);

                    case UnityWebRequest.Result.DataProcessingError:
                        return LlmResult.Fail(LlmErrorKind.InvalidResponse, request.error);

                    default: // ConnectionError: sunucuya ulaşılamadı ya da zaman aşımı
                        bool isTimeout = request.error != null
                            && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
                        return LlmResult.Fail(isTimeout ? LlmErrorKind.Timeout : LlmErrorKind.ServerUnreachable, request.error);
                }
            }
        }
        catch (Exception exception)
        {
            // Beklenmeyen her şey LlmResult'a çevrilir; oyun asla bu yüzden çökmez.
            return LlmResult.Fail(LlmErrorKind.InvalidResponse, exception.Message);
        }
    }

    string BuildRequestJson(IReadOnlyList<ChatMessage> messages, string jsonSchema)
    {
        ChatMessage[] array = new ChatMessage[messages.Count];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = messages[i];
        }

        OllamaRequest request = new OllamaRequest();
        request.model = model;
        request.messages = array;
        request.stream = false;
        request.keep_alive = keepAlive;
        request.options = new OllamaOptions();
        request.options.temperature = temperature;
        request.options.num_predict = maxTokens;

        string json = JsonUtility.ToJson(request);

        // JsonUtility serbest bir JSON nesnesini (şema) yazamadığı için "format" alanını elle ekliyoruz.
        if (!string.IsNullOrEmpty(jsonSchema))
        {
            json = json.Substring(0, json.Length - 1) + ",\"format\":" + jsonSchema + "}";
        }

        return json;
    }

    static LlmResult ParseSuccess(string json)
    {
        OllamaChatResponse response;
        try
        {
            response = JsonUtility.FromJson<OllamaChatResponse>(json);
        }
        catch (Exception exception)
        {
            return LlmResult.Fail(LlmErrorKind.InvalidResponse, "Sunucu cevabı okunamadı: " + exception.Message);
        }

        if (response == null || response.message == null || string.IsNullOrEmpty(response.message.content))
        {
            return LlmResult.Fail(LlmErrorKind.InvalidResponse, "Sunucu boş cevap döndürdü.");
        }

        return LlmResult.Ok(response.message.content);
    }

    static LlmResult MapHttpError(UnityWebRequest request)
    {
        long code = request.responseCode;
        string serverMessage = ReadServerError(request.downloadHandler != null ? request.downloadHandler.text : null);

        if (code == 404)
        {
            return LlmResult.Fail(LlmErrorKind.ModelNotFound, serverMessage ?? "Model bulunamadı (HTTP 404).");
        }

        string detail = "HTTP " + code;
        if (!string.IsNullOrEmpty(serverMessage))
        {
            detail += ": " + serverMessage;
        }
        return LlmResult.Fail(LlmErrorKind.HttpError, detail);
    }

    // Ollama hata gövdesi: {"error":"..."}
    static string ReadServerError(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            OllamaChatResponse response = JsonUtility.FromJson<OllamaChatResponse>(json);
            return response != null ? response.error : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

// --- Ollama JSON veri sınıfları (JsonUtility için) ---

[Serializable]
class OllamaOptions
{
    public float temperature;
    public int num_predict;
}

[Serializable]
class OllamaRequest
{
    public string model;
    public ChatMessage[] messages;
    public bool stream;
    public string keep_alive;
    public OllamaOptions options;
}

[Serializable]
class OllamaChatResponse
{
    public ChatMessage message;
    public string error;
}
