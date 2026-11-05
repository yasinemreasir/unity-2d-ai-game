using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Companion test modu: companion'ı farklı (ama tohumlu, yani tekrarlanabilir) rastgele başlangıç konumlarından
// başlatıp plakaya gidip basmasını ölçer. Başarı oranı ve tamamlama süresini üretir (doküman madde 4.4).
// Kullanım: oyunu çalıştır, F9'a bas. Sonuç ekranda, Console'da ve CSV/Markdown dosyalarında.
public class CompanionTestRunner : MonoBehaviour
{
    class TrialResult
    {
        public int Index;
        public Vector2 Start;
        public bool Success;
        public float Seconds;
        public string Note = string.Empty;
    }

    [Header("Test hedefi")]
    [SerializeField] CompanionController companion;
    [Tooltip("Companion'ın gideceği plaka (IInteractable uygulayan bileşen: PressurePlate).")]
    [SerializeField] MonoBehaviour targetBehaviour;
    [Tooltip("Her denemeden önce başlangıç haline döndürülecek objeler (plaka ve çit kapısı). IResettable içermeli.")]
    [SerializeField] GameObject[] resetTargets;

    [Header("Rastgele başlangıç")]
    [Tooltip("Companion her denemede bu alanın içinde rastgele bir noktadan başlar (Bölge 1'de bir BoxCollider2D).")]
    [SerializeField] Collider2D startArea;
    [SerializeField] int trials = 20;
    [Tooltip("Aynı tohum aynı başlangıç konumlarını üretir: test tekrarlanabilir.")]
    [SerializeField] int randomSeed = 12345;
    [Tooltip("Bir denemenin en fazla sürebileceği süre (sn).")]
    [SerializeField] float maxTrialSeconds = 30f;
    [Tooltip("Rastgele nokta NavMesh'e bu mesafe içinde oturtulur.")]
    [SerializeField] float navMeshSnapDistance = 0.5f;

    [Header("Kontrol")]
    [SerializeField] Key startKey = Key.F9;
    [SerializeField] bool autoStart;

    readonly List<TrialResult> results = new List<TrialResult>();
    bool running;
    bool trialFinished;
    bool trialSuccess;
    int currentTrial;
    string summary = string.Empty;

    void Start()
    {
        if (autoStart)
        {
            StartTest();
        }
    }

    void Update()
    {
        if (running) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[startKey].wasPressedThisFrame)
        {
            StartTest();
        }
    }

    public void StartTest()
    {
        if (!running)
        {
            StartCoroutine(RunTests());
        }
    }

    IEnumerator RunTests()
    {
        IInteractable target = targetBehaviour as IInteractable;
        if (target == null || companion == null || startArea == null)
        {
            Debug.LogError("CompanionTestRunner: Companion, Target Behaviour (PressurePlate) ve Start Area alanları doldurulmalı.", this);
            yield break;
        }

        running = true;
        results.Clear();
        summary = string.Empty;

        System.Random random = new System.Random(randomSeed);
        Debug.Log("[Test] Companion testi başladı: " + trials + " deneme, tohum=" + randomSeed, this);

        for (int i = 1; i <= trials; i++)
        {
            currentTrial = i;
            ResetTargets();

            // Önceki emir bitmediyse bekle (en fazla 5 sn)
            float waitUntil = Time.realtimeSinceStartup + 5f;
            while (companion.IsBusy && Time.realtimeSinceStartup < waitUntil)
            {
                yield return null;
            }

            TrialResult result = new TrialResult();
            result.Index = i;
            results.Add(result);

            Vector2 start;
            if (!TryPickStart(random, out start))
            {
                result.Note = "NavMesh üzerinde başlangıç noktası bulunamadı";
                continue;
            }

            result.Start = start;

            if (!companion.Agent.Warp(start))
            {
                result.Note = "Companion başlangıç noktasına yerleştirilemedi";
                continue;
            }

            yield return null; // yeni konum otursun

            trialFinished = false;
            trialSuccess = false;
            companion.CommandFinished += OnCommandFinished;

            float startedAt = Time.realtimeSinceStartup;
            bool accepted = companion.GiveCommand(target);

            if (accepted)
            {
                while (!trialFinished && Time.realtimeSinceStartup - startedAt < maxTrialSeconds)
                {
                    yield return null;
                }
            }

            companion.CommandFinished -= OnCommandFinished;
            result.Seconds = Time.realtimeSinceStartup - startedAt;

            if (!accepted)
            {
                result.Note = "Emir kabul edilmedi";
            }
            else if (!trialFinished)
            {
                result.Note = "Süre doldu";
                companion.FinishCommand(false); // companion'ı takibe döndür
            }
            else
            {
                result.Success = trialSuccess;
                if (!trialSuccess) result.Note = "Companion emri tamamlayamadı";
            }

            yield return new WaitForSeconds(0.3f);
        }

        ResetTargets(); // sahneyi başlangıç haline bırak
        FinishAndReport();
    }

    void OnCommandFinished(bool success)
    {
        trialFinished = true;
        trialSuccess = success;
    }

    void ResetTargets()
    {
        if (resetTargets == null) return;

        foreach (GameObject go in resetTargets)
        {
            if (go == null) continue;

            foreach (IResettable resettable in go.GetComponents<IResettable>())
            {
                resettable.ResetState();
            }
        }
    }

    bool TryPickStart(System.Random random, out Vector2 position)
    {
        Bounds bounds = startArea.bounds;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble());
            float y = Mathf.Lerp(bounds.min.y, bounds.max.y, (float)random.NextDouble());

            NavMeshHit hit;
            if (NavMesh.SamplePosition(new Vector3(x, y, 0f), out hit, navMeshSnapDistance, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }
        }

        position = Vector2.zero;
        return false;
    }

    void FinishAndReport()
    {
        List<float> times = new List<float>();
        foreach (TrialResult r in results)
        {
            if (r.Success) times.Add(r.Seconds);
        }

        int total = results.Count;
        int successCount = times.Count;
        float rate = total > 0 ? 100f * successCount / total : 0f;

        float mean = 0f, min = 0f, max = 0f, std = 0f;
        if (successCount > 0)
        {
            min = float.MaxValue;
            float sum = 0f;
            foreach (float t in times)
            {
                sum += t;
                if (t < min) min = t;
                if (t > max) max = t;
            }
            mean = sum / successCount;

            float variance = 0f;
            foreach (float t in times) variance += (t - mean) * (t - mean);
            std = Mathf.Sqrt(variance / successCount);
        }

        summary = "Companion testi: " + successCount + "/" + total + " başarılı (%" + rate.ToString("F0") + ")"
            + " | ort. süre " + mean.ToString("F2") + " sn (min " + min.ToString("F2") + ", maks " + max.ToString("F2") + ", std " + std.ToString("F2") + ")";

        Debug.Log("[Test] " + summary, this);

        // CSV
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("deneme,baslangic_x,baslangic_y,basarili,sure_sn,not");
        foreach (TrialResult r in results)
        {
            csv.AppendLine(r.Index + "," + F(r.Start.x) + "," + F(r.Start.y) + "," + (r.Success ? 1 : 0) + "," + F(r.Seconds) + "," + TestReportWriter.CsvCell(r.Note));
        }

        // Markdown (README'ye yapıştırılabilir)
        StringBuilder md = new StringBuilder();
        md.AppendLine("### Companion (NavMesh) Test Sonuçları");
        md.AppendLine();
        md.AppendLine("- Deneme sayısı: " + total + " (rastgele başlangıç konumu, tohum: " + randomSeed + ")");
        md.AppendLine("- Başarı oranı: **" + successCount + "/" + total + " (%" + rate.ToString("F0") + ")**");
        md.AppendLine("- Ortalama tamamlama süresi: **" + F(mean) + " sn** (min " + F(min) + ", maks " + F(max) + ", std " + F(std) + ")");
        md.AppendLine();
        md.AppendLine("| Deneme | Başlangıç (x, y) | Sonuç | Süre (sn) | Not |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (TrialResult r in results)
        {
            md.AppendLine("| " + r.Index + " | (" + F(r.Start.x) + ", " + F(r.Start.y) + ") | " + (r.Success ? "Başarılı" : "Başarısız")
                + " | " + F(r.Seconds) + " | " + TestReportWriter.MdCell(r.Note) + " |");
        }

        TestReportWriter.Save("companion_test", csv.ToString(), md.ToString());

        running = false;
    }

    static string F(float value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    // Ekranın sağ üstünde ilerleme / sonuç göster (rapor için ekran görüntüsü alınabilir)
    void OnGUI()
    {
        if (!running && string.IsNullOrEmpty(summary)) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.wordWrap = true;
        style.alignment = TextAnchor.UpperLeft;

        int successSoFar = 0;
        foreach (TrialResult r in results)
        {
            if (r.Success) successSoFar++;
        }

        string text = running
            ? "Companion testi çalışıyor: deneme " + currentTrial + "/" + trials + " | başarılı: " + successSoFar
            : summary;

        float width = 560f;
        GUI.Box(new Rect(Screen.width - width - 10f, 10f, width, running ? 40f : 70f), text, style);
    }
}
