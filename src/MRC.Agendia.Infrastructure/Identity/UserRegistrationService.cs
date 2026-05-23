using Microsoft.AspNetCore.Identity;
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

        public UserRegistrationService(
            UserManager<ApplicationUser> userManager,
            IClientRepository clientRepository,
            IBusinessRepository businessRepository,
            IEmployeeRepository employeeRepository,
            IUnitOfWork unitOfWork,
            IAuthEmailService authEmailService,
            IAuthResponseFactory authResponseFactory)
        {
            _userManager = userManager;
            _clientRepository = clientRepository;
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _unitOfWork = unitOfWork;
            _authEmailService = authEmailService;
            _authResponseFactory = authResponseFactory;
        }

        public async Task<AuthResponseDto> RegisterClientAsync(RegisterClientDto dto)
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

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Client);

            // Create the associated Client entity.
            var client = new Client
            {
                Name = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                UserId = user.Id
            };
            await _clientRepository.AddAsync(client);
            await _unitOfWork.Save();

            await _authEmailService.SendEmailConfirmationAsync(user);
            return await _authResponseFactory.CreateAsync(user);
        }

        public async Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto)
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

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

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
            await _businessRepository.AddAsync(business);
            await _unitOfWork.Save();

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
            await _employeeRepository.AddAsync(ownerEmployee);
            await _unitOfWork.Save();

            await _authEmailService.SendEmailConfirmationAsync(user);
            return await _authResponseFactory.CreateAsync(user);
        }

        public async Task<UserDto> RegisterEmployeeAsync(RegisterEmployeeDto dto, string currentOwnerUserId)
        {
            // Validate the business exists and the caller owns it.
            var business = await _businessRepository.GetByIdAsync(dto.BusinessId)
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

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

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
            await _employeeRepository.AddAsync(employee);
            await _unitOfWork.Save();

            await _authEmailService.SendEmailConfirmationAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
        }
    }
}
