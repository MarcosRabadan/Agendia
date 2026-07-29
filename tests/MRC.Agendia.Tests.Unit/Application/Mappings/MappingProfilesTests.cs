using AutoMapper;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Application.Mappings;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Tests.Unit.Application.Mappings
{
    /// <summary>
    /// Locks the cross-tenant mapping invariants: an Update DTO must never be able to
    /// repoint an entity to another business or user. This class of bug has regressed
    /// twice (#91 BusinessId, #125 Employee) and today is prevented only by the Update
    /// DTOs omitting the tenant fields plus a couple of explicit <c>Ignore()</c> calls.
    /// These tests fail the moment a tenant-owning field becomes mappable from an
    /// Update DTO (e.g. someone adds BusinessId to UpdateServiceDto).
    /// </summary>
    public class MappingProfilesTests
    {
        private readonly IMapper _mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<BusinessProfile>();
            cfg.AddProfile<ServiceProfile>();
            cfg.AddProfile<EmployeeProfile>();
            cfg.AddProfile<ClientProfile>();
        }).CreateMapper();

        [Fact]
        public void UpdateBusiness_preserves_OwnerUserId()
        {
            var entity = new MRC.Agendia.Domain.Entities.Business { Id = 1, OwnerUserId = "owner-1", Name = "old" };

            _mapper.Map(new UpdateBusinessDto(1, "new", null, "Addr", "600", "a@b.c", true), entity);

            Assert.Equal("owner-1", entity.OwnerUserId);
            Assert.Equal("new", entity.Name);
        }

        [Fact]
        public void UpdateService_preserves_BusinessId()
        {
            var entity = new Service { Id = 1, BusinessId = 7, Name = "old", DurationMinutes = 30, Price = 10 };

            _mapper.Map(new UpdateServiceDto(1, "new", null, 45, 20m), entity);

            Assert.Equal(7, entity.BusinessId);
            Assert.Equal("new", entity.Name);
            Assert.Equal(45, entity.DurationMinutes);
        }

        [Fact]
        public void UpdateEmployee_preserves_BusinessId_and_UserId()
        {
            var entity = new Employee { Id = 1, BusinessId = 7, UserId = "user-1", FullName = "old" };

            _mapper.Map(new UpdateEmployeeDto(1, "new", null, null, true, 3), entity);

            Assert.Equal(7, entity.BusinessId);
            Assert.Equal("user-1", entity.UserId);
            Assert.Equal("new", entity.FullName);
            Assert.Equal(3, entity.MaxConcurrentAppointments);
        }

        [Fact]
        public void UpdateClient_preserves_BusinessId_and_UserId()
        {
            var entity = new Client { Id = 1, BusinessId = 7, UserId = "user-1", Name = "old" };

            _mapper.Map(new UpdateClientDto(1, "new", "600", "a@b.c"), entity);

            Assert.Equal(7, entity.BusinessId);
            Assert.Equal("user-1", entity.UserId);
            Assert.Equal("new", entity.Name);
        }

        [Fact]
        public void CreateBusiness_and_CreateEmployee_default_IsActive_to_true()
        {
            var business = _mapper.Map<MRC.Agendia.Domain.Entities.Business>(
                new CreateBusinessDto("N", null, "Addr", "600", "a@b.c", "owner-1"));
            var employee = _mapper.Map<Employee>(
                new CreateEmployeeDto(BusinessId: 3, FullName: "E", Email: null, Phone: null));

            Assert.True(business.IsActive);
            Assert.True(employee.IsActive);
        }
    }
}
