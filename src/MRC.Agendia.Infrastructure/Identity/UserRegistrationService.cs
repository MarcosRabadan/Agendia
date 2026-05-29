using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Auth;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Identity
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClientRepository _clientRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthEmailService _authEmailService;
        private readonly IAuthResponseFactory _authResponseFactory;
        private readonly IAuditLogger _auditLogger;
        private readonly IConfiguration _configuration;
        private readonly AgendiaDbContext _dbContext;

        public UserRegistrationService(
            UserManager<ApplicationUser> userManager,
            IClientRepository clientRepository,
            IBusinessRepository businessRepository,
            IEmployeeRepository employeeRepository,
            IUnitOfWork unitOfWork,
            IAuthEmailService authEmailService,
            IAuthResponseFactory authResponseFactory,
            IAuditLogger auditLogger,
            IConfiguration configuration,
            AgendiaDbContext dbContext)
        {
            _userManager = userManager;
            _clientRepository = clientRepository;
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _unitOfWork = unitOfWork;
            _authEmailService = authEmailService;
            _authResponseFactory = authResponseFactory;
            _auditLogger = auditLogger;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<AuthResponseDto> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new DuplicateEmailException();

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.Phone,
                IsActive = true
            };

            AuthResponseDto response = null!;

            // Atomic: the user, role, Client entity and session token commit together
            // or not at all, so a failure mid-flow cannot leave an orphaned account
            // (the email taken but no Client row). Best-effort audit/email run after.
            await RunInTransactionAsync(async () =>
            {
                await CreateUserAsync(user, dto.Password);
                await _userManager.AddToRoleAsync(user, Roles.Client);

                var client = new Client
                {
                    Name = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    UserId = user.Id
                };
                await _clientRepository.AddAsync(client, cancellationToken);
                await _unitOfWork.Save(cancellationToken);

                response = await BuildSessionAsync(user, cancellationToken);
            }, cancellationToken);

            await _auditLogger.LogAsync(AuditActions.UserCreated, "User", user.Id, new { user.Email, role = Roles.Client }, cancellationToken);
            await _authEmailService.SendEmailConfirmationAsync(user, cancellationToken);

            return response;
        }

        public async Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new DuplicateEmailException();

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.Phone,
                IsActive = true
            };

            AuthResponseDto response = null!;

            await RunInTransactionAsync(async () =>
            {
                await CreateUserAsync(user, dto.Password);
                await _userManager.AddToRoleAsync(user, Roles.BusinessOwner);

                // Create the associated Business.
                var business = new Business
                {
                    Name = dto.BusinessName,
                    Description = dto.BusinessDescription,
                    Address = dto.BusinessAddress,
                    Phone = dto.BusinessPhone,
                    Email = dto.BusinessEmail,
                    IsActive = true,
                    OwnerUserId = user.Id
                };
                await _businessRepository.AddAsync(business, cancellationToken);
                await _unitOfWork.Save(cancellationToken);

                // Also auto-create an Employee record for the owner so a solo
                // professional can start taking bookings immediately without an
                // extra setup step. MaxConcurrentAppointments defaults to 1; the
                // owner can change it from /api/employee.
                var ownerEmployee = new Employee
                {
                    BusinessId = business.Id,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    IsActive = true,
                    UserId = user.Id,
                    MaxConcurrentAppointments = 1
                };
                await _employeeRepository.AddAsync(ownerEmployee, cancellationToken);
                await _unitOfWork.Save(cancellationToken);

                response = await BuildSessionAsync(user, cancellationToken);
            }, cancellationToken);

            await _auditLogger.LogAsync(AuditActions.UserCreated, "User", user.Id, new { user.Email, role = Roles.BusinessOwner }, cancellationToken);
            await _authEmailService.SendEmailConfirmationAsync(user, cancellationToken);

            return response;
        }

        public async Task<UserDto> RegisterEmployeeAsync(RegisterEmployeeDto dto, string currentOwnerUserId, CancellationToken cancellationToken = default)
        {
            // Validate the business exists and the caller owns it (before creating anything).
            var business = await _businessRepository.GetByIdAsync(dto.BusinessId, cancellationToken)
                ?? throw new BusinessNotFoundException(dto.BusinessId);

            if (business.OwnerUserId != currentOwnerUserId)
                throw new UnauthorizedAccessException("Solo el dueno del negocio puede crear empleados.");

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new DuplicateEmailException();

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.Phone,
                IsActive = true
            };

            await RunInTransactionAsync(async () =>
            {
                await CreateUserAsync(user, dto.Password);
                await _userManager.AddToRoleAsync(user, Roles.Employee);

                var employee = new Employee
                {
                    BusinessId = dto.BusinessId,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    IsActive = true,
                    UserId = user.Id
                };
                await _employeeRepository.AddAsync(employee, cancellationToken);
                await _unitOfWork.Save(cancellationToken);
            }, cancellationToken);

            await _auditLogger.LogAsync(AuditActions.UserCreated, "User", user.Id, new { user.Email, role = Roles.Employee }, cancellationToken);
            await _authEmailService.SendEmailConfirmationAsync(user, cancellationToken);

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
        }

        private async Task CreateUserAsync(ApplicationUser user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // When email confirmation is required, do not auto-login: the login gate
        // blocks unconfirmed users, so issuing a session here would bypass it. The
        // account exists; the user confirms then logs in.
        private async Task<AuthResponseDto> BuildSessionAsync(ApplicationUser user, CancellationToken cancellationToken)
            => _configuration.GetValue<bool>("Auth:RequireConfirmedEmail")
                ? await _authResponseFactory.CreateWithoutSessionAsync(user)
                : await _authResponseFactory.CreateAsync(user, cancellationToken: cancellationToken);

        /// <summary>
        /// Runs the account-creation critical section atomically. On a relational
        /// provider it wraps the steps (user + role + domain entity + session token)
        /// in a single transaction so any failure rolls them all back, preventing
        /// orphaned ApplicationUsers (UserManager auto-saves immediately, so without
        /// this a later failure would leave a committed user with no domain entity).
        /// EF InMemory (tests) does not support transactions, so it runs directly
        /// there - the same provider-guard approach BookingConcurrencyGuard uses
        /// (it guards on IsSqlServer; here IsRelational covers any relational provider).
        /// </summary>
        private async Task RunInTransactionAsync(Func<Task> work, CancellationToken cancellationToken)
        {
            if (!_dbContext.Database.IsRelational())
            {
                await work();
                return;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await work();
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
