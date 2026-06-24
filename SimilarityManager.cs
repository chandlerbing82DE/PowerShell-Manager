namespace PowerShellAnalyzer
{
    public class ScriptSimilarityInfo
    {
        public string Path { get; set; }
        public string Id { get; set; }
        public string FileName { get; set; }
        public double[] VectorData { get; set; }
        public string SimilarityGroupId { get; set; }
        public string BestMatchScriptId { get; set; }
        public double BestSimilarityScore { get; set; }
        public int SimilarScriptsCount { get; set; }
        public string Content { get; set; }
    }

    public class SimilarityManager
    {
        public List<ScriptSimilarityInfo> Scripts { get; private set; } = new List<ScriptSimilarityInfo>();

        public async Task ComputeSimilaritiesAsync(List<ScriptSimilarityInfo> inputScripts)
        {
            await Task.Run(() =>
            {
                var vocabulary = new Dictionary<string, int>();
                var tokenizedScripts = new List<List<string>>();

                foreach (var script in inputScripts)
                {
                    try
                    {
                        script.Content = File.ReadAllText(script.Path);
                        var tokens = Tokenize(script.Content);
                        tokenizedScripts.Add(tokens);
                        foreach (var token in tokens)
                        {
                            if (!vocabulary.ContainsKey(token))
                                vocabulary[token] = vocabulary.Count;
                        }
                    }
                    catch
                    {
                        tokenizedScripts.Add(new List<string>());
                        script.Content = "";
                    }
                }

                int vocabSize = vocabulary.Count;
                foreach (var (script, tokens) in inputScripts.Zip(tokenizedScripts, (s, t) => (s, t)))
                {
                    script.VectorData = new double[vocabSize];
                    foreach (var token in tokens)
                    {
                        script.VectorData[vocabulary[token]] += 1;
                    }
                    // Normalize
                    double norm = Math.Sqrt(script.VectorData.Sum(x => x * x));
                    if (norm > 0)
                    {
                        for (int i = 0; i < vocabSize; i++)
                            script.VectorData[i] /= norm;
                    }
                }

                double threshold = 0.85;
                int groupIdCounter = 1;
                var unassigned = new HashSet<ScriptSimilarityInfo>(inputScripts);

                while (unassigned.Count > 0)
                {
                    var current = unassigned.First();
                    unassigned.Remove(current);

                    current.SimilarityGroupId = $"G{groupIdCounter:D3}";
                    var group = new List<ScriptSimilarityInfo> { current };

                    foreach (var other in unassigned.ToList())
                    {
                        double score = CosineSimilarity(current.VectorData, other.VectorData);

                        if (score > current.BestSimilarityScore)
                        {
                            current.BestSimilarityScore = score;
                            current.BestMatchScriptId = other.Id;
                        }
                        if (score > other.BestSimilarityScore)
                        {
                            other.BestSimilarityScore = score;
                            other.BestMatchScriptId = current.Id;
                        }

                        if (score >= threshold)
                        {
                            other.SimilarityGroupId = current.SimilarityGroupId;
                            group.Add(other);
                            unassigned.Remove(other);
                        }
                    }

                    foreach (var s in group)
                    {
                        s.SimilarScriptsCount = group.Count - 1;
                    }

                    if (group.Count > 1)
                        groupIdCounter++;
                    else
                        current.SimilarityGroupId = "";
                }

                Scripts = inputScripts;
            });
        }

        private List<string> Tokenize(string text)
        {
            return new string(text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray())
                .ToLower()
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private double CosineSimilarity(double[] v1, double[] v2)
        {
            if (v1 == null || v2 == null || v1.Length != v2.Length) return 0;
            double dot = 0;
            for (int i = 0; i < v1.Length; i++) dot += v1[i] * v2[i];
            return dot;
        }
    }
}