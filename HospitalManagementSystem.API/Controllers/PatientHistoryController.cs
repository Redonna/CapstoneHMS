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
    public class PatientHistoryController : ControllerBase
    {
        private readonly IPatientHistoryService _service;

        public PatientHistoryController(IPatientHistoryService service)
        {
            _service = service;
        }

        /// <summary>Get the medical history timeline for a patient</summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            if (patientId <= 0) return BadRequest("Patient ID must be greater than 0.");
            var entries = await _service.GetByPatientIdAsync(patientId);
            return Ok(entries);
        }

        /// <summary>Add a medical history entry for a patient</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Create([FromBody] PatientHistoryCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var creatorRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var username = User.Identity?.Name ?? string.Empty;
            var (result, error) = await _service.CreateAsync(dto, creatorRole, username);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }

        /// <summary>Upload a document attachment (PDF/image/Word, max 10MB) for a history entry</summary>
        [HttpPost("{id}/attachment")]
        [Authorize(Roles = "Admin,Doctor")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var (result, error) = await _service.UploadAttachmentAsync(id, file);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }

        /// <summary>Download the document attached to a history entry</summary>
        [HttpGet("{id}/attachment")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            if (id <= 0) return BadRequest("ID must be greater than 0.");
            var (path, fileName, error) = await _service.GetAttachmentAsync(id);
            if (error != null) return NotFound(error);
            return PhysicalFile(path!, "application/octet-stream", fileName);
        }
    }
}
