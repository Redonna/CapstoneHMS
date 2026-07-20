using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.API.Models
{
    public class PatientHistoryEntry
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        public int? RecordedByDoctorId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Details { get; set; } = string.Empty;

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(260)]
        public string? AttachmentFileName { get; set; }

        [MaxLength(300)]
        public string? AttachmentStoredPath { get; set; }

        // Navigation
        public Patient Patient { get; set; } = null!;
        public Doctor? RecordedByDoctor { get; set; }
    }
}
