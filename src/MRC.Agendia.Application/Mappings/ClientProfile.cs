using AutoMapper;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class ClientProfile : Profile
    {
        public ClientProfile()
        {
            CreateMap<Client, ClientDto>();
            CreateMap<CreateClientDto, Client>();
            // UserId is not mappable on update: repointing an existing client to
            // another Harmony user would hand that user the client's appointments.
            CreateMap<UpdateClientDto, Client>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore());
        }
    }
}
