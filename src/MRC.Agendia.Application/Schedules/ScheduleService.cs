using AutoMapper;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Schedules
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleTemplateRepository _templateRepository;
        private readonly IScheduleOverrideRepository _overrideRepository;
        private readonly IHolidayCalendarRepository _holidayRepository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditLogger _auditLogger;

        public ScheduleService(
            IScheduleTemplateRepository templateRepository,
            IScheduleOverrideRepository overrideRepository,
            IHolidayCalendarRepository holidayRepository,
            IScheduleResolver scheduleResolver,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAuditLogger auditLogger)
        {
            _templateRepository = templateRepository;
            _overrideRepository = overrideRepository;
            _holidayRepository = holidayRepository;
            _scheduleResolver = scheduleResolver;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auditLogger = auditLogger;
        }

        #region Templates
        public async Task<IEnumerable<ScheduleTemplateDto>> GetTemplatesByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
        {
            var entities = await _templateRepository.GetByBusinessIdAsync(businessId, cancellationToken);
            return _mapper.Map<IEnumerable<ScheduleTemplateDto>>(entities);
        }

        public async Task<ScheduleTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _templateRepository.GetByIdWithSlotsAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<ScheduleTemplateDto>(entity);
        }

        public async Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto, CancellationToken cancellationToken = default)
        {
            if (await _templateRepository.HasOverlappingTemplateAsync(dto.BusinessId, dto.EffectiveFrom, dto.EffectiveTo, cancellationToken: cancellationToken))
                throw new TemplatesOverlapException("Ya existe una plantilla de horario que se solapa con las fechas indicadas.");

            var entity = _mapper.Map<ScheduleTemplate>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            await _templateRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);

            await _auditLogger.LogAsync(AuditActions.ScheduleTemplateCreated, "ScheduleTemplate", entity.Id.ToString(), new { entity.BusinessId }, cancellationToken);
            return _mapper.Map<ScheduleTemplateDto>(entity);
        }

        public async Task<ScheduleTemplateDto> UpdateTemplateAsync(UpdateScheduleTemplateDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _templateRepository.GetByIdWithSlotsAsync(dto.Id, cancellationToken)
                ?? throw new ScheduleTemplateNotFoundException(dto.Id);

            if (await _templateRepository.HasOverlappingTemplateAsync(entity.BusinessId, dto.EffectiveFrom, dto.EffectiveTo, dto.Id, cancellationToken))
                throw new TemplatesOverlapException("Ya existe una plantilla de horario que se solapa con las fechas indicadas.");

            entity.Name = dto.Name;
            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;
            entity.IsDefault = dto.IsDefault;

            entity.WeeklySlots.Clear();
            foreach (var slotDto in dto.WeeklySlots)
            {
                entity.WeeklySlots.Add(_mapper.Map<WeeklyTimeSlot>(slotDto));
            }

            _templateRepository.Update(entity);
            await _unitOfWork.Save(cancellationToken);

            await _auditLogger.LogAsync(AuditActions.ScheduleTemplateUpdated, "ScheduleTemplate", entity.Id.ToString(), new { entity.BusinessId }, cancellationToken);
            return _mapper.Map<ScheduleTemplateDto>(entity);
        }

        public async Task<bool> DeleteTemplateAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _templateRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new ScheduleTemplateNotFoundException(id);

            _templateRepository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);

            await _auditLogger.LogAsync(AuditActions.ScheduleTemplateDeleted, "ScheduleTemplate", id.ToString(), cancellationToken: cancellationToken);
            return true;
        }
        #endregion

        #region Overrides
        public async Task<IEnumerable<ScheduleOverrideDto>> GetOverridesByBusinessIdAsync(int businessId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
        {
            IEnumerable<ScheduleOverride> entities;

            if (from.HasValue && to.HasValue)
                entities = await _overrideRepository.GetByBusinessIdAndDateRangeAsync(businessId, from.Value, to.Value, cancellationToken);
            else
                entities = await _overrideRepository.GetByBusinessIdAsync(businessId, cancellationToken);

            return _mapper.Map<IEnumerable<ScheduleOverrideDto>>(entities);
        }

        public async Task<ScheduleOverrideDto?> GetOverrideByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _overrideRepository.GetByIdWithSlotsAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<ScheduleOverrideDto>(entity);
        }

        public async Task<ScheduleOverrideDto> CreateOverrideAsync(CreateScheduleOverrideDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<ScheduleOverride>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            await _overrideRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);

            await _auditLogger.LogAsync(AuditActions.ScheduleOverrideCreated, "ScheduleOverride", entity.Id.ToString(), new { entity.BusinessId }, cancellationToken);
            return _mapper.Map<ScheduleOverrideDto>(entity);
        }

        public async Task<ScheduleOverrideDto> UpdateOverrideAsync(UpdateScheduleOverrideDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _overrideRepository.GetByIdWithSlotsAsync(dto.Id, cancellationToken)
                ?? throw new ScheduleOverrideNotFoundException(dto.Id);

            entity.Date = dto.Date;
            entity.OverrideType = dto.OverrideType;
            entity.Reason = dto.Reason;

            entity.CustomSlots.Clear();
            if (dto.CustomSlots != null)
            {
                foreach (var slotDto in dto.CustomSlots)
                {
                    entity.CustomSlots.Add(_mapper.Map<CustomTimeSlot>(slotDto));
                }
            }

            _overrideRepository.Update(entity);
            await _unitOfWork.Save(cancellationToken);

            await _auditLogger.LogAsync(AuditActions.ScheduleOverrideUpdated, "ScheduleOverride", entity.Id.ToString(), new { entity.BusinessId }, cancellationToken);
            return _mapper.Map<ScheduleOverrideDto>(entity);
        }

        public async Task<bool> DeleteOverrideAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _overrideRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new ScheduleOverrideNotFoundException(id);

            _overrideRepository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);

            await _auditLogger.LogAsync(AuditActions.ScheduleOverrideDeleted, "ScheduleOverride", id.ToString(), cancellationToken: cancellationToken);
            return true;
        }
        #endregion

        #region Effective Schedule
        public async Task<EffectiveScheduleDto> GetEffectiveScheduleAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default)
        {
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(businessId, date, cancellationToken);
            var templateEntities = await _templateRepository.GetByBusinessIdAsync(businessId, cancellationToken);
            var templates = _mapper.Map<List<ScheduleTemplateDto>>(templateEntities);
            var activeTemplate = templates.FirstOrDefault(t => t.EffectiveFrom <= date && t.EffectiveTo >= date);

            return new EffectiveScheduleDto(
                Date: effective.Date,
                IsOpen: effective.IsOpen,
                ClosedReason: effective.ClosedReason,
                OverrideType: effective.OverrideType,
                TimeSlots: effective.TimeSlots
                    .Select(ts => new EffectiveTimeSlotDto(ts.StartTime, ts.EndTime))
                    .ToList(),
                ActiveTemplate: activeTemplate,
                Templates: templates);
        }

        public async Task<IEnumerable<CalendarDayDto>> GetCalendarAsync(int businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        {
            var days = await _scheduleResolver.GetEffectiveSchedulesAsync(businessId, from, to, cancellationToken);
            return days.Select(CalendarDayDto.FromEffective).ToList();
        }
        #endregion
    }
}
