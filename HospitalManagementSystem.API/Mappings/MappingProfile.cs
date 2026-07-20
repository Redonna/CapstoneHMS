using AutoMapper;
using HospitalManagementSystem.API.DTOs;
using HospitalManagementSystem.API.Models;

namespace HospitalManagementSystem.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Patient
            CreateMap<PatientCreateDto, Patient>();
            CreateMap<Patient, PatientReadDto>()
                .ForMember(dest => dest.TotalAppointments,
                    opt => opt.MapFrom(src => src.Appointments.Count));

            // Doctor
            CreateMap<DoctorCreateDto, Doctor>();
            CreateMap<Doctor, DoctorReadDto>()
                .ForMember(dest => dest.TotalAppointments,
                    opt => opt.MapFrom(src => src.Appointments.Count));

            // Appointment
            CreateMap<AppointmentCreateDto, Appointment>();
            CreateMap<Appointment, AppointmentReadDto>()
                .ForMember(dest => dest.PatientName,
                    opt => opt.MapFrom(src => src.Patient.FirstName + " " + src.Patient.LastName))
                .ForMember(dest => dest.DoctorName,
                    opt => opt.MapFrom(src => src.Doctor.FirstName + " " + src.Doctor.LastName))
                .ForMember(dest => dest.DoctorSpecialization,
                    opt => opt.MapFrom(src => src.Doctor.Specialization))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()));

            // Doctor-Patient Assignment
            CreateMap<AssignmentCreateDto, DoctorPatientAssignment>();
            CreateMap<DoctorPatientAssignment, AssignmentReadDto>()
                .ForMember(dest => dest.PatientName,
                    opt => opt.MapFrom(src => src.Patient.FirstName + " " + src.Patient.LastName))
                .ForMember(dest => dest.DoctorName,
                    opt => opt.MapFrom(src => src.Doctor.FirstName + " " + src.Doctor.LastName))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()));

            // Patient History
            CreateMap<PatientHistoryCreateDto, PatientHistoryEntry>();
            CreateMap<PatientHistoryEntry, PatientHistoryReadDto>()
                .ForMember(dest => dest.RecordedByDoctorName,
                    opt => opt.MapFrom(src => src.RecordedByDoctor == null ? "Admin" : src.RecordedByDoctor.FirstName + " " + src.RecordedByDoctor.LastName))
                .ForMember(dest => dest.HasAttachment,
                    opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.AttachmentStoredPath)));
        }
    }
}
