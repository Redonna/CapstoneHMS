using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.API.Models
{
    public enum AssignmentStatus
    {
        Pending,
        Accepted,
        Denied,
        Cancelled
    }

    public class DoctorPatientAssignment
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DecidedAt { get; set; }

        // Navigation
        public Patient Patient { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
    }
}
