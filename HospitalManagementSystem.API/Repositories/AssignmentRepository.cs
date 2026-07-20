using HospitalManagementSystem.API.Data;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.API.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly HospitalDbContext _context;

        public AssignmentRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DoctorPatientAssignment>> GetAllAsync()
        {
            return await _context.DoctorPatientAssignments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
        }

        public async Task<DoctorPatientAssignment?> GetByIdAsync(int id)
        {
            return await _context.DoctorPatientAssignments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<DoctorPatientAssignment>> GetByDoctorIdAsync(int doctorId)
        {
            return await _context.DoctorPatientAssignments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DoctorPatientAssignment>> GetByPatientIdAsync(int patientId)
        {
            return await _context.DoctorPatientAssignments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
        }

        public async Task<DoctorPatientAssignment> CreateAsync(DoctorPatientAssignment assignment)
        {
            _context.DoctorPatientAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            return assignment;
        }

        public async Task<DoctorPatientAssignment?> UpdateAsync(int id, DoctorPatientAssignment updated)
        {
            var existing = await _context.DoctorPatientAssignments.FindAsync(id);
            if (existing == null) return null;

            existing.Status = updated.Status;
            existing.DecidedAt = updated.DecidedAt;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var assignment = await _context.DoctorPatientAssignments.FindAsync(id);
            if (assignment == null) return false;

            _context.DoctorPatientAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
