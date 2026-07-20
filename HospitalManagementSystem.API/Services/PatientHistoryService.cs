using AutoMapper;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using HospitalManagementSystem.API.Services.Interfaces;

namespace HospitalManagementSystem.API.Services
{
    public class PatientHistoryService : IPatientHistoryService
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private readonly IPatientHistoryRepository _historyRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IMapper _mapper;

        public PatientHistoryService(
            IPatientHistoryRepository historyRepository,
            IPatientRepository patientRepository,
            IUserRepository userRepository,
            IWebHostEnvironment environment,
            IMapper mapper)
        {
            _historyRepository = historyRepository;
            _patientRepository = patientRepository;
            _userRepository = userRepository;
            _environment = environment;
            _mapper = mapper;
        }

        private string UploadsDirectory => Path.Combine(_environment.ContentRootPath, "Uploads", "PatientHistory");

        public async Task<IEnumerable<PatientHistoryReadDto>> GetByPatientIdAsync(int patientId)
        {
            var entries = await _historyRepository.GetByPatientIdAsync(patientId);
            return _mapper.Map<IEnumerable<PatientHistoryReadDto>>(entries);
        }

        public async Task<(PatientHistoryReadDto? result, string? error)> CreateAsync(PatientHistoryCreateDto dto, string creatorRole, string callerUsername)
        {
            if (!await _patientRepository.ExistsAsync(dto.PatientId))
                return (null, $"Patient with ID {dto.PatientId} not found.");

            var entry = _mapper.Map<PatientHistoryEntry>(dto);
            entry.RecordDate = dto.RecordDate ?? DateTime.UtcNow;

            if (creatorRole == "Doctor")
            {
                var user = await _userRepository.GetByUsernameAsync(callerUsername);
                entry.RecordedByDoctorId = user?.ProfileId;
            }

            var created = await _historyRepository.CreateAsync(entry);
            var all = await _historyRepository.GetByPatientIdAsync(dto.PatientId);
            var full = all.FirstOrDefault(e => e.Id == created.Id);
            return (_mapper.Map<PatientHistoryReadDto>(full ?? created), null);
        }

        public async Task<(PatientHistoryReadDto? result, string? error)> UploadAttachmentAsync(int entryId, IFormFile file)
        {
            var entry = await _historyRepository.GetByIdAsync(entryId);
            if (entry == null) return (null, $"History entry with ID {entryId} not found.");

            if (file == null || file.Length == 0)
                return (null, "No file was provided.");

            if (file.Length > MaxFileSizeBytes)
                return (null, "File exceeds the 10MB size limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return (null, $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}.");

            Directory.CreateDirectory(UploadsDirectory);
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(UploadsDirectory, storedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await _historyRepository.SetAttachmentAsync(entryId, file.FileName, storedFileName);

            var updated = await _historyRepository.GetByIdAsync(entryId);
            return (_mapper.Map<PatientHistoryReadDto>(updated), null);
        }

        public async Task<(string? absolutePath, string? fileName, string? error)> GetAttachmentAsync(int entryId)
        {
            var entry = await _historyRepository.GetByIdAsync(entryId);
            if (entry == null) return (null, null, "History entry not found.");
            if (string.IsNullOrEmpty(entry.AttachmentStoredPath)) return (null, null, "This entry has no attachment.");

            var fullPath = Path.Combine(UploadsDirectory, entry.AttachmentStoredPath);
            if (!File.Exists(fullPath)) return (null, null, "Attachment file is missing on disk.");

            return (fullPath, entry.AttachmentFileName, null);
        }
    }
}
