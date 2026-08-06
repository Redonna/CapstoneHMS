using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace HospitalManagementSystem.API.Services
{
    public class DiseaseInfoEntry
    {
        public string Description { get; set; } = string.Empty;
        public List<string> Precautions { get; set; } = new();
        public List<string> Medications { get; set; } = new();
    }

    /// <summary>
    /// Loads the ONNX symptom-to-disease classifier and its metadata once and keeps them
    /// in memory for the lifetime of the app. Registered as a singleton since InferenceSession
    /// is expensive to construct and safe to call Run() on concurrently.
    /// </summary>
    public class SymptomModelStore
    {
        private readonly InferenceSession _session;

        public IReadOnlyList<string> SymptomFeatures { get; }
        public IReadOnlyList<string> DiseaseLabels { get; }
        public IReadOnlyDictionary<string, DiseaseInfoEntry> DiseaseInfo { get; }

        public SymptomModelStore(IWebHostEnvironment environment)
        {
            var modelsDir = Path.Combine(environment.ContentRootPath, "MLModels");

            _session = new InferenceSession(Path.Combine(modelsDir, "symptom_model.onnx"));

            SymptomFeatures = JsonSerializer.Deserialize<List<string>>(
                File.ReadAllText(Path.Combine(modelsDir, "symptom_features.json")))!;

            DiseaseLabels = JsonSerializer.Deserialize<List<string>>(
                File.ReadAllText(Path.Combine(modelsDir, "disease_labels.json")))!;

            DiseaseInfo = JsonSerializer.Deserialize<Dictionary<string, DiseaseInfoEntry>>(
                File.ReadAllText(Path.Combine(modelsDir, "disease_info.json")),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public (int predictedIndex, float[] probabilities) Predict(float[] features)
        {
            var inputTensor = new DenseTensor<float>(features, new[] { 1, features.Length });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("symptoms", inputTensor) };

            using var results = _session.Run(inputs);
            var resultsList = results.ToList();

            var label = (int)resultsList.First(r => r.Name == "label").AsTensor<long>().First();
            var probabilities = resultsList.First(r => r.Name == "probabilities").AsEnumerable<float>().ToArray();

            return (label, probabilities);
        }
    }
}
