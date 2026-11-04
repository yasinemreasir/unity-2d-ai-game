using System;

// Ollama /api/chat mesaj formatı. JsonUtility ile serileştirildiği için alan adları küçük harflidir.
[Serializable]
public class ChatMessage
{
    public string role;     // "system", "user" veya "assistant"
    public string content;

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}
