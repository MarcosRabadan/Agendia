using AutoMapper;
using MRC.Agendia.Application.Holidays.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class HolidayCalendarProfile : Profile
    {
        public HolidayCalendarProfile()
        {
            CreateMap<HolidayCalendar, HolidayCalendarDto>();
            CreateMap<CreateHolidayCalendarDto, HolidayCalendar>();
            CreateMap<UpdateHolidayCalendarDto, HolidayCalendar>();
        }
    }
}
