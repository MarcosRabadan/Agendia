using AutoMapper;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.Mappings
{
    public class BusinessScheduleProfile : Profile
    {
        public BusinessScheduleProfile()
        {
            CreateMap<Domain.Entities.BusinessSchedule, BusinessScheduleDto>();
            CreateMap<CreateBusinessScheduleDto, Domain.Entities.BusinessSchedule>();
            CreateMap<UpdateBusinessScheduleDto, Domain.Entities.BusinessSchedule>();
        }
    }
}
