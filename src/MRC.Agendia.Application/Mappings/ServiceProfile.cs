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

            // UpdateServiceDto carries no BusinessId, so a Service cannot be moved
            // to another tenant on update (see issue #91). BusinessId keeps whatever
            // value the existing entity already had.
            CreateMap<UpdateServiceDto, Service>();
        }
    }
}
