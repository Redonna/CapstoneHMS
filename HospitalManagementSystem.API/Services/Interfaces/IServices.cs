using HospitalManagementSystem.API.DTOs;

namespace HospitalManagementSystem.API.Services.Interfaces
{
    public interface IPatientService
    {
        Task<IEnumerable<PatientReadDto>> GetAllAsync();
        Task<PatientReadDto?> GetByIdAsync(int id);
        Task<PatientReadDto> CreateAsync(PatientCreateDto dto);
        Task<PatientReadDto?> UpdateAsync(int id, PatientUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IDoctorService
    {
        Task<IEnumerable<DoctorReadDto>> GetAllAsync();
        Task<DoctorReadDto?> GetByIdAsync(int id);
        Task<DoctorReadDto> CreateAsync(DoctorCreateDto dto);
        Task<DoctorReadDto?> UpdateAsync(int id, DoctorUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentReadDto>> GetAllAsync();
        Task<AppointmentReadDto?> GetByIdAsync(int id);
        Task<IEnumerable<AppointmentReadDto>> GetByPatientIdAsync(int patientId);
        Task<IEnumerable<AppointmentReadDto>> GetByDoctorIdAsync(int doctorId);
        Task<(AppointmentReadDto? result, string? error)> CreateAsync(AppointmentCreateDto dto, string creatorRole);
        Task<(AppointmentReadDto? result, string? error)> UpdateAsync(int id, AppointmentUpdateDto dto);
        Task<bool> CancelAsync(int id);
        Task<(AppointmentReadDto? result, string? error)> AcceptAsync(int id, string callerUsername);
        Task<(AppointmentReadDto? result, string? error)> DenyAsync(int id, string callerUsername);
    }

    public interface IAuthService
    {
        Task<(AuthResponseDto? result, string? error)> LoginAsync(LoginDto dto);
        Task<(AuthResponseDto? result, string? error)> RegisterAsync(RegisterDto dto);
    }

    public interface IAssignmentService
    {
        Task<IEnumerable<AssignmentReadDto>> GetAllAsync();
        Task<IEnumerable<AssignmentReadDto>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<AssignmentReadDto>> GetByPatientIdAsync(int patientId);
        Task<(AssignmentReadDto? result, string? error)> CreateAsync(AssignmentCreateDto dto);
        Task<(AssignmentReadDto? result, string? error)> CreateSelfAssignedAsync(int patientId, string callerUsername);
        Task<(AssignmentReadDto? result, string? error)> AcceptAsync(int id, string callerUsername);
        Task<(AssignmentReadDto? result, string? error)> DenyAsync(int id, string callerUsername);
        Task<(bool success, string? error)> RemoveAsync(int id, string callerRole, string callerUsername);
    }

    public interface IPatientHistoryService
    {
        Task<IEnumerable<PatientHistoryReadDto>> GetByPatientIdAsync(int patientId);
        Task<(PatientHistoryReadDto? result, string? error)> CreateAsync(PatientHistoryCreateDto dto, string creatorRole, string callerUsername);
        Task<(PatientHistoryReadDto? result, string? error)> UploadAttachmentAsync(int entryId, IFormFile file);
        Task<(string? absolutePath, string? fileName, string? error)> GetAttachmentAsync(int entryId);
    }
}
