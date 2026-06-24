using Newtonsoft.Json.Linq;
using System.Text;

namespace PowerShellAnalyzer
{
    public static class AIEngine
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> AnalyzeScriptAsync(string scriptContent, string apiKey, string selectedModel)
        {
            string systemPrompt = "Du bist ein Experte für PowerShell-Skripte. Analysiere das folgende Skript und erkläre, was es tut. " +
                                  "Antworte AUSSCHLIESSLICH auf Deutsch und halte dich STRIKT an dieses Format:\n\n" +
                                  "[Ein kurzer, prägnanter Satz, der den Hauptzweck des Skripts zusammenfasst.]\n" +
                                  "- [Wichtigste Aktion 1]\n" +
                                  "- [Wichtigste Aktion 2]\n" +
                                  "- [Wichtigste Aktion 3]\n\n" +
                                  "Empfohlener Dateiname: [Ein kurzer, treffender Dateiname für das Skript, endend mit .ps1]\n\n" +
                                  "Ignoriere einfache Variablenzuweisungen. Konzentriere dich auf die Kernfunktionen (z.B. API-Aufrufe, Dateioperationen, AD-Änderungen).";

            // Zwrotnica: Sprawdzamy co użytkownik wybrał w ComboBoxie
            if (selectedModel.StartsWith("Gemini"))
            {
                // Usuwamy przedrostek "Gemini: ", żeby uzyskać czystą nazwę modelu (np. gemini-1.5-flash)
                string actualModel = selectedModel.Replace("Gemini: ", "").ToLower().Replace(" ", "-").Trim();
                return await CallGeminiAPI(scriptContent, systemPrompt, apiKey, actualModel);
            }
            else // Domyślnie OpenAI
            {
                string actualModel = selectedModel.Replace("OpenAI: ", "").Trim();
                return await CallOpenAIAPI(scriptContent, systemPrompt, apiKey, actualModel);
            }
        }

        public static async Task<string> AnalyzeDuplicatesAsync(List<ScriptSimilarityInfo> duplicates, string apiKey, string selectedModel)
        {
            string systemPrompt = "Du bist ein Experte für PowerShell-Skripte und Code-Qualität. Ich gebe dir eine Liste von Skript-Duplikaten. " +
                                  "Deine Aufgabe ist es, diese Skripte zu analysieren und zu entscheiden, welches Skript behalten werden soll und welche als überflüssig gelöscht werden können.\n" +
                                  "Berücksichtige Aspekte wie Modernität, Lesbarkeit, Fehlerbehandlung und Vollständigkeit der Funktionalität, um das beste Skript auszuwählen.\n" +
                                  "Antworte AUSSCHLIESSLICH im JSON-Format wie folgt:\n" +
                                  "[\n" +
                                  "  { \"Status\": \"Behalten\", \"Skript\": \"Name_des_Skripts.ps1\", \"Grund\": \"Komplette Begründung mit Details zur Modernität etc.\" },\n" +
                                  "  { \"Status\": \"Löschen\", \"Skript\": \"Alter_Name.ps1\", \"Grund\": \"Begründung zur Veraltung oder warum es schlechter ist.\" }\n" +
                                  "]";

            StringBuilder userPromptBuilder = new StringBuilder();
            userPromptBuilder.AppendLine("Hier sind die zu vergleichenden Skript-Duplikate:");
            for (int i = 0; i < duplicates.Count; i++)
            {
                userPromptBuilder.AppendLine($"\n--- SKRIPT {i + 1}: {duplicates[i].FileName} (Pfad: {duplicates[i].Path}) ---");
                userPromptBuilder.AppendLine(duplicates[i].Content);
            }

            string userPrompt = userPromptBuilder.ToString();

            if (selectedModel.StartsWith("Gemini"))
            {
                string actualModel = selectedModel.Replace("Gemini: ", "").ToLower().Replace(" ", "-").Trim();
                return await CallGeminiAPI(userPrompt, systemPrompt, apiKey, actualModel);
            }
            else
            {
                string actualModel = selectedModel.Replace("OpenAI: ", "").Trim();
                return await CallOpenAIAPI(userPrompt, systemPrompt, apiKey, actualModel);
            }
        }

        public static async Task<string> AnalyzeRenameAsync(string scriptDescription, string apiKey, string selectedModel)
        {
            string systemPrompt = "Du sollst einen neuen, perfekten und professionellen Dateinamen für ein PowerShell-Skript basierend auf seiner Beschreibung generieren. " +
                                  "GIB AUSSCHLIESSLICH DEN DATEINAMEN OHNE DIE .ps1 ENDUNG ZURÜCK! Keine Leerzeichen, keine Erklärungen, keine Anführungszeichen. " +
                                  "Benutze am besten das typische PowerShell Verb-Noun Format (z.B. Get-ActiveDirectoryUsers) oder PascalCase (z.B. SetupServerEnvironment).";

            if (selectedModel.StartsWith("Gemini"))
            {
                string actualModel = selectedModel.Replace("Gemini: ", "").ToLower().Replace(" ", "-").Trim();
                return await CallGeminiAPI(scriptDescription, systemPrompt, apiKey, actualModel);
            }
            else
            {
                string actualModel = selectedModel.Replace("OpenAI: ", "").Trim();
                return await CallOpenAIAPI(scriptDescription, systemPrompt, apiKey, actualModel);
            }
        }

        private static async Task<string> CallOpenAIAPI(string userPrompt, string systemPrompt, string apiKey, string model)
        {
            string cleanApiKey = apiKey?.Replace("\r", "")?.Replace("\n", "")?.Trim() ?? "";
            if (cleanApiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                cleanApiKey = cleanApiKey.Substring(7).Trim();
            }
            string endpoint = "https://api.openai.com/v1/chat/completions";

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2
            };

            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {cleanApiKey}");

            var response = await client.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API Fehler: {response.StatusCode}\n{error}");
            }

            string responseString = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseString);
            return json["choices"][0]["message"]["content"].ToString().Trim();
        }

        // --- OBSŁUGA DIALEKTU GOOGLE GEMINI ---
        private static async Task<string> CallGeminiAPI(string userPrompt, string systemPrompt, string apiKey, string model)
        {
            string cleanApiKey = apiKey?.Replace("\r", "")?.Replace("\n", "")?.Trim() ?? "";
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={cleanApiKey}";

            // Da manche ältere Gemini-Modelle kein system_instruction unterstützen,
            // packen wir den systemPrompt sicherheitshalber einfach vor den userPrompt.
            string combinedPrompt = $"SYSTEM: {systemPrompt}\n\nUSER:\n{userPrompt}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = combinedPrompt } } }
                },
                generationConfig = new { temperature = 0.2 }
            };

            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();

            var response = await client.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Fehler: {response.StatusCode}\nDetails: {error}");
            }

            string responseString = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseString);

            return json["candidates"][0]["content"]["parts"][0]["text"].ToString().Trim();
        }
    }
}