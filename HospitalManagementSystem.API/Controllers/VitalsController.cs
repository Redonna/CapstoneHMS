using System.Security.Claims;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VitalsController : ControllerBase
    {
        private readonly IVitalsService _service;

        public VitalsController(IVitalsService service)
        {
            _service = service;
        }

        /// <summary>Get the vitals timeline for a patient</summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            if (patientId <= 0) return BadRequest("Patient ID must be greater than 0.");
            var records = await _service.GetByPatientIdAsync(patientId);
            return Ok(records);
        }

        /// <summary>Log a vitals measurement for a patient</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Create([FromBody] VitalsCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var creatorRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var username = User.Identity?.Name ?? string.Empty;
            var (result, error) = await _service.CreateAsync(dto, creatorRole, username);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }
    }
}
