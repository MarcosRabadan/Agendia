using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Authorization;
using MRC.Agendia.Tests.Unit.TestDoubles;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Authorization
{
    /// <summary>
    /// Tests for <see cref="ResourceAuthorizationService"/>. Every Ensure* method is
    /// exercised against a small seeded graph using the EF Core InMemory provider.
    ///
    /// The default seed sets up two businesses so that "happy path" and "cross-tenant"
    /// scenarios share the same data:
    ///
    ///   Business 1 (owner = "owner-1")
    ///     - Employee "employee-1"    (active)
    ///     - Employee "employee-off"  (inactive)
    ///     - Service / ScheduleTemplate / ScheduleOverride
    ///   Business 2 (owner = "other-owner")
    ///     - Employee "other-employee" (active)
    ///   Clients
    ///     - "client-1"
    ///     - "other-client"
    ///   Appointment 1 (Client=client-1, Employee=employee-1, on Business 1)
    /// </summary>
    public class ResourceAuthorizationServiceTests
    {
        // ----- Seeded ids -----
        private const int Business1Id = 1;
        private const int Business2Id = 2;
        private const int EmployeeActiveId = 10;
        private const int EmployeeInactiveId = 11;
        private const int EmployeeOtherBusinessId = 20;
        private const int Client1Id = 100;
        private const int OtherClientId = 101;
        private const int Service1Id = 1000;
        private const int Appointment1Id = 10000;
        private const int ScheduleTemplate1Id = 200;
        private const int ScheduleOverride1Id = 300;

        // ----- Seeded user ids -----
        private const string OwnerUserId = "owner-1";
        private const string OtherOwnerUserId = "other-owner";
        private const string EmployeeUserId = "employee-1";
        private const string InactiveEmployeeUserId = "employee-off";
        private const string OtherBusinessEmployeeUserId = "other-employee";
        private const string ClientUserId = "client-1";
        private const string OtherClientUserId = "other-client";
        private const string StrangerUserId = "stranger";
        private const string AdminUserId = "admin";

        // ===================================================================
        //  EnsureCanManageBusinessAsync
        // ===================================================================
        #region EnsureCanManageBusinessAsync

        [Fact]
        public async Task ManageBusiness_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanManageBusinessAsync(Business1Id);
        }

        [Fact]
        public async Task ManageBusiness_NotAuthenticated_Throws()
        {
            var (sut, _) = await BuildAsync(NotAuthenticated());
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessAsync(Business1Id));
            Assert.Equal("Usuario no autenticado.", ex.Message);
        }

        [Fact]
        public async Task ManageBusiness_Owner_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanManageBusinessAsync(Business1Id);
        }

        [Fact]
        public async Task ManageBusiness_DifferentOwner_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(OtherOwnerUserId));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessAsync(Business1Id));
            Assert.Equal("No tienes permiso para gestionar este negocio.", ex.Message);
        }

        [Fact]
        public async Task ManageBusiness_EmployeeOfBusiness_Throws()
        {
            // Being employee of the business is NOT enough to "manage" the business.
            // Only the owner (or admin) can.
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessAsync(Business1Id));
        }

        [Fact]
        public async Task ManageBusiness_Stranger_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessAsync(Business1Id));
        }

        #endregion

        // ===================================================================
        //  EnsureCanManageBusinessResourcesAsync
        // ===================================================================
        #region EnsureCanManageBusinessResourcesAsync

        [Fact]
        public async Task ManageBusinessResources_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanManageBusinessResourcesAsync(Business1Id);
        }

        [Fact]
        public async Task ManageBusinessResources_NotAuthenticated_Throws()
        {
            var (sut, _) = await BuildAsync(NotAuthenticated());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessResourcesAsync(Business1Id));
        }

        [Fact]
        public async Task ManageBusinessResources_Owner_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanManageBusinessResourcesAsync(Business1Id);
        }

        [Fact]
        public async Task ManageBusinessResources_ActiveEmployee_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            await sut.EnsureCanManageBusinessResourcesAsync(Business1Id);
        }

        [Fact]
        public async Task ManageBusinessResources_InactiveEmployee_Throws()
        {
            // An inactive employee should not retain access.
            var (sut, _) = await BuildAsync(AsUser(InactiveEmployeeUserId));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessResourcesAsync(Business1Id));
            Assert.Equal("No tienes permiso para gestionar recursos de este negocio.", ex.Message);
        }

        [Fact]
        public async Task ManageBusinessResources_OwnerOfDifferentBusiness_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(OtherOwnerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessResourcesAsync(Business1Id));
        }

        [Fact]
        public async Task ManageBusinessResources_Stranger_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageBusinessResourcesAsync(Business1Id));
        }

        #endregion

        // ===================================================================
        //  EnsureCanViewEmployeeAsync
        // ===================================================================
        #region EnsureCanViewEmployeeAsync

        [Fact]
        public async Task ViewEmployee_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanViewEmployeeAsync(EmployeeActiveId);
        }

        [Fact]
        public async Task ViewEmployee_NotFound_ThrowsKeyNotFound()
        {
            // Admin would normally short-circuit, but only when the resource exists is the
            // KeyNotFoundException meaningful, so we use a non-admin to hit the lookup path.
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanViewEmployeeAsync(999_999));
        }

        [Fact]
        public async Task ViewEmployee_SelfEmployee_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            await sut.EnsureCanViewEmployeeAsync(EmployeeActiveId);
        }

        [Fact]
        public async Task ViewEmployee_OwnerOfTheBusiness_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanViewEmployeeAsync(EmployeeActiveId);
        }

        [Fact]
        public async Task ViewEmployee_OwnerOfDifferentBusiness_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(OtherOwnerUserId));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanViewEmployeeAsync(EmployeeActiveId));
            Assert.Equal("No tienes permiso para ver este empleado.", ex.Message);
        }

        [Fact]
        public async Task ViewEmployee_Stranger_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanViewEmployeeAsync(EmployeeActiveId));
        }

        #endregion

        // ===================================================================
        //  EnsureCanUpdateEmployeeAsync (smoke - delegates to View)
        // ===================================================================
        #region EnsureCanUpdateEmployeeAsync

        [Fact]
        public async Task UpdateEmployee_SelfEmployee_Passes_DelegatesToView()
        {
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            await sut.EnsureCanUpdateEmployeeAsync(EmployeeActiveId);
        }

        [Fact]
        public async Task UpdateEmployee_Stranger_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanUpdateEmployeeAsync(EmployeeActiveId));
        }

        #endregion

        // ===================================================================
        //  EnsureCanDeleteEmployeeAsync
        // ===================================================================
        #region EnsureCanDeleteEmployeeAsync

        [Fact]
        public async Task DeleteEmployee_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanDeleteEmployeeAsync(EmployeeActiveId);
        }

        [Fact]
        public async Task DeleteEmployee_NotFound_ThrowsKeyNotFound()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanDeleteEmployeeAsync(999_999));
        }

        [Fact]
        public async Task DeleteEmployee_Owner_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanDeleteEmployeeAsync(EmployeeActiveId);
        }

        [Fact]
        public async Task DeleteEmployee_SelfEmployee_Throws()
        {
            // Important rule: an employee CANNOT delete itself. Only the owner (or admin) can.
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanDeleteEmployeeAsync(EmployeeActiveId));
            Assert.Equal("Solo el dueno del negocio (o un admin) puede eliminar empleados.", ex.Message);
        }

        [Fact]
        public async Task DeleteEmployee_Stranger_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanDeleteEmployeeAsync(EmployeeActiveId));
        }

        #endregion

        // ===================================================================
        //  EnsureCanManageClientAsync
        // ===================================================================
        #region EnsureCanManageClientAsync

        [Fact]
        public async Task ManageClient_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanManageClientAsync(Client1Id);
        }

        [Fact]
        public async Task ManageClient_NotAuthenticated_Throws()
        {
            var (sut, _) = await BuildAsync(NotAuthenticated());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageClientAsync(Client1Id));
        }

        [Fact]
        public async Task ManageClient_SelfClient_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(ClientUserId));
            await sut.EnsureCanManageClientAsync(Client1Id);
        }

        [Fact]
        public async Task ManageClient_DifferentClient_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(OtherClientUserId));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageClientAsync(Client1Id));
            Assert.Equal("Solo puedes gestionar tu propia cuenta de cliente.", ex.Message);
        }

        #endregion

        // ===================================================================
        //  EnsureCanManageAppointmentAsync
        // ===================================================================
        #region EnsureCanManageAppointmentAsync

        [Fact]
        public async Task ManageAppointment_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanManageAppointmentAsync(Appointment1Id);
        }

        [Fact]
        public async Task ManageAppointment_NotFound_ThrowsKeyNotFound()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanManageAppointmentAsync(999_999));
        }

        [Fact]
        public async Task ManageAppointment_OwnerOfBusiness_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanManageAppointmentAsync(Appointment1Id);
        }

        [Fact]
        public async Task ManageAppointment_ActiveEmployeeOfBusiness_Passes()
        {
            // Any active employee of the business can manage the appointment,
            // not only the one assigned to it.
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            await sut.EnsureCanManageAppointmentAsync(Appointment1Id);
        }

        [Fact]
        public async Task ManageAppointment_ClientOfAppointment_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(ClientUserId).WithRole(Roles.Client));
            await sut.EnsureCanManageAppointmentAsync(Appointment1Id);
        }

        [Fact]
        public async Task ManageAppointment_OwnerOfDifferentBusiness_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(OtherOwnerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageAppointmentAsync(Appointment1Id));
        }

        [Fact]
        public async Task ManageAppointment_OtherClient_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(OtherClientUserId).WithRole(Roles.Client));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageAppointmentAsync(Appointment1Id));
            Assert.Equal("No tienes permiso para gestionar esta cita.", ex.Message);
        }

        [Fact]
        public async Task ManageAppointment_Stranger_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanManageAppointmentAsync(Appointment1Id));
        }

        #endregion

        // ===================================================================
        //  EnsureCanCreateAppointmentAsync
        // ===================================================================
        #region EnsureCanCreateAppointmentAsync

        [Fact]
        public async Task CreateAppointment_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanCreateAppointmentAsync(Client1Id, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_EmployeeNotFound_ThrowsKeyNotFound()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanCreateAppointmentAsync(Client1Id, 999_999));
        }

        [Fact]
        public async Task CreateAppointment_OwnerOfBusinessOfEmployee_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanCreateAppointmentAsync(Client1Id, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_ActiveEmployeeOfBusiness_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            await sut.EnsureCanCreateAppointmentAsync(Client1Id, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_Client_ForSelf_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(ClientUserId).WithRole(Roles.Client));
            await sut.EnsureCanCreateAppointmentAsync(Client1Id, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_Client_ForOtherClient_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(ClientUserId).WithRole(Roles.Client));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanCreateAppointmentAsync(OtherClientId, EmployeeActiveId));
            Assert.Equal("Solo puedes crear citas para tu propia cuenta de cliente.", ex.Message);
        }

        [Fact]
        public async Task CreateAppointment_Stranger_NotClientRole_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanCreateAppointmentAsync(Client1Id, EmployeeActiveId));
            Assert.Equal("No tienes permiso para crear esta cita.", ex.Message);
        }

        #endregion

        // ===================================================================
        //  Business-scoped resources (smoke tests for delegation)
        // ===================================================================
        #region EnsureCanManageServiceAsync

        [Fact]
        public async Task ManageService_NotFound_ThrowsKeyNotFound()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanManageServiceAsync(999_999));
        }

        [Fact]
        public async Task ManageService_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanManageServiceAsync(Service1Id);
        }

        [Fact]
        public async Task ManageService_OwnerOfBusiness_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanManageServiceAsync(Service1Id);
        }

        #endregion

        #region EnsureCanManageScheduleTemplateAsync

        [Fact]
        public async Task ManageScheduleTemplate_NotFound_ThrowsKeyNotFound()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanManageScheduleTemplateAsync(999_999));
        }

        [Fact]
        public async Task ManageScheduleTemplate_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanManageScheduleTemplateAsync(ScheduleTemplate1Id);
        }

        [Fact]
        public async Task ManageScheduleTemplate_OwnerOfBusiness_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanManageScheduleTemplateAsync(ScheduleTemplate1Id);
        }

        #endregion

        #region EnsureCanManageScheduleOverrideAsync

        [Fact]
        public async Task ManageScheduleOverride_NotFound_ThrowsKeyNotFound()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanManageScheduleOverrideAsync(999_999));
        }

        [Fact]
        public async Task ManageScheduleOverride_Admin_Passes()
        {
            var (sut, _) = await BuildAsync(AsAdmin());
            await sut.EnsureCanManageScheduleOverrideAsync(ScheduleOverride1Id);
        }

        [Fact]
        public async Task ManageScheduleOverride_OwnerOfBusiness_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanManageScheduleOverrideAsync(ScheduleOverride1Id);
        }

        #endregion

        // ===================================================================
        //  Helpers
        // ===================================================================

        private static async Task<(ResourceAuthorizationService sut, AgendiaDbContext db)> BuildAsync(FakeCurrentUserContext currentUser)
        {
            var db = CreateDb();
            await SeedDefaultGraphAsync(db);
            var sut = new ResourceAuthorizationService(db, currentUser);
            return (sut, db);
        }

        private static AgendiaDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AgendiaDbContext(options);
        }

        private static async Task SeedDefaultGraphAsync(AgendiaDbContext db)
        {
            var business1 = new Business
            {
                Id = Business1Id,
                Name = "Business 1",
                Address = "Calle 1",
                Phone = "111",
                Email = "b1@test.local",
                IsActive = true,
                OwnerUserId = OwnerUserId,
            };
            var business2 = new Business
            {
                Id = Business2Id,
                Name = "Business 2",
                Address = "Calle 2",
                Phone = "222",
                Email = "b2@test.local",
                IsActive = true,
                OwnerUserId = OtherOwnerUserId,
            };

            var employeeActive = new Employee
            {
                Id = EmployeeActiveId,
                BusinessId = Business1Id,
                FullName = "Active Employee",
                UserId = EmployeeUserId,
                IsActive = true,
                MaxConcurrentAppointments = 1,
            };
            var employeeInactive = new Employee
            {
                Id = EmployeeInactiveId,
                BusinessId = Business1Id,
                FullName = "Inactive Employee",
                UserId = InactiveEmployeeUserId,
                IsActive = false,
                MaxConcurrentAppointments = 1,
            };
            var employeeOther = new Employee
            {
                Id = EmployeeOtherBusinessId,
                BusinessId = Business2Id,
                FullName = "Other Business Employee",
                UserId = OtherBusinessEmployeeUserId,
                IsActive = true,
                MaxConcurrentAppointments = 1,
            };

            var client1 = new Client
            {
                Id = Client1Id,
                Name = "Client One",
                Phone = "100",
                UserId = ClientUserId,
            };
            var client2 = new Client
            {
                Id = OtherClientId,
                Name = "Client Other",
                Phone = "101",
                UserId = OtherClientUserId,
            };

            var service1 = new Service
            {
                Id = Service1Id,
                BusinessId = Business1Id,
                Name = "Corte",
                Price = 20m,
                DurationMinutes = 30,
            };

            var appointment1 = new Appointment
            {
                Id = Appointment1Id,
                ClientId = Client1Id,
                EmployeeId = EmployeeActiveId,
                ServiceId = Service1Id,
                StartDate = new DateTime(2026, 5, 18, 10, 0, 0),
                EndDate = new DateTime(2026, 5, 18, 10, 30, 0),
                Status = AppointmentStatus.Pending,
            };

            var scheduleTemplate1 = new ScheduleTemplate
            {
                Id = ScheduleTemplate1Id,
                BusinessId = Business1Id,
                Name = "Default",
                EffectiveFrom = new DateOnly(2026, 1, 1),
                EffectiveTo = new DateOnly(2026, 12, 31),
                IsDefault = true,
            };

            var scheduleOverride1 = new ScheduleOverride
            {
                Id = ScheduleOverride1Id,
                BusinessId = Business1Id,
                Date = new DateOnly(2026, 12, 25),
                OverrideType = ScheduleOverrideType.NationalHoliday,
                Reason = "Navidad",
            };

            await db.Businesses.AddRangeAsync(business1, business2);
            await db.Employees.AddRangeAsync(employeeActive, employeeInactive, employeeOther);
            await db.Clients.AddRangeAsync(client1, client2);
            await db.Services.AddAsync(service1);
            await db.Appointments.AddAsync(appointment1);
            await db.ScheduleTemplates.AddAsync(scheduleTemplate1);
            await db.ScheduleOverrides.AddAsync(scheduleOverride1);
            await db.SaveChangesAsync();
        }

        // ----- FakeCurrentUserContext factories -----

        private static FakeCurrentUserContext AsAdmin() => new FakeCurrentUserContext
        {
            UserId = AdminUserId,
            IsAuthenticated = true,
        }.WithRole(Roles.Admin);

        private static FakeCurrentUserContext AsUser(string userId) => new FakeCurrentUserContext
        {
            UserId = userId,
            IsAuthenticated = true,
        };

        private static FakeCurrentUserContext NotAuthenticated() => new FakeCurrentUserContext
        {
            UserId = null,
            IsAuthenticated = false,
        };
    }
}
