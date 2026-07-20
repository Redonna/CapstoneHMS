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
    public class PatientAssignmentsController : ControllerBase
    {
        private readonly IAssignmentService _service;

        public PatientAssignmentsController(IAssignmentService service)
        {
            _service = service;
        }

        /// <summary>Get all doctor-patient assignments</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var assignments = await _service.GetAllAsync();
            return Ok(assignments);
        }

        /// <summary>Get all patients assigned to a specific doctor</summary>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> GetByDoctor(int doctorId)
        {
            if (doctorId <= 0) return BadRequest("Doctor ID must be greater than 0.");
            var assignments = await _service.GetByDoctorIdAsync(doctorId);
            return Ok(assignments);
        }

        /// <summary>Get all doctor assignments for a specific patient</summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            if (patientId <= 0) return BadRequest("Patient ID must be greater than 0.");
            var assignments = await _service.GetByPatientIdAsync(patientId);
            return Ok(assignments);
        }

        /// <summary>Admin assigns a patient to a doctor (no appointment needed)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] AssignmentCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var (result, error) = await _service.CreateAsync(dto);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }

        /// <summary>Doctor accepts a pending patient assignment</summary>
        [HttpPatch("{id}/accept")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Accept(int id)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var username = User.Identity?.Name ?? string.Empty;
            var (result, error) = await _service.AcceptAsync(id, username);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }

        /// <summary>Doctor denies a pending patient assignment</summary>
        [HttpPatch("{id}/deny")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Deny(int id)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var username = User.Identity?.Name ?? string.Empty;
            var (result, error) = await _service.DenyAsync(id, username);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }

        /// <summary>Remove a doctor-patient assignment (does not delete the patient record itself)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Remove(int id)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var username = User.Identity?.Name ?? string.Empty;
            var (success, error) = await _service.RemoveAsync(id, role, username);
            if (error != null) return BadRequest(error);
            return success ? NoContent() : NotFound($"Assignment with ID {id} not found.");
        }
    }
}
