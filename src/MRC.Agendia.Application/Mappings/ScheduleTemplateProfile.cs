using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Mappings
{
    public class ScheduleTemplateProfile : Profile
    {
        public ScheduleTemplateProfile()
        {
            CreateMap<ScheduleTemplate, ScheduleTemplateDto>();
            CreateMap<CreateScheduleTemplateDto, ScheduleTemplate>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
            CreateMap<GenerateScheduleRequestDto, ScheduleTemplate>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.WeeklySlots, opt => opt.MapFrom(src => src.WeeklySlots));

            CreateMap<WeeklyTimeSlot, WeeklyTimeSlotDto>();
            CreateMap<CreateWeeklyTimeSlotDto, WeeklyTimeSlot>();
        }
    }
}
