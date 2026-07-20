using HospitalManagementSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.API.Data
{
    public class HospitalDbContext : DbContext
    {
        public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options) { }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<User> Users => Set<User>();
        public DbSet<DoctorPatientAssignment> DoctorPatientAssignments => Set<DoctorPatientAssignment>();
        public DbSet<PatientHistoryEntry> PatientHistoryEntries => Set<PatientHistoryEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Appointment -> Patient (restrict delete to avoid cascade conflicts)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment -> Doctor
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorPatientAssignment -> Patient / Doctor
            modelBuilder.Entity<DoctorPatientAssignment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DoctorPatientAssignment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // PatientHistoryEntry -> Patient / RecordedByDoctor
            modelBuilder.Entity<PatientHistoryEntry>()
                .HasOne(h => h.Patient)
                .WithMany()
                .HasForeignKey(h => h.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PatientHistoryEntry>()
                .HasOne(h => h.RecordedByDoctor)
                .WithMany()
                .HasForeignKey(h => h.RecordedByDoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed admin user (password: Admin@123)
            // Hash pre-computed so migrations are deterministic
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "$2a$11$V1JGVUqqi3SZOmGHbCPfteLFA0GY5cm6yfpnqq1.NRNrsM7kF8kN6",
                Role = "Admin",
                Email = "admin@hospital.com",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            });
        }
    }
}
