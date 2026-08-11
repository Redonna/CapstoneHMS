using AutoMapper;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using HospitalManagementSystem.API.Services.Interfaces;

namespace HospitalManagementSystem.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            IMapper mapper)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AppointmentReadDto>> GetAllAsync()
        {
            var appointments = await _appointmentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<AppointmentReadDto>>(appointments);
        }

        public async Task<AppointmentReadDto?> GetByIdAsync(int id)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            return appointment == null ? null : _mapper.Map<AppointmentReadDto>(appointment);
        }

        public async Task<IEnumerable<AppointmentReadDto>> GetByPatientIdAsync(int patientId)
        {
            var appointments = await _appointmentRepository.GetByPatientIdAsync(patientId);
            return _mapper.Map<IEnumerable<AppointmentReadDto>>(appointments);
        }

        public async Task<IEnumerable<AppointmentReadDto>> GetByDoctorIdAsync(int doctorId)
        {
            var appointments = await _appointmentRepository.GetByDoctorIdAsync(doctorId);
            return _mapper.Map<IEnumerable<AppointmentReadDto>>(appointments);
        }

        public async Task<(AppointmentReadDto? result, string? error)> CreateAsync(AppointmentCreateDto dto, string creatorRole)
        {
            // Business rule: appointment must be in the future
            if (dto.AppointmentDate <= DateTime.UtcNow)
                return (null, "Appointment date must be in the future.");

            // Business rule: patient must exist
            if (!await _patientRepository.ExistsAsync(dto.PatientId))
                return (null, $"Patient with ID {dto.PatientId} not found.");

            // Business rule: doctor must exist
            if (!await _doctorRepository.ExistsAsync(dto.DoctorId))
                return (null, $"Doctor with ID {dto.DoctorId} not found.");

            // Business rule: doctor cannot have two appointments at the same time
            var doctorAppointments = await _appointmentRepository.GetByDoctorIdAsync(dto.DoctorId);
            bool conflict = doctorAppointments.Any(a =>
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Denied &&
                Math.Abs((a.AppointmentDate - dto.AppointmentDate).TotalMinutes) < 30);

            if (conflict)
                return (null, "The doctor already has an appointment within 30 minutes of the requested time.");

            var appointment = _mapper.Map<Appointment>(dto);
            // Admin-assigned appointments need the doctor's confirmation; a doctor booking
            // their own appointment is confirmed immediately.
            appointment.Status = creatorRole == "Admin" ? AppointmentStatus.Pending : AppointmentStatus.Scheduled;
            var created = await _appointmentRepository.CreateAsync(appointment);

            if (created.Status == AppointmentStatus.Pending)
                await NotifyDoctorOfNewAppointmentAsync(created);

            // Reload with navigation properties for mapping
            var full = await _appointmentRepository.GetByIdAsync(created.Id);
            return (_mapper.Map<AppointmentReadDto>(full), null);
        }

        public async Task<(AppointmentReadDto? result, string? error)> UpdateAsync(int id, AppointmentUpdateDto dto)
        {
            var existing = await _appointmentRepository.GetByIdAsync(id);
            if (existing == null) return (null, "Appointment not found.");

            if (existing.Status == AppointmentStatus.Cancelled)
                return (null, "Cannot update a cancelled appointment.");

            // Apply updates
            if (dto.AppointmentDate.HasValue)
            {
                if (dto.AppointmentDate.Value <= DateTime.UtcNow)
                    return (null, "Appointment date must be in the future.");
                existing.AppointmentDate = dto.AppointmentDate.Value;
            }
            if (dto.Reason != null) existing.Reason = dto.Reason;
            if (dto.Notes != null) existing.Notes = dto.Notes;
            if (dto.Status != null && Enum.TryParse<AppointmentStatus>(dto.Status, out var status))
                existing.Status = status;

            var updated = await _appointmentRepository.UpdateAsync(id, existing);
            return (updated == null ? null : _mapper.Map<AppointmentReadDto>(updated), null);
        }

        public async Task<bool> CancelAsync(int id)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null) return false;

            appointment.Status = AppointmentStatus.Cancelled;
            await _appointmentRepository.UpdateAsync(id, appointment);
            return true;
        }

        public async Task<(AppointmentReadDto? result, string? error)> AcceptAsync(int id, string callerUsername)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null) return (null, "Appointment not found.");

            if (appointment.Status != AppointmentStatus.Pending)
                return (null, "Only pending appointments can be accepted.");

            var owns = await IsOwnedByCallerAsync(appointment.DoctorId, callerUsername);
            if (!owns) return (null, "You can only accept appointments assigned to you.");

            appointment.Status = AppointmentStatus.Scheduled;
            var updated = await _appointmentRepository.UpdateAsync(id, appointment);
            await NotifyPatientOfStatusChangeAsync(appointment, accepted: true);
            return (_mapper.Map<AppointmentReadDto>(updated), null);
        }

        public async Task<(AppointmentReadDto? result, string? error)> DenyAsync(int id, string callerUsername)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null) return (null, "Appointment not found.");

            if (appointment.Status != AppointmentStatus.Pending)
                return (null, "Only pending appointments can be denied.");

            var owns = await IsOwnedByCallerAsync(appointment.DoctorId, callerUsername);
            if (!owns) return (null, "You can only deny appointments assigned to you.");

            appointment.Status = AppointmentStatus.Denied;
            var updated = await _appointmentRepository.UpdateAsync(id, appointment);
            await NotifyPatientOfStatusChangeAsync(appointment, accepted: false);
            return (_mapper.Map<AppointmentReadDto>(updated), null);
        }

        private async Task<bool> IsOwnedByCallerAsync(int doctorId, string callerUsername)
        {
            var user = await _userRepository.GetByUsernameAsync(callerUsername);
            return user?.ProfileId == doctorId;
        }

        private async Task NotifyDoctorOfNewAppointmentAsync(Appointment appointment)
        {
            var doctor = await _doctorRepository.GetByIdAsync(appointment.DoctorId);
            var patient = await _patientRepository.GetByIdAsync(appointment.PatientId);
            if (doctor == null || patient == null) return;

            var subject = "New appointment request";
            var body = $"Hello Dr. {doctor.LastName},\n\n" +
                $"A new appointment has been requested for you:\n" +
                $"Patient: {patient.FirstName} {patient.LastName}\n" +
                $"Date: {appointment.AppointmentDate:f}\n" +
                $"Reason: {appointment.Reason}\n\n" +
                "Please log in to accept or deny this appointment.";

            await _emailService.SendAsync(doctor.Email, $"{doctor.FirstName} {doctor.LastName}", subject, body);
        }

        private async Task NotifyPatientOfStatusChangeAsync(Appointment appointment, bool accepted)
        {
            var doctor = await _doctorRepository.GetByIdAsync(appointment.DoctorId);
            var patient = await _patientRepository.GetByIdAsync(appointment.PatientId);
            if (doctor == null || patient == null) return;

            var subject = accepted ? "Your appointment has been confirmed" : "Your appointment request was not accepted";
            var body = accepted
                ? $"Hello {patient.FirstName},\n\nYour appointment with Dr. {doctor.LastName} on {appointment.AppointmentDate:f} has been confirmed."
                : $"Hello {patient.FirstName},\n\nUnfortunately, your appointment request with Dr. {doctor.LastName} on {appointment.AppointmentDate:f} was not accepted. Please contact the clinic to reschedule.";

            await _emailService.SendAsync(patient.Email, $"{patient.FirstName} {patient.LastName}", subject, body);
        }
    }
}
