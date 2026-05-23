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
            CreateMap<UpdateEmployeeDto, Employee>()
                .ForMember(dest => dest.BusinessId, opt => opt.Ignore());
        }
    }
}
