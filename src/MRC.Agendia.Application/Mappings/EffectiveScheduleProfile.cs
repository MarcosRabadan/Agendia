using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Mappings
{
    public class EffectiveScheduleProfile : Profile
    {
        public EffectiveScheduleProfile()
        {
            // EffectiveSchedule -> EffectiveScheduleDto se construye manualmente
            // en ScheduleService porque necesita anadir ActiveTemplate y Templates.
            CreateMap<EffectiveTimeSlot, EffectiveTimeSlotDto>();
        }
    }
}
