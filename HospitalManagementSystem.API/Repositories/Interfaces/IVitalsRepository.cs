using HospitalManagementSystem.API.Models;

namespace HospitalManagementSystem.API.Repositories.Interfaces
{
    public interface IVitalsRepository
    {
        Task<IEnumerable<VitalsRecord>> GetByPatientIdAsync(int patientId);
        Task<VitalsRecord> CreateAsync(VitalsRecord record);
    }
}
