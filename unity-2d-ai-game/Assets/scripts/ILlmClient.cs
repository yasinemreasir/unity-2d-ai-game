using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Dil modeliyle konuşan her istemcinin uyguladığı arayüz.
// NpcBrain yalnızca bu arayüzü bilir; Ollama yerine başka bir yerel sunucu yazmak NPC kodunu değiştirmez
// (SOLID: Dependency Inversion / Open-Closed).
public interface ILlmClient
{
    // jsonSchema doluysa model çıktısı bu JSON şemasına uymak zorundadır (yapılandırılmış çıktı).
    // Metot asla exception fırlatmaz; hatalar LlmResult içinde döner. İstek ana iş parçacığını bloklamaz.
    Task<LlmResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellationToken);
}
