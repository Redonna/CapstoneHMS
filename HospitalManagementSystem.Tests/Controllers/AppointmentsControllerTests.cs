using System.Security.Claims;
using HospitalManagementSystem.API.Controllers;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HospitalManagementSystem.Tests.Controllers
{
    public class AppointmentsControllerTests
    {
        private readonly IAppointmentService _service;
        private readonly AppointmentsController _controller;

        private const string CallerUsername = "drsmith";

        public AppointmentsControllerTests()
        {
            _service = Substitute.For<IAppointmentService>();
            _controller = new AppointmentsController(_service);
            SetCaller("Admin", CallerUsername);
        }

        private void SetCaller(string role, string username)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, username)
            }, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        private AppointmentReadDto MakeDto(int id = 1) => new()
        {
            Id = id,
            PatientId = 1,
            PatientName = "Elizabeth Jones",
            DoctorId = 1,
            DoctorName = "James Smith",
            DoctorSpecialization = "Cardiology",
            AppointmentDate = DateTime.UtcNow.AddDays(7),
            Reason = "Routine checkup",
            Status = "Scheduled",
            Notes = "",
            CreatedAt = DateTime.UtcNow
        };

        // ── GET /api/appointments ─────────────────────────────────────────────

        [Fact]
        public async Task GetAll_Returns200_WithAppointments()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<AppointmentReadDto> { MakeDto(1), MakeDto(2) });

            // Act
            var result = await _controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<AppointmentReadDto>>(ok.Value);
            Assert.Equal(2, data.Count());
        }

        // ── GET /api/appointments/{id} ────────────────────────────────────────

        [Fact]
        public async Task GetById_Returns200_WhenFound()
        {
            // Arrange
            _service.GetByIdAsync(1).Returns(MakeDto(1));

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<AppointmentReadDto>(ok.Value);
            Assert.Equal(1, data.Id);
        }

        [Fact]
        public async Task GetById_Returns404_WhenNotFound()
        {
            // Arrange
            _service.GetByIdAsync(99).Returns((AppointmentReadDto?)null);

            // Act
            var result = await _controller.GetById(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetById_Returns400_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.GetById(0);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── POST /api/appointments ────────────────────────────────────────────

        [Fact]
        public async Task Create_Returns201_WhenValid()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.AddDays(7),
                Reason = "Routine checkup"
            };
            _service.CreateAsync(dto, "Admin").Returns((MakeDto(1), (string?)null));

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
        }

        [Fact]
        public async Task Create_Returns400_WhenServiceReturnsError()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.AddDays(-1),
                Reason = "Past date"
            };
            _service.CreateAsync(dto, "Admin").Returns(((AppointmentReadDto?)null, "Appointment date must be in the future."));

            // Act
            var result = await _controller.Create(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── PATCH /api/appointments/{id}/cancel ───────────────────────────────

        [Fact]
        public async Task Cancel_Returns204_WhenCancelled()
        {
            // Arrange
            _service.CancelAsync(1).Returns(true);

            // Act
            var result = await _controller.Cancel(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Cancel_Returns404_WhenNotFound()
        {
            // Arrange
            _service.CancelAsync(99).Returns(false);

            // Act
            var result = await _controller.Cancel(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ── PATCH /api/appointments/{id}/accept ───────────────────────────────

        [Fact]
        public async Task Accept_Returns200_WhenPending()
        {
            // Arrange
            var dto = MakeDto(1);
            dto.Status = "Scheduled";
            _service.AcceptAsync(1, CallerUsername).Returns((dto, (string?)null));

            // Act
            var result = await _controller.Accept(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task Accept_Returns400_WhenNotPending()
        {
            // Arrange
            _service.AcceptAsync(1, CallerUsername).Returns(((AppointmentReadDto?)null, "Only pending appointments can be accepted."));

            // Act
            var result = await _controller.Accept(1);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── PATCH /api/appointments/{id}/deny ─────────────────────────────────

        [Fact]
        public async Task Deny_Returns200_WhenPending()
        {
            // Arrange
            var dto = MakeDto(1);
            dto.Status = "Denied";
            _service.DenyAsync(1, CallerUsername).Returns((dto, (string?)null));

            // Act
            var result = await _controller.Deny(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task Deny_Returns400_WhenNotPending()
        {
            // Arrange
            _service.DenyAsync(1, CallerUsername).Returns(((AppointmentReadDto?)null, "Only pending appointments can be denied."));

            // Act
            var result = await _controller.Deny(1);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
