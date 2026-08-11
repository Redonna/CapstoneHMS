using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Role { get; set; } = string.Empty; // Admin, Doctor, Patient

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public int? ProfileId { get; set; } // Links to Patient.Id or Doctor.Id

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiresAt { get; set; }
    }
}
