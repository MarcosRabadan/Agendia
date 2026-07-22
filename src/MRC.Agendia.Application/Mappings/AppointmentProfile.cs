using AutoMapper;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class AppointmentProfile : Profile
    {
        public AppointmentProfile()
        {
            // AppointmentDto is a positional record, so the value comes from the
            // constructor parameter (ForMember would not apply): map the extra
            // service ids via ForCtorParam.
            CreateMap<Appointment, AppointmentDto>()
                .ForCtorParam("ExtraServiceIds",
                    opt => opt.MapFrom(src => src.ExtraServices.Select(e => e.ServiceId).ToList()));
            CreateMap<CreateAppointmentDto, Appointment>()
                // Status is resolved in the service (business default or staff override).
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ExtraServices, opt => opt.Ignore());
            CreateMap<UpdateAppointmentDto, Appointment>()
                .ForMember(dest => dest.ExtraServices, opt => opt.Ignore());
        }
    }
}
