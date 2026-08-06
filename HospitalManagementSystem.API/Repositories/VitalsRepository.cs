using HospitalManagementSystem.API.Data;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.API.Repositories
{
    public class VitalsRepository : IVitalsRepository
    {
        private readonly HospitalDbContext _context;

        public VitalsRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VitalsRecord>> GetByPatientIdAsync(int patientId)
        {
            return await _context.VitalsRecords
                .AsNoTracking()
                .Include(v => v.RecordedByDoctor)
                .Where(v => v.PatientId == patientId)
                .OrderBy(v => v.RecordDate)
                .ToListAsync();
        }

        public async Task<VitalsRecord> CreateAsync(VitalsRecord record)
        {
            _context.VitalsRecords.Add(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
