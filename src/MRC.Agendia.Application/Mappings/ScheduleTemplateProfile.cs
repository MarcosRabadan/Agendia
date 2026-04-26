using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class ScheduleTemplateProfile : Profile
    {
        public ScheduleTemplateProfile()
        {
            CreateMap<ScheduleTemplate, ScheduleTemplateDto>();
            CreateMap<CreateScheduleTemplateDto, ScheduleTemplate>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
            CreateMap<GenerateScheduleTemplateInputDto, ScheduleTemplate>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessId, opt => opt.Ignore())
                .ForMember(dest => dest.WeeklySlots, opt => opt.MapFrom(src => src.WeeklySlots));

            CreateMap<WeeklyTimeSlot, WeeklyTimeSlotDto>();
            CreateMap<CreateWeeklyTimeSlotDto, WeeklyTimeSlot>();
        }
    }
}
