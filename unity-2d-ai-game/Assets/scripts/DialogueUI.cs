using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Diyalog paneli (sadece görünüm): yazı kutusu, gönder butonu, konuşma metni ve "Düşünüyor..." göstergesi.
// İş mantığını bilmez; oyuncu mesaj gönderince MessageSubmitted olayını tetikler.
// NOT: Bu script AKTİF bir objede durmalı (örn. Canvas). Panel ise ayrı bir alt obje olup kapalı başlar.
public class DialogueUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text logText;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button sendButton;
    [Tooltip("Cevap beklenirken gösterilen 'Düşünüyor...' yazısı.")]
    [SerializeField] TMP_Text thinkingText;
    [SerializeField] int maxLines = 8;

    readonly List<string> lines = new List<string>();
    bool busy;

    public event Action<string> MessageSubmitted;

    public bool IsOpen
    {
        get { return panel != null && panel.activeSelf; }
    }

    void Awake()
    {
        if (sendButton != null) sendButton.onClick.AddListener(Submit);
        if (inputField != null) inputField.onSubmit.AddListener(OnInputSubmitted);

        if (panel != null) panel.SetActive(false);
        if (thinkingText != null) thinkingText.gameObject.SetActive(false);
    }

    void Update()
    {
        // "Düşünüyor", "Düşünüyor.", "Düşünüyor.." ... animasyonu
        if (!busy || thinkingText == null) return;

        int dots = (int)(Time.unscaledTime * 2.5f) % 4;
        thinkingText.text = "Düşünüyor" + new string('.', dots);
    }

    public void Show()
    {
        lines.Clear();
        RefreshLog();

        if (inputField != null) inputField.text = string.Empty;

        panel.SetActive(true);
        SetBusy(false);
    }

    public void Hide()
    {
        SetBusy(false);
        if (panel != null) panel.SetActive(false);
    }

    public void AppendLine(string speaker, string text)
    {
        AddLine("<b>" + speaker + ":</b> " + Sanitize(text));
    }

    // Hata / bilgi mesajı (kırmızımsı, italik)
    public void AppendSystemLine(string text)
    {
        AddLine("<i><color=#E57373>" + Sanitize(text) + "</color></i>");
    }

    public void SetBusy(bool value)
    {
        busy = value;

        if (sendButton != null) sendButton.interactable = !value;
        if (inputField != null) inputField.interactable = !value;

        if (thinkingText != null)
        {
            thinkingText.gameObject.SetActive(value);
            if (value) thinkingText.text = "Düşünüyor";
        }

        if (!value) FocusInput();
    }

    void OnInputSubmitted(string ignored)
    {
        Submit();
    }

    void Submit()
    {
        if (busy || inputField == null) return;

        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        inputField.text = string.Empty;
        AppendLine("Sen", text);

        if (MessageSubmitted != null)
        {
            MessageSubmitted(text);
        }
    }

    void AddLine(string line)
    {
        lines.Add(line);
        while (lines.Count > maxLines)
        {
            lines.RemoveAt(0);
        }
        RefreshLog();
    }

    void RefreshLog()
    {
        if (logText != null)
        {
            logText.text = string.Join("\n", lines);
        }
    }

    // Odaklamayı bir kare sonraya erteliyoruz: diyaloğu açan 'F' tuşu yazı kutusuna "f" olarak yazılmasın.
    void FocusInput()
    {
        if (!isActiveAndEnabled) return;
        StartCoroutine(FocusNextFrame());
    }

    IEnumerator FocusNextFrame()
    {
        yield return null;

        if (inputField != null && IsOpen && !busy)
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    // Oyuncunun yazdığı '<' karakteri TMP etiketlerini bozmasın
    static string Sanitize(string text)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : text.Replace("<", "‹");
    }
}
