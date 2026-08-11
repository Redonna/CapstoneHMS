using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using HospitalManagementSystem.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace HospitalManagementSystem.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IPatientRepository patientRepository,
            IDoctorRepository doctorRepository, IEmailService emailService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<(AuthResponseDto? result, string? error)> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByUsernameAnyStatusAsync(dto.Username);
            if (user == null) return (null, "Invalid username or password.");
            if (!user.IsActive) return (null, "This account has been deactivated. Please contact the administrator.");
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return (null, "Invalid username or password.");

            var response = GenerateToken(user);

            if (user.ProfileId.HasValue)
            {
                response.ProfileId = user.ProfileId;
            }
            else if (user.Role == "Patient")
            {
                var patients = await _patientRepository.GetAllAsync();
                var match = patients.FirstOrDefault(p =>
                    p.LastName.Equals(user.Username, StringComparison.OrdinalIgnoreCase) ||
                    p.FirstName.Equals(user.Username, StringComparison.OrdinalIgnoreCase) ||
                    user.Username.Contains(p.LastName, StringComparison.OrdinalIgnoreCase) ||
                    p.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
                response.ProfileId = match?.Id;
            }
            else if (user.Role == "Doctor")
            {
                var doctors = await _doctorRepository.GetAllAsync();
                var match = doctors.FirstOrDefault(d =>
                    d.LastName.Equals(user.Username, StringComparison.OrdinalIgnoreCase) ||
                    d.FirstName.Equals(user.Username, StringComparison.OrdinalIgnoreCase) ||
                    user.Username.Contains(d.LastName, StringComparison.OrdinalIgnoreCase) ||
                    d.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
                response.ProfileId = match?.Id;
            }

            return (response, null);
        }

        public async Task<(AuthResponseDto? result, string? error)> RegisterAsync(RegisterDto dto)
        {
            var validRoles = new[] { "Admin", "Doctor", "Patient" };
            if (!validRoles.Contains(dto.Role))
                return (null, "Role must be Admin, Doctor, or Patient.");

            if (await _userRepository.UsernameExistsAsync(dto.Username))
                return (null, "Username already taken.");

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                Email = dto.Email,
                ProfileId = dto.ProfileId
            };

            var created = await _userRepository.CreateAsync(user);
            return (GenerateToken(created), null);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userRepository.GetByUsernameAsync(dto.Username);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                var expiresAt = DateTime.UtcNow.AddHours(1);
                await _userRepository.SetResetTokenAsync(user.Id, token, expiresAt);

                var baseUrl = _configuration["AppBaseUrl"] ?? "http://localhost:56076";
                var resetLink = $"{baseUrl}/reset-password.html?token={token}";
                var body = $"Hello {user.Username},\n\n" +
                    "We received a request to reset your password. Click the link below to choose a new one:\n" +
                    $"{resetLink}\n\n" +
                    "This link expires in 1 hour. If you didn't request this, you can safely ignore this email.";

                await _emailService.SendAsync(user.Email, user.Username, "Reset your password", body);
            }
            // No branch on whether the user/email was found - the caller always sees the
            // same generic response, so this endpoint can't be used to enumerate usernames.
        }

        public async Task<(bool success, string? error)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userRepository.GetByResetTokenAsync(dto.Token);
            if (user == null || user.ResetTokenExpiresAt == null || user.ResetTokenExpiresAt < DateTime.UtcNow)
                return (false, "This reset link is invalid or has expired.");

            await _userRepository.ResetPasswordAsync(user.Id, BCrypt.Net.BCrypt.HashPassword(dto.NewPassword));
            return (true, null);
        }

        private AuthResponseDto GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(8);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.Username,
                Role = user.Role,
                ExpiresAt = expires
            };
        }
    }
}
