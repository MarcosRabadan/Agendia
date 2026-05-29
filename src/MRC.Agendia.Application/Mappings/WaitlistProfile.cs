using AutoMapper;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class WaitlistProfile : Profile
    {
        public WaitlistProfile()
        {
            CreateMap<WaitlistEntry, WaitlistEntryDto>();
        }
    }
}
