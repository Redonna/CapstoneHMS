using HospitalManagementSystem.API.Models;

namespace HospitalManagementSystem.API.Repositories.Interfaces
{
    public interface IPatientHistoryRepository
    {
        Task<IEnumerable<PatientHistoryEntry>> GetByPatientIdAsync(int patientId);
        Task<PatientHistoryEntry?> GetByIdAsync(int id);
        Task<PatientHistoryEntry> CreateAsync(PatientHistoryEntry entry);
        Task<PatientHistoryEntry?> SetAttachmentAsync(int id, string fileName, string storedPath);
    }
}
