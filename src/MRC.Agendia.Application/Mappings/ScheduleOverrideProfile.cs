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

            // No mapping for UpdateScheduleOverrideDto -> ScheduleOverride on
            // purpose. ScheduleService.UpdateOverrideAsync applies the changes
            // field by field so the entity's BusinessId is never reassigned by
            // a crafted DTO (issue #91 pattern). See also the defensive tests
            // in Tests.Integration/Schedules/ScheduleCrossTenantTests.

            CreateMap<CustomTimeSlot, CustomTimeSlotDto>();
            CreateMap<CreateCustomTimeSlotDto, CustomTimeSlot>();
        }
    }
}
