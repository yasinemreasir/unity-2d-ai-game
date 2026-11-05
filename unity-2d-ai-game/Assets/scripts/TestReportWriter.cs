using System;
using System.IO;
using System.Text;
using UnityEngine;

// Test modlarının ortak yardımcısı: sonuçları CSV ve Markdown olarak diske yazar,
// Markdown'ı panoya da kopyalar (README'ye doğrudan yapıştırılabilir).
public static class TestReportWriter
{
    // Dosyaların yazıldığı klasörü döndürür (yazılamazsa null).
    public static string Save(string baseName, string csv, string markdown)
    {
        GUIUtility.systemCopyBuffer = markdown;

        try
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string folder = Application.persistentDataPath;

            // CSV'yi BOM'lu yazıyoruz: Excel Türkçe karakterleri doğru açsın
            File.WriteAllText(Path.Combine(folder, baseName + "_" + stamp + ".csv"), csv, new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(folder, baseName + "_" + stamp + ".md"), markdown, new UTF8Encoding(false));

            Debug.Log("[Test] Sonuçlar kaydedildi ve Markdown panoya kopyalandı. Klasör: " + folder);
            return folder;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Test] Sonuç dosyaları yazılamadı (Markdown yine de panoya kopyalandı): " + exception.Message);
            return null;
        }
    }

    // CSV hücresi: tırnak içine alır, içindeki tırnakları ikiler.
    public static string CsvCell(string value)
    {
        if (value == null) value = string.Empty;
        return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
    }

    // Markdown tablo hücresi: '|' ve satır sonlarını temizler.
    public static string MdCell(string value)
    {
        if (value == null) return string.Empty;
        return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
