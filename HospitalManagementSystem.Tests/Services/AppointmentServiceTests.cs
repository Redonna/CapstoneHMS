using AutoMapper;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Mappings;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using HospitalManagementSystem.API.Services;
using NSubstitute;
using Xunit;

namespace HospitalManagementSystem.Tests.Services
{
    public class AppointmentServiceTests
    {
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IPatientRepository _patientRepo;
        private readonly IDoctorRepository _doctorRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly AppointmentService _service;

        private const string DoctorUsername = "drsmith";

        public AppointmentServiceTests()
        {
            _appointmentRepo = Substitute.For<IAppointmentRepository>();
            _patientRepo = Substitute.For<IPatientRepository>();
            _doctorRepo = Substitute.For<IDoctorRepository>();
            _userRepo = Substitute.For<IUserRepository>();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
            _service = new AppointmentService(_appointmentRepo, _patientRepo, _doctorRepo, _userRepo, _mapper);

            // By default the caller is the doctor assigned to appointment DoctorId=1 (see MakeAppointment)
            _userRepo.GetByUsernameAsync(DoctorUsername).Returns(new User { Id = 1, Username = DoctorUsername, Role = "Doctor", ProfileId = 1 });
        }

        private Appointment MakeAppointment(int id = 1) => new()
        {
            Id = id,
            PatientId = 1,
            DoctorId = 1,
            AppointmentDate = DateTime.UtcNow.AddDays(7),
            Reason = "Routine checkup",
            Status = AppointmentStatus.Scheduled,
            Notes = "",
            CreatedAt = DateTime.UtcNow,
            Patient = new Patient
            {
                Id = 1, FirstName = "Elizabeth", LastName = "Jones",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "Female",
                PhoneNumber = "072345678", Email = "elizabeth@test.com",
                IsActive = true, Appointments = new List<Appointment>()
            },
            Doctor = new Doctor
            {
                Id = 1, FirstName = "James", LastName = "Smith",
                Specialization = "Cardiology", PhoneNumber = "071234567",
                Email = "james@hospital.com", Department = "Cardiology",
                IsActive = true, Appointments = new List<Appointment>()
            }
        };

        // ── Happy Path ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsMappedDtos()
        {
            // Arrange
            _appointmentRepo.GetAllAsync().Returns(new List<Appointment> { MakeAppointment(1), MakeAppointment(2) });

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            await _appointmentRepo.Received(1).GetAllAsync();
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenFound()
        {
            // Arrange
            _appointmentRepo.GetByIdAsync(1).Returns(MakeAppointment(1));

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Elizabeth Jones", result.PatientName);
        }

        [Fact]
        public async Task GetByPatientIdAsync_ReturnsDtos()
        {
            // Arrange
            _appointmentRepo.GetByPatientIdAsync(1).Returns(new List<Appointment> { MakeAppointment(1) });

            // Act
            var result = await _service.GetByPatientIdAsync(1);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsError_WhenDateIsInPast()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.AddDays(-1),
                Reason = "Checkup"
            };

            // Act
            var (result, error) = await _service.CreateAsync(dto, "Admin");

            // Assert
            Assert.Null(result);
            Assert.Equal("Appointment date must be in the future.", error);
        }

        [Fact]
        public async Task CreateAsync_ReturnsError_WhenPatientNotFound()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                PatientId = 99,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.AddDays(7),
                Reason = "Checkup"
            };
            _patientRepo.ExistsAsync(99).Returns(false);

            // Act
            var (result, error) = await _service.CreateAsync(dto, "Admin");

            // Assert
            Assert.Null(result);
            Assert.Contains("Patient", error);
        }

        [Fact]
        public async Task CreateAsync_ReturnsError_WhenDoctorNotFound()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                PatientId = 1,
                DoctorId = 99,
                AppointmentDate = DateTime.UtcNow.AddDays(7),
                Reason = "Checkup"
            };
            _patientRepo.ExistsAsync(1).Returns(true);
            _doctorRepo.ExistsAsync(99).Returns(false);

            // Act
            var (result, error) = await _service.CreateAsync(dto, "Admin");

            // Assert
            Assert.Null(result);
            Assert.Contains("Doctor", error);
        }

        [Fact]
        public async Task CreateAsync_SetsPending_WhenCreatedByAdmin()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.AddDays(7),
                Reason = "Checkup"
            };
            _patientRepo.ExistsAsync(1).Returns(true);
            _doctorRepo.ExistsAsync(1).Returns(true);
            _appointmentRepo.GetByDoctorIdAsync(1).Returns(new List<Appointment>());
            _appointmentRepo.CreateAsync(Arg.Any<Appointment>())
                .Returns(ci => ci.Arg<Appointment>());
            _appointmentRepo.GetByIdAsync(0).ReturnsForAnyArgs(MakeAppointment(1));

            // Act
            var (result, error) = await _service.CreateAsync(dto, "Admin");

            // Assert
            Assert.Null(error);
            await _appointmentRepo.Received(1).CreateAsync(
                Arg.Is<Appointment>(a => a.Status == AppointmentStatus.Pending));
        }

        [Fact]
        public async Task CreateAsync_SetsScheduled_WhenCreatedByDoctor()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.AddDays(7),
                Reason = "Checkup"
            };
            _patientRepo.ExistsAsync(1).Returns(true);
            _doctorRepo.ExistsAsync(1).Returns(true);
            _appointmentRepo.GetByDoctorIdAsync(1).Returns(new List<Appointment>());
            _appointmentRepo.CreateAsync(Arg.Any<Appointment>())
                .Returns(ci => ci.Arg<Appointment>());
            _appointmentRepo.GetByIdAsync(0).ReturnsForAnyArgs(MakeAppointment(1));

            // Act
            var (result, error) = await _service.CreateAsync(dto, "Doctor");

            // Assert
            Assert.Null(error);
            await _appointmentRepo.Received(1).CreateAsync(
                Arg.Is<Appointment>(a => a.Status == AppointmentStatus.Scheduled));
        }

        [Fact]
        public async Task AcceptAsync_SetsScheduled_WhenPendingAndOwnedByCaller()
        {
            // Arrange
            var appointment = MakeAppointment(1);
            appointment.Status = AppointmentStatus.Pending;
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);
            _appointmentRepo.UpdateAsync(1, Arg.Any<Appointment>()).Returns(appointment);

            // Act
            var (result, error) = await _service.AcceptAsync(1, DoctorUsername);

            // Assert
            Assert.Null(error);
            Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        }

        [Fact]
        public async Task DenyAsync_SetsDenied_WhenPendingAndOwnedByCaller()
        {
            // Arrange
            var appointment = MakeAppointment(1);
            appointment.Status = AppointmentStatus.Pending;
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);
            _appointmentRepo.UpdateAsync(1, Arg.Any<Appointment>()).Returns(appointment);

            // Act
            var (result, error) = await _service.DenyAsync(1, DoctorUsername);

            // Assert
            Assert.Null(error);
            Assert.Equal(AppointmentStatus.Denied, appointment.Status);
        }

        [Fact]
        public async Task CancelAsync_ReturnsTrue_WhenAppointmentExists()
        {
            // Arrange
            var appointment = MakeAppointment(1);
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);
            _appointmentRepo.UpdateAsync(1, Arg.Any<Appointment>()).Returns(appointment);

            // Act
            var result = await _service.CancelAsync(1);

            // Assert
            Assert.True(result);
            await _appointmentRepo.Received(1).UpdateAsync(1, Arg.Any<Appointment>());
        }

        // ── Sad Path ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            _appointmentRepo.GetByIdAsync(99).Returns((Appointment?)null);

            // Act
            var result = await _service.GetByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CancelAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            _appointmentRepo.GetByIdAsync(99).Returns((Appointment?)null);

            // Act
            var result = await _service.CancelAsync(99);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AcceptAsync_ReturnsError_WhenNotPending()
        {
            // Arrange
            var appointment = MakeAppointment(1);
            appointment.Status = AppointmentStatus.Scheduled;
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);

            // Act
            var (result, error) = await _service.AcceptAsync(1, DoctorUsername);

            // Assert
            Assert.Null(result);
            Assert.Equal("Only pending appointments can be accepted.", error);
        }

        [Fact]
        public async Task DenyAsync_ReturnsError_WhenNotPending()
        {
            // Arrange
            var appointment = MakeAppointment(1);
            appointment.Status = AppointmentStatus.Scheduled;
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);

            // Act
            var (result, error) = await _service.DenyAsync(1, DoctorUsername);

            // Assert
            Assert.Null(result);
            Assert.Equal("Only pending appointments can be denied.", error);
        }

        [Fact]
        public async Task AcceptAsync_ReturnsError_WhenNotOwnedByCaller()
        {
            // Arrange
            var appointment = MakeAppointment(1); // DoctorId = 1
            appointment.Status = AppointmentStatus.Pending;
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);
            _userRepo.GetByUsernameAsync("otherdoctor").Returns(new User { Id = 2, Username = "otherdoctor", Role = "Doctor", ProfileId = 2 });

            // Act
            var (result, error) = await _service.AcceptAsync(1, "otherdoctor");

            // Assert
            Assert.Null(result);
            Assert.Equal("You can only accept appointments assigned to you.", error);
        }

        [Fact]
        public async Task DenyAsync_ReturnsError_WhenNotOwnedByCaller()
        {
            // Arrange
            var appointment = MakeAppointment(1); // DoctorId = 1
            appointment.Status = AppointmentStatus.Pending;
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);
            _userRepo.GetByUsernameAsync("otherdoctor").Returns(new User { Id = 2, Username = "otherdoctor", Role = "Doctor", ProfileId = 2 });

            // Act
            var (result, error) = await _service.DenyAsync(1, "otherdoctor");

            // Assert
            Assert.Null(result);
            Assert.Equal("You can only deny appointments assigned to you.", error);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsError_WhenAppointmentCancelled()
        {
            // Arrange
            var appointment = MakeAppointment(1);
            appointment.Status = AppointmentStatus.Cancelled;
            _appointmentRepo.GetByIdAsync(1).Returns(appointment);

            // Act
            var (result, error) = await _service.UpdateAsync(1, new AppointmentUpdateDto());

            // Assert
            Assert.Null(result);
            Assert.Equal("Cannot update a cancelled appointment.", error);
        }
    }
}
