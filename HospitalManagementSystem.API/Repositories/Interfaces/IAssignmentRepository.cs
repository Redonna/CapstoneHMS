using HospitalManagementSystem.API.Models;

namespace HospitalManagementSystem.API.Repositories.Interfaces
{
    public interface IAssignmentRepository
    {
        Task<IEnumerable<DoctorPatientAssignment>> GetAllAsync();
        Task<DoctorPatientAssignment?> GetByIdAsync(int id);
        Task<IEnumerable<DoctorPatientAssignment>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<DoctorPatientAssignment>> GetByPatientIdAsync(int patientId);
        Task<DoctorPatientAssignment> CreateAsync(DoctorPatientAssignment assignment);
        Task<DoctorPatientAssignment?> UpdateAsync(int id, DoctorPatientAssignment assignment);
        Task<bool> DeleteAsync(int id);
    }
}
