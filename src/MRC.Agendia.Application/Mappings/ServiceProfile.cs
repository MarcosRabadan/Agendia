using AutoMapper;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class ServiceProfile : Profile
    {
        public ServiceProfile()
        {
            CreateMap<Service, ServiceDto>();
            CreateMap<CreateServiceDto, Service>();

            // Update does NOT allow changing the BusinessId. Mapping it would let
            // an Owner move a Service from another tenant to his own by sending a
            // crafted DTO (see issue #91). The id stays as whatever the entity had.
            CreateMap<UpdateServiceDto, Service>()
                .ForMember(dest => dest.BusinessId, opt => opt.Ignore());
        }
    }
}
