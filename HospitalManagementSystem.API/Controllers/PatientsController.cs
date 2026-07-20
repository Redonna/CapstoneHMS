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
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _service;
        private readonly IAssignmentService _assignmentService;

        public PatientsController(IPatientService service, IAssignmentService assignmentService)
        {
            _service = service;
            _assignmentService = assignmentService;
        }

        /// <summary>Get the patient profile for the currently authenticated patient user</summary>
        [HttpGet("me")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMe()
        {
            var username = User.Identity?.Name;
            var patients = await _service.GetAllAsync();
            var match = patients.FirstOrDefault(p =>
                p.FirstName.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                p.LastName.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                (username != null && username.Contains(p.LastName, StringComparison.OrdinalIgnoreCase)) ||
                (username != null && username.Contains(p.FirstName, StringComparison.OrdinalIgnoreCase)));
            return match == null ? NotFound("No patient profile found for the current user.") : Ok(match);
        }

        /// <summary>Get all active patients</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> GetAll()
        {
            var patients = await _service.GetAllAsync();
            return Ok(patients);
        }

        /// <summary>Get a patient by ID</summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var patient = await _service.GetByIdAsync(id);
            return patient == null ? NotFound($"Patient with ID {id} not found.") : Ok(patient);
        }

        /// <summary>Register a new patient. When created by a Doctor, the patient is automatically assigned and accepted for that doctor.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _service.CreateAsync(dto);

            if (User.FindFirstValue(ClaimTypes.Role) == "Doctor")
            {
                var username = User.Identity?.Name ?? string.Empty;
                await _assignmentService.CreateSelfAssignedAsync(created.Id, username);
            }

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Update patient information</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Update(int id, [FromBody] PatientUpdateDto dto)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var updated = await _service.UpdateAsync(id, dto);
            return updated == null ? NotFound($"Patient with ID {id} not found.") : Ok(updated);
        }

        /// <summary>Soft-delete a patient</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound($"Patient with ID {id} not found.");
        }
    }
}
