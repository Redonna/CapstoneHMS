using HospitalManagementSystem.API.Data;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.API.Repositories
{
    public class PatientHistoryRepository : IPatientHistoryRepository
    {
        private readonly HospitalDbContext _context;

        public PatientHistoryRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PatientHistoryEntry>> GetByPatientIdAsync(int patientId)
        {
            return await _context.PatientHistoryEntries
                .AsNoTracking()
                .Include(h => h.RecordedByDoctor)
                .Where(h => h.PatientId == patientId)
                .OrderByDescending(h => h.RecordDate)
                .ToListAsync();
        }

        public async Task<PatientHistoryEntry?> GetByIdAsync(int id)
        {
            return await _context.PatientHistoryEntries
                .AsNoTracking()
                .Include(h => h.RecordedByDoctor)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<PatientHistoryEntry> CreateAsync(PatientHistoryEntry entry)
        {
            _context.PatientHistoryEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<PatientHistoryEntry?> SetAttachmentAsync(int id, string fileName, string storedPath)
        {
            var existing = await _context.PatientHistoryEntries.FindAsync(id);
            if (existing == null) return null;

            existing.AttachmentFileName = fileName;
            existing.AttachmentStoredPath = storedPath;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
