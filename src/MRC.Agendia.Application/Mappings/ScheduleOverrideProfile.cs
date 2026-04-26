using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class ScheduleOverrideProfile : Profile
    {
        public ScheduleOverrideProfile()
        {
            CreateMap<ScheduleOverride, ScheduleOverrideDto>();
            CreateMap<CreateScheduleOverrideDto, ScheduleOverride>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
            CreateMap<UpdateScheduleOverrideDto, ScheduleOverride>();

            CreateMap<CustomTimeSlot, CustomTimeSlotDto>();
            CreateMap<CreateCustomTimeSlotDto, CustomTimeSlot>();
        }
    }
}
