using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SymptomSummaryController : ControllerBase
    {
        private readonly ISymptomSummaryService _service;

        public SymptomSummaryController(ISymptomSummaryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Summarize a patient's reported symptoms: matches known symptoms in the text,
        /// predicts a likely illness via an in-process ONNX model, and returns suggested
        /// precautions/medications. Not a diagnosis — a starting-point suggestion only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Summarize([FromBody] SymptomSummaryRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
            var username = User.Identity?.Name ?? string.Empty;
            var (result, error) = await _service.SummarizeAsync(dto, role, username);
            if (error != null) return BadRequest(error);
            return Ok(result);
        }
    }
}
