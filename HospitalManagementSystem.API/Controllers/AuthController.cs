using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Login and receive a JWT token</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (result, error) = await _authService.LoginAsync(dto);
            if (error != null) return Unauthorized(error);
            return Ok(result);
        }

        /// <summary>Register a new user (Admin only)</summary>
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (result, error) = await _authService.RegisterAsync(dto);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }

        /// <summary>Request a password reset email</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message = "If an account with that username exists and has an email on file, a reset link has been sent." });
        }

        /// <summary>Reset a password using a token from the reset email</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (success, error) = await _authService.ResetPasswordAsync(dto);
            if (!success) return BadRequest(error);
            return Ok(new { message = "Your password has been reset. You can now log in." });
        }
    }
}
