using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MRC.Agendia.Application.Authorization;
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
        private static readonly Guid Business1Id = TestIds.Of(1);
        private static readonly Guid Business2Id = TestIds.Of(2);
        private static readonly Guid EmployeeActiveId = TestIds.Of(10);
        private static readonly Guid EmployeeInactiveId = TestIds.Of(11);
        private static readonly Guid EmployeeOtherBusinessId = TestIds.Of(20);
        private static readonly Guid Service1Id = TestIds.Of(1000);
        private static readonly Guid Appointment1Id = TestIds.Of(10000);
        private static readonly Guid ScheduleTemplate1Id = TestIds.Of(200);
        private static readonly Guid ScheduleOverride1Id = TestIds.Of(300);

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
            Assert.Equal("User not authenticated.", ex.Message);
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
            Assert.Equal("You do not have permission to manage this business.", ex.Message);
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
            Assert.Equal("You do not have permission to manage this business's resources.", ex.Message);
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
                () => sut.EnsureCanViewEmployeeAsync(TestIds.Of(999_999)));
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
            Assert.Equal("You do not have permission to view this employee.", ex.Message);
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
                () => sut.EnsureCanDeleteEmployeeAsync(TestIds.Of(999_999)));
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
            Assert.Equal("Only the business owner (or an admin) can delete employees.", ex.Message);
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
                () => sut.EnsureCanManageAppointmentAsync(TestIds.Of(999_999)));
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

        // #292: an appointment keeps its history when a participant is soft-deleted. Reaching
        // the owning business through the required Employee navigation used to drop the row
        // (the parent's soft-delete filter applied to the INNER JOIN), so a live booking
        // turned into a 404 for the very people entitled to it.

        [Fact]
        public async Task ManageAppointment_ClientOfAppointment_WithDeletedEmployee_Passes()
        {
            var (sut, db) = await BuildAsync(AsUser(ClientUserId).WithRole(Roles.Client));
            await SoftDeleteAppointmentEmployeeAsync(db);

            await sut.EnsureCanManageAppointmentAsync(Appointment1Id);
        }

        [Fact]
        public async Task ManageAppointment_OwnerOfBusiness_WithDeletedEmployee_Passes()
        {
            var (sut, db) = await BuildAsync(AsUser(OwnerUserId));
            await SoftDeleteAppointmentEmployeeAsync(db);

            await sut.EnsureCanManageAppointmentAsync(Appointment1Id);
        }

        // The other half of dropping the filters: the appointment's own soft delete still
        // hides it, so a deleted booking is a 404 and not something its client can act on.
        [Fact]
        public async Task ManageAppointment_SoftDeletedAppointment_ThrowsNotFound()
        {
            var (sut, db) = await BuildAsync(AsUser(ClientUserId).WithRole(Roles.Client));
            var appointment = await db.Appointments.FirstAsync(a => a.Id == Appointment1Id);
            appointment.IsDeleted = true;
            await db.SaveChangesAsync();

            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanManageAppointmentAsync(Appointment1Id));
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
            Assert.Equal("You do not have permission to manage this appointment.", ex.Message);
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
            await sut.EnsureCanCreateAppointmentAsync(ClientUserId, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_EmployeeNotFound_ThrowsKeyNotFound()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await Assert.ThrowsAnyAsync<NotFoundException>(
                () => sut.EnsureCanCreateAppointmentAsync(ClientUserId, TestIds.Of(999_999)));
        }

        [Fact]
        public async Task CreateAppointment_OwnerOfBusinessOfEmployee_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(OwnerUserId));
            await sut.EnsureCanCreateAppointmentAsync(ClientUserId, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_ActiveEmployeeOfBusiness_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(EmployeeUserId));
            await sut.EnsureCanCreateAppointmentAsync(ClientUserId, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_Client_ForSelf_Passes()
        {
            var (sut, _) = await BuildAsync(AsUser(ClientUserId).WithRole(Roles.Client));
            await sut.EnsureCanCreateAppointmentAsync(ClientUserId, EmployeeActiveId);
        }

        [Fact]
        public async Task CreateAppointment_Client_ForOtherClient_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(ClientUserId).WithRole(Roles.Client));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanCreateAppointmentAsync(OtherClientUserId, EmployeeActiveId));
            Assert.Equal("You can only create appointments for your own client account.", ex.Message);
        }

        [Fact]
        public async Task CreateAppointment_Stranger_NotClientRole_Throws()
        {
            var (sut, _) = await BuildAsync(AsUser(StrangerUserId));
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.EnsureCanCreateAppointmentAsync(ClientUserId, EmployeeActiveId));
            Assert.Equal("You do not have permission to create this appointment.", ex.Message);
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
                () => sut.EnsureCanManageServiceAsync(TestIds.Of(999_999)));
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
                () => sut.EnsureCanManageScheduleTemplateAsync(TestIds.Of(999_999)));
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
                () => sut.EnsureCanManageScheduleOverrideAsync(TestIds.Of(999_999)));
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
            // The same scope instance the context filters with, so both agree on what the
            // caller may see. Unrestricted here: the cross-tenant 404 that a restricted scope
            // produces is covered end-to-end in SoftDeleteIntegrationTests, with the real
            // CurrentBusinessScope resolving from a token.
            var businessScope = new UnrestrictedBusinessScope();
            var db = CreateDb(businessScope);
            await SeedDefaultGraphAsync(db);
            var sut = new ResourceAuthorizationService(db, currentUser, businessScope);
            return (sut, db);
        }

        private static AgendiaDbContext CreateDb(ICurrentBusinessScope businessScope)
        {
            var options = new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AgendiaDbContext(options, businessScope);
        }

        /// <summary>Soft-deletes the appointment's employee, as a business dropping a member does.</summary>
        private static async Task SoftDeleteAppointmentEmployeeAsync(AgendiaDbContext db)
        {
            var employee = await db.Employees.FirstAsync(e => e.Id == EmployeeActiveId);
            employee.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        private static async Task SeedDefaultGraphAsync(AgendiaDbContext db)
        {
            var business1 = new Business
            {
                Id = Business1Id,
                IsActive = true,
                OwnerUserId = OwnerUserId,
            };
            var business2 = new Business
            {
                Id = Business2Id,
                IsActive = true,
                OwnerUserId = OtherOwnerUserId,
            };

            var employeeActive = new Employee
            {
                Id = EmployeeActiveId,
                BusinessId = Business1Id,
                UserId = EmployeeUserId,
                IsActive = true,
                MaxConcurrentAppointments = 1,
            };
            var employeeInactive = new Employee
            {
                Id = EmployeeInactiveId,
                BusinessId = Business1Id,
                UserId = InactiveEmployeeUserId,
                IsActive = false,
                MaxConcurrentAppointments = 1,
            };
            var employeeOther = new Employee
            {
                Id = EmployeeOtherBusinessId,
                BusinessId = Business2Id,
                UserId = OtherBusinessEmployeeUserId,
                IsActive = true,
                MaxConcurrentAppointments = 1,
            };

            var service1 = new Service
            {
                Id = Service1Id,
                BusinessId = Business1Id,
                DurationMinutes = 30,
            };

            var appointment1 = new Appointment
            {
                Id = Appointment1Id,
                ClientUserId = ClientUserId,
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
