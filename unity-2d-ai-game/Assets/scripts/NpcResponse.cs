using System;
using UnityEngine;

// LLM'in tetikleyebileceği oyun eylemleri. Model yalnızca bu listedeki eylemleri seçebilir (whitelist).
public enum NpcAction
{
    None,
    RemoveLogs
}

// Modelin döndürmesi zorunlu olan JSON'un veri sınıfı: {"reply":"...","action":"..."}
[Serializable]
public class NpcResponse
{
    public string reply;
    public string action;
}

// Ayrıştırılıp doğrulanmış, oyunun güvenle kullanabileceği cevap.
public class NpcReply
{
    public string Text { get; private set; }
    public NpcAction Action { get; private set; }

    public NpcReply(string text, NpcAction action)
    {
        Text = text;
        Action = action;
    }
}

// Model çıktısını güvenle NpcReply'a çevirir. Bozuk JSON, eksik alan veya bilinmeyen eylem durumunda
// false döner (oyun çökmez); bilinmeyen eylem "None" sayılır.
public static class NpcResponseParser
{
    // Ollama'ya "format" olarak verilen şema: modelin çıktısı bu yapıya uymak zorundadır.
    public const string JsonSchema =
        "{\"type\":\"object\",\"properties\":{\"reply\":{\"type\":\"string\"},\"action\":{\"type\":\"string\",\"enum\":[\"none\",\"remove_logs\"]}},\"required\":[\"reply\",\"action\"]}";

    public static bool TryParse(string raw, out NpcReply reply)
    {
        reply = null;

        if (string.IsNullOrWhiteSpace(raw)) return false;

        string json = ExtractJsonObject(raw);
        if (json == null) return false;

        NpcResponse dto;
        try
        {
            dto = JsonUtility.FromJson<NpcResponse>(json);
        }
        catch (Exception)
        {
            return false;
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.reply)) return false;

        reply = new NpcReply(dto.reply.Trim(), ParseAction(dto.action));
        return true;
    }

    static NpcAction ParseAction(string action)
    {
        if (string.IsNullOrEmpty(action)) return NpcAction.None;

        switch (action.Trim().ToLowerInvariant())
        {
            case "remove_logs":
                return NpcAction.RemoveLogs;
            default:
                return NpcAction.None;
        }
    }

    // Model JSON'un etrafına metin eklerse ilk '{' ile son '}' arasını alır.
    static string ExtractJsonObject(string text)
    {
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');

        if (start < 0 || end <= start) return null;
        return text.Substring(start, end - start + 1);
    }
}
