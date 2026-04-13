using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Mappings
{
    public class EffectiveScheduleProfile : Profile
    {
        public EffectiveScheduleProfile()
        {
            CreateMap<EffectiveSchedule, EffectiveScheduleDto>();
            CreateMap<EffectiveTimeSlot, EffectiveTimeSlotDto>();
        }
    }
}
