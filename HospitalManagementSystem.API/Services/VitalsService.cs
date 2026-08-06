using AutoMapper;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Models;
using HospitalManagementSystem.API.Repositories.Interfaces;
using HospitalManagementSystem.API.Services.Interfaces;

namespace HospitalManagementSystem.API.Services
{
    public class VitalsService : IVitalsService
    {
        private readonly IVitalsRepository _vitalsRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public VitalsService(
            IVitalsRepository vitalsRepository,
            IPatientRepository patientRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _vitalsRepository = vitalsRepository;
            _patientRepository = patientRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<VitalsReadDto>> GetByPatientIdAsync(int patientId)
        {
            var records = await _vitalsRepository.GetByPatientIdAsync(patientId);
            return _mapper.Map<IEnumerable<VitalsReadDto>>(records);
        }

        public async Task<(VitalsReadDto? result, string? error)> CreateAsync(VitalsCreateDto dto, string creatorRole, string callerUsername)
        {
            if (!await _patientRepository.ExistsAsync(dto.PatientId))
                return (null, $"Patient with ID {dto.PatientId} not found.");

            if (!dto.BloodPressureSystolic.HasValue && !dto.BloodPressureDiastolic.HasValue &&
                !dto.HeartRate.HasValue && !dto.Temperature.HasValue && !dto.Weight.HasValue)
                return (null, "At least one vital measurement must be provided.");

            var record = _mapper.Map<VitalsRecord>(dto);
            record.RecordDate = dto.RecordDate ?? DateTime.UtcNow;

            if (creatorRole == "Doctor")
            {
                var user = await _userRepository.GetByUsernameAsync(callerUsername);
                record.RecordedByDoctorId = user?.ProfileId;
            }

            var created = await _vitalsRepository.CreateAsync(record);
            var all = await _vitalsRepository.GetByPatientIdAsync(dto.PatientId);
            var full = all.FirstOrDefault(v => v.Id == created.Id);
            return (_mapper.Map<VitalsReadDto>(full ?? created), null);
        }
    }
}
