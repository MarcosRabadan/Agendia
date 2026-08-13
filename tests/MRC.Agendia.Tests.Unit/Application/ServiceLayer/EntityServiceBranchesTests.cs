using AutoMapper;
using MRC.Agendia.Application.Business;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Employees;
using MRC.Agendia.Application.Holidays;
using MRC.Agendia.Application.Holidays.DTO;
using MRC.Agendia.Application.Services;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.ServiceLayer
{
    /// <summary>
    /// Branch coverage for the application services beyond the thin handlers: the
    /// not-found paths of Update/Delete throw the typed domain exception, and the
    /// Restore state machine (missing → throws, already active → idempotent no-op,
    /// deleted → un-deletes and saves).
    /// </summary>
    public class EntityServiceBranchesTests
    {
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();

        // ---------- Service ----------

        private ServicesService BuildServices(IServiceRepository repo) => new(repo, _uow, _mapper);

        [Fact]
        public async Task Service_update_missing_throws()
        {
            var repo = Substitute.For<IServiceRepository>();
            repo.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns((Service?)null);

            await Assert.ThrowsAsync<ServiceNotFoundException>(() =>
                BuildServices(repo).UpdateAsync(new UpdateServiceDto(9, "X", null, 30, 10m)));
        }

        [Fact]
        public async Task Service_delete_missing_throws()
        {
            var repo = Substitute.For<IServiceRepository>();
            repo.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns((Service?)null);

            await Assert.ThrowsAsync<ServiceNotFoundException>(() => BuildServices(repo).DeleteAsync(9));
        }

        [Fact]
        public async Task Service_restore_missing_throws()
        {
            var repo = Substitute.For<IServiceRepository>();
            repo.GetByIdIncludingDeletedAsync(9, Arg.Any<CancellationToken>()).Returns((Service?)null);

            await Assert.ThrowsAsync<ServiceNotFoundException>(() => BuildServices(repo).RestoreAsync(9));
        }

        [Fact]
        public async Task Service_restore_when_not_deleted_is_idempotent_noop()
        {
            var repo = Substitute.For<IServiceRepository>();
            repo.GetByIdIncludingDeletedAsync(1, Arg.Any<CancellationToken>())
                .Returns(new Service { Id = 1, IsDeleted = false });

            var result = await BuildServices(repo).RestoreAsync(1);

            Assert.True(result);
            repo.DidNotReceive().Update(Arg.Any<Service>());
            await _uow.DidNotReceive().Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Service_restore_when_deleted_undeletes_and_saves()
        {
            var repo = Substitute.For<IServiceRepository>();
            var entity = new Service { Id = 1, IsDeleted = true, DeletedAt = DateTime.UtcNow };
            repo.GetByIdIncludingDeletedAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

            var result = await BuildServices(repo).RestoreAsync(1);

            Assert.True(result);
            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedAt);
            repo.Received(1).Update(entity);
            await _uow.Received(1).Save(Arg.Any<CancellationToken>());
        }

        // ---------- Other entity services: not-found paths ----------

        [Fact]
        public async Task Business_update_missing_throws()
        {
            var repo = Substitute.For<IBusinessRepository>();
            repo.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns((MRC.Agendia.Domain.Entities.Business?)null);

            await Assert.ThrowsAsync<BusinessNotFoundException>(() =>
                new BusinessService(repo, _uow, _mapper)
                    .UpdateAsync(new UpdateBusinessDto(9, IsActive: true)));
        }

        [Fact]
        public async Task Employee_delete_missing_throws()
        {
            var repo = Substitute.For<IEmployeeRepository>();
            repo.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns((Employee?)null);

            await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
                new EmployeeService(repo, _uow, _mapper).DeleteAsync(9));
        }

        // ---------- Holiday (not soft-deletable): not-found paths ----------

        [Fact]
        public async Task Holiday_update_missing_throws()
        {
            var repo = Substitute.For<IHolidayCalendarRepository>();
            repo.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns((HolidayCalendar?)null);

            await Assert.ThrowsAsync<HolidayNotFoundException>(() =>
                new HolidayService(repo, _uow, _mapper)
                    .UpdateAsync(new UpdateHolidayCalendarDto(9, new DateOnly(2026, 5, 1), "X", HolidayScope.National, 2026)));
        }

        [Fact]
        public async Task Holiday_delete_missing_throws()
        {
            var repo = Substitute.For<IHolidayCalendarRepository>();
            repo.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns((HolidayCalendar?)null);

            await Assert.ThrowsAsync<HolidayNotFoundException>(() => new HolidayService(repo, _uow, _mapper).DeleteAsync(9));
        }
    }
}
