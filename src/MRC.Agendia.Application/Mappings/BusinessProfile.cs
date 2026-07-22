using AutoMapper;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Mappings
{
    public class BusinessProfile : Profile
    {
        public BusinessProfile()
        {
            CreateMap<Domain.Entities.Business, BusinessDto>();
            CreateMap<Domain.Entities.Business, BusinessPublicDto>();
            CreateMap<CreateBusinessDto, Domain.Entities.Business>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.DefaultLanguage, opt => opt.MapFrom(src => SupportedLanguages.Normalize(src.DefaultLanguage)));
            // Update must never repoint the owner: mapping OwnerUserId here would let
            // anyone who can edit a business hand it to another user (or take it over
            // themselves) via a crafted DTO. Same vector as the BusinessId rule (#91/#92).
            CreateMap<UpdateBusinessDto, Domain.Entities.Business>()
                .ForMember(dest => dest.OwnerUserId, opt => opt.Ignore())
                .ForMember(dest => dest.DefaultLanguage, opt => opt.MapFrom(src => SupportedLanguages.Normalize(src.DefaultLanguage)));
        }
    }
}
