using AutoMapper;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Mappings
{
    public class BusinessProfile : Profile
    {
        public BusinessProfile()
        {
            CreateMap<Domain.Entities.Business, BusinessDto>();
            CreateMap<Domain.Entities.Business, BusinessPublicDto>();
            CreateMap<CreateBusinessDto, Domain.Entities.Business>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));
            CreateMap<UpdateBusinessDto, Domain.Entities.Business>();
        }
    }
}
