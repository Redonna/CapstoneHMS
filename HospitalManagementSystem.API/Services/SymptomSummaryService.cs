using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using HospitalManagementSystem.API.Services.Interfaces;

namespace HospitalManagementSystem.API.Services
{
    public class SymptomSummaryService : ISymptomSummaryService
    {
        private readonly SymptomModelStore _modelStore;
        private readonly IPatientRepository _patientRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPatientHistoryService _patientHistoryService;

        public SymptomSummaryService(
            SymptomModelStore modelStore,
            IPatientRepository patientRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IPatientHistoryService patientHistoryService)
        {
            _modelStore = modelStore;
            _patientRepository = patientRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _patientHistoryService = patientHistoryService;
        }

        public async Task<(SymptomSummaryResponseDto? result, string? error)> SummarizeAsync(
            SymptomSummaryRequestDto dto, string callerRole, string callerUsername)
        {
            if (!await _patientRepository.ExistsAsync(dto.PatientId))
                return (null, $"Patient with ID {dto.PatientId} not found.");

            if (callerRole == "Doctor")
            {
                var user = await _userRepository.GetByUsernameAsync(callerUsername);
                var assignments = await _assignmentRepository.GetByPatientIdAsync(dto.PatientId);
                var accepted = assignments.Any(a => a.DoctorId == user?.ProfileId && a.Status == AssignmentStatus.Accepted);
                if (!accepted)
                    return (null, "You can only summarize symptoms for patients you have accepted.");
            }

            var (currentSymptoms, currentFeatures) = MatchSymptoms(dto.SymptomText);

            var pastText = await _patientHistoryService.GetHistoryDocumentTextAsync(dto.PatientId);
            var hasPastHistory = !string.IsNullOrWhiteSpace(pastText);
            var (pastSymptoms, pastFeatures) = hasPastHistory
                ? MatchSymptoms(pastText)
                : (new List<string>(), new float[_modelStore.SymptomFeatures.Count]);

            if (currentSymptoms.Count == 0 && pastSymptoms.Count == 0)
                return (null, "No recognizable symptoms found in the description or the patient's medical history. Try using plainer terms (e.g. 'fever', 'headache', 'joint pain').");

            // Prioritize what the doctor is describing right now. A patient's accumulated
            // past history can easily outnumber a couple of newly-typed symptoms, so a flat
            // union would let old, unrelated conditions dominate every prediction regardless
            // of what's typed. Past history is only blended into the actual classification
            // when the current description alone is too sparse to work with; otherwise it's
            // still shown to the doctor as reference, just not fed into the model.
            const int sparseCurrentSymptomThreshold = 2;
            bool blendPastHistory = pastSymptoms.Count > 0 && currentSymptoms.Count < sparseCurrentSymptomThreshold;

            float[] predictionFeatures;
            if (currentSymptoms.Count == 0)
                predictionFeatures = pastFeatures;
            else if (blendPastHistory)
            {
                predictionFeatures = new float[_modelStore.SymptomFeatures.Count];
                for (int i = 0; i < predictionFeatures.Length; i++)
                    predictionFeatures[i] = (currentFeatures[i] == 1f || pastFeatures[i] == 1f) ? 1f : 0f;
            }
            else
                predictionFeatures = currentFeatures;

            var (predictedIndex, probabilities) = _modelStore.Predict(predictionFeatures);
            var disease = _modelStore.DiseaseLabels[predictedIndex];
            var confidence = probabilities[predictedIndex];

            _modelStore.DiseaseInfo.TryGetValue(disease, out var info);

            return (new SymptomSummaryResponseDto
            {
                CurrentSymptoms = currentSymptoms,
                PastSymptoms = pastSymptoms,
                HasPastHistory = pastSymptoms.Count > 0,
                BlendedPastHistory = blendPastHistory || currentSymptoms.Count == 0,
                PredictedDisease = disease,
                Confidence = Math.Round(confidence * 100, 1),
                Description = info?.Description ?? "",
                Precautions = info?.Precautions ?? new List<string>(),
                Medications = info?.Medications ?? new List<string>()
            }, null);
        }

        private (List<string> matched, float[] features) MatchSymptoms(string text)
        {
            var normalizedText = " " + text.ToLowerInvariant() + " ";
            var matched = new List<string>();
            var features = new float[_modelStore.SymptomFeatures.Count];

            for (int i = 0; i < _modelStore.SymptomFeatures.Count; i++)
            {
                var readable = _modelStore.SymptomFeatures[i].Replace('_', ' ').Trim();
                readable = System.Text.RegularExpressions.Regex.Replace(readable, @"\s+", " ");
                if (normalizedText.Contains(" " + readable + " ") || normalizedText.Contains(readable))
                {
                    features[i] = 1f;
                    matched.Add(readable);
                }
            }

            return (matched, features);
        }
    }
}
