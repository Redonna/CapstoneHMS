namespace HospitalManagementSystem.API.Models
{
    public class VitalsRecord
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public int? RecordedByDoctorId { get; set; }

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        public int? BloodPressureSystolic { get; set; }

        public int? BloodPressureDiastolic { get; set; }

        public int? HeartRate { get; set; }

        public decimal? Temperature { get; set; }

        public decimal? Weight { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Patient Patient { get; set; } = null!;
        public Doctor? RecordedByDoctor { get; set; }
    }
}
