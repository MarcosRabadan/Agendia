using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Mappings
{
    public class EffectiveScheduleProfile : Profile
    {
        public EffectiveScheduleProfile()
        {
            // EffectiveSchedule -> EffectiveScheduleDto is built manually in
            // ScheduleService because it also needs ActiveTemplate and Templates.
            CreateMap<EffectiveTimeSlot, EffectiveTimeSlotDto>();
        }
    }
}
