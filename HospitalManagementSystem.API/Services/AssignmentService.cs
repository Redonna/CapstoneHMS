using AutoMapper;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using HospitalManagementSystem.API.Services.Interfaces;

namespace HospitalManagementSystem.API.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public AssignmentService(
            IAssignmentRepository assignmentRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _assignmentRepository = assignmentRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AssignmentReadDto>> GetAllAsync()
        {
            var assignments = await _assignmentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<AssignmentReadDto>>(assignments);
        }

        public async Task<IEnumerable<AssignmentReadDto>> GetByDoctorIdAsync(int doctorId)
        {
            var assignments = await _assignmentRepository.GetByDoctorIdAsync(doctorId);
            return _mapper.Map<IEnumerable<AssignmentReadDto>>(assignments);
        }

        public async Task<IEnumerable<AssignmentReadDto>> GetByPatientIdAsync(int patientId)
        {
            var assignments = await _assignmentRepository.GetByPatientIdAsync(patientId);
            return _mapper.Map<IEnumerable<AssignmentReadDto>>(assignments);
        }

        public async Task<(AssignmentReadDto? result, string? error)> CreateAsync(AssignmentCreateDto dto)
        {
            if (!await _patientRepository.ExistsAsync(dto.PatientId))
                return (null, $"Patient with ID {dto.PatientId} not found.");

            if (!await _doctorRepository.ExistsAsync(dto.DoctorId))
                return (null, $"Doctor with ID {dto.DoctorId} not found.");

            // A patient can only have one live assignment; reassigning supersedes any
            // still-pending or already-accepted assignment to a previous doctor.
            var existingAssignments = await _assignmentRepository.GetByPatientIdAsync(dto.PatientId);
            foreach (var previous in existingAssignments.Where(a => a.Status == AssignmentStatus.Pending || a.Status == AssignmentStatus.Accepted))
            {
                await _assignmentRepository.UpdateAsync(previous.Id, new DoctorPatientAssignment
                {
                    Status = AssignmentStatus.Cancelled,
                    DecidedAt = DateTime.UtcNow
                });
            }

            var assignment = _mapper.Map<DoctorPatientAssignment>(dto);
            var created = await _assignmentRepository.CreateAsync(assignment);

            var full = await _assignmentRepository.GetByIdAsync(created.Id);
            return (_mapper.Map<AssignmentReadDto>(full), null);
        }

        public async Task<(AssignmentReadDto? result, string? error)> CreateSelfAssignedAsync(int patientId, string callerUsername)
        {
            var user = await _userRepository.GetByUsernameAsync(callerUsername);
            if (user?.ProfileId == null)
                return (null, "Could not resolve your doctor profile.");

            var (created, error) = await CreateAsync(new AssignmentCreateDto { PatientId = patientId, DoctorId = user.ProfileId.Value });
            if (error != null) return (null, error);

            return await AcceptAsync(created!.Id, callerUsername);
        }

        public async Task<(AssignmentReadDto? result, string? error)> AcceptAsync(int id, string callerUsername)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null) return (null, "Assignment not found.");

            if (assignment.Status != AssignmentStatus.Pending)
                return (null, "Only pending assignments can be accepted.");

            var owns = await IsOwnedByCallerAsync(assignment.DoctorId, callerUsername);
            if (!owns) return (null, "You can only accept patients assigned to you.");

            assignment.Status = AssignmentStatus.Accepted;
            assignment.DecidedAt = DateTime.UtcNow;
            var updated = await _assignmentRepository.UpdateAsync(id, assignment);

            var full = await _assignmentRepository.GetByIdAsync(id);
            return (_mapper.Map<AssignmentReadDto>(full ?? updated), null);
        }

        public async Task<(AssignmentReadDto? result, string? error)> DenyAsync(int id, string callerUsername)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null) return (null, "Assignment not found.");

            if (assignment.Status != AssignmentStatus.Pending)
                return (null, "Only pending assignments can be denied.");

            var owns = await IsOwnedByCallerAsync(assignment.DoctorId, callerUsername);
            if (!owns) return (null, "You can only deny patients assigned to you.");

            assignment.Status = AssignmentStatus.Denied;
            assignment.DecidedAt = DateTime.UtcNow;
            var updated = await _assignmentRepository.UpdateAsync(id, assignment);

            var full = await _assignmentRepository.GetByIdAsync(id);
            return (_mapper.Map<AssignmentReadDto>(full ?? updated), null);
        }

        public async Task<(bool success, string? error)> RemoveAsync(int id, string callerRole, string callerUsername)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null) return (false, "Assignment not found.");

            if (callerRole == "Doctor")
            {
                var owns = await IsOwnedByCallerAsync(assignment.DoctorId, callerUsername);
                if (!owns) return (false, "You can only remove patients assigned to you.");
            }

            var deleted = await _assignmentRepository.DeleteAsync(id);
            return (deleted, deleted ? null : "Failed to remove assignment.");
        }

        private async Task<bool> IsOwnedByCallerAsync(int doctorId, string callerUsername)
        {
            var user = await _userRepository.GetByUsernameAsync(callerUsername);
            return user?.ProfileId == doctorId;
        }
    }
}
