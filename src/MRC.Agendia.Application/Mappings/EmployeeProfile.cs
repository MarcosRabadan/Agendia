using AutoMapper;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Application.Mappings
{
    public class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>();
            CreateMap<CreateEmployeeDto, Employee>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

            // Update does NOT allow changing the BusinessId. Mapping it would let an
            // Employee (or Owner) move an employee to another tenant via a crafted
            // DTO (cross-tenant takeover, same vector as #91/#92). See issue #125.
            // UserId is likewise not mappable on update: repointing an existing
            // employee to another Harmony user would grant that user access to the
            // business through EnsureCanManageBusinessResourcesAsync.
            CreateMap<UpdateEmployeeDto, Employee>()
                .ForMember(dest => dest.BusinessId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore());
        }
    }
}
