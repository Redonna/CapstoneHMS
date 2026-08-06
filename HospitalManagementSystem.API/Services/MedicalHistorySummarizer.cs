namespace HospitalManagementSystem.API.Services
{
    /// <summary>
    /// Turns the paragraphs of a medical-history document into a short bullet-point
    /// timeline. Looks for the "Visit / Chief complaint / Diagnosis" structure used by
    /// the generated demo histories; falls back to raw paragraphs for any other document.
    /// </summary>
    public static class MedicalHistorySummarizer
    {
        public static List<string> SummarizeToBullets(IEnumerable<string> paragraphs)
        {
            var bullets = new List<string>();
            string? currentVisit = null;
            string? currentSymptoms = null;
            string? currentDiagnosis = null;

            void Flush()
            {
                if (currentVisit == null) return;
                var parts = new List<string> { currentVisit };
                if (currentSymptoms != null) parts.Add($"reported {currentSymptoms}");
                if (currentDiagnosis != null) parts.Add($"diagnosed with {currentDiagnosis}");
                bullets.Add(string.Join(" — ", parts));
                currentSymptoms = null;
                currentDiagnosis = null;
            }

            foreach (var line in paragraphs)
            {
                if (line.StartsWith("Visit", StringComparison.OrdinalIgnoreCase))
                {
                    Flush();
                    currentVisit = line;
                }
                else if (line.StartsWith("Chief complaint:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSymptoms = line["Chief complaint:".Length..].Trim().TrimEnd('.');
                }
                else if (line.StartsWith("Diagnosis:", StringComparison.OrdinalIgnoreCase))
                {
                    var diagnosis = line["Diagnosis:".Length..].Trim();
                    var dotIndex = diagnosis.IndexOf('.');
                    currentDiagnosis = dotIndex > 0 ? diagnosis[..dotIndex] : diagnosis;
                }
            }
            Flush();

            if (bullets.Count == 0)
            {
                bullets = paragraphs.Where(p => p.Length > 20).Take(10).ToList();
            }

            return bullets;
        }
    }
}
