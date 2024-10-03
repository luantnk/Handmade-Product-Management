﻿using AutoMapper;
using FluentValidation;
using HandmadeProductManagement.Contract.Repositories.Entity;
using HandmadeProductManagement.Contract.Repositories.Interface;
using HandmadeProductManagement.Contract.Services.Interface;
using HandmadeProductManagement.Core.Base;
using HandmadeProductManagement.ModelViews.StatusChangeModelViews;
using Microsoft.EntityFrameworkCore;

namespace HandmadeProductManagement.Services.Service
{
    public class StatusChangeService : IStatusChangeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<StatusChangeForCreationDto> _creationValidator;
        private readonly IValidator<StatusChangeForUpdateDto> _updateValidator;

        public StatusChangeService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<StatusChangeForCreationDto> creationValidator, IValidator<StatusChangeForUpdateDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _creationValidator = creationValidator;
            _updateValidator = updateValidator;
        }

        // Get status changes by page (only active records)
        public async Task<IList<StatusChangeResponseModel>> GetByPage(int page, int pageSize)
        {
            if (page <= 0)
            {
                throw new BaseException.BadRequestException("invalid_input", "Page must be greater than 0.");
            }

            if (pageSize <= 0)
            {
                throw new BaseException.BadRequestException("invalid_input", "Page size must be greater than 0.");
            }

            IQueryable<StatusChange> query = _unitOfWork.GetRepository<StatusChange>().Entities
                .Where(cr => !cr.DeletedTime.HasValue || cr.DeletedBy == null);

            var statusChanges = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(statusChange => new StatusChangeResponseModel
                {
                    Id = statusChange.Id.ToString(),
                    OrderId = statusChange.OrderId,
                    Status = statusChange.Status,
                    ChangeTime = statusChange.ChangeTime
                })
                .ToListAsync();

            var statusChangeDto = _mapper.Map<IList<StatusChangeResponseModel>>(statusChanges);
            return statusChangeDto;
        }

        // Get status changes by OrderId (only active records)
        public async Task<IList<StatusChangeResponseModel>> GetByOrderId(string orderId)
        {
            IQueryable<StatusChange> query = _unitOfWork.GetRepository<StatusChange>().Entities
                .Where(sc => sc.OrderId == orderId && (!sc.DeletedTime.HasValue || sc.DeletedBy == null));

            var statusChanges = await query.ToListAsync();

            if (!statusChanges.Any())
            {
                throw new BaseException.NotFoundException("not_found", "No status changes found for the given OrderId.");
            }

            var statusChangeDtos = _mapper.Map<IList<StatusChangeResponseModel>>(statusChanges);

            return statusChangeDtos;
        }

        // Create a new status change
        public async Task<bool> Create(StatusChangeForCreationDto createStatusChange)
        {
            // Validate
            var result = _creationValidator.ValidateAsync(createStatusChange);

            if (!result.Result.IsValid)
            {
                throw new ValidationException(result.Result.Errors);
            }

            // Validate ChangeTime
            await ValidateChangeTime(createStatusChange.OrderId, createStatusChange.Status, createStatusChange.ChangeTime);

            var statusChangeEntity = _mapper.Map<StatusChange>(createStatusChange);

            // Set metadata
            statusChangeEntity.CreatedBy = "currentUser"; // Update with actual user info
            statusChangeEntity.LastUpdatedBy = "currentUser"; // Update with actual user info

            await _unitOfWork.GetRepository<StatusChange>().InsertAsync(statusChangeEntity);
            await _unitOfWork.SaveAsync();

            return true;
        }

        // Update an existing status change
        public async Task<bool> Update(string id, StatusChangeForUpdateDto updatedStatusChange)
        {
            var result = _updateValidator.ValidateAsync(updatedStatusChange);

            if (!result.Result.IsValid)
            {
                throw new ValidationException(result.Result.Errors);
            }
            var statusChangeEntity = await _unitOfWork.GetRepository<StatusChange>().Entities
                .FirstOrDefaultAsync(p => p.Id == id);

            if (statusChangeEntity == null)
            {
                throw new BaseException.NotFoundException("not_found", "Status Change not found");
            }

            // Ensure OrderId cannot be changed
            if (statusChangeEntity.OrderId != updatedStatusChange.OrderId)
            {
                throw new BaseException.BadRequestException("invalid_order_id_change", "OrderId cannot be changed when updating a status change.");
            }

            // Validate ChangeTime
            await ValidateChangeTime(statusChangeEntity.OrderId, updatedStatusChange.Status, updatedStatusChange.ChangeTime);

            _mapper.Map(updatedStatusChange, statusChangeEntity);

            statusChangeEntity.LastUpdatedTime = DateTime.UtcNow;
            statusChangeEntity.LastUpdatedBy = "user";

            await _unitOfWork.GetRepository<StatusChange>().UpdateAsync(statusChangeEntity);
            await _unitOfWork.SaveAsync();

            return true;
        }

        // Soft delete status change
        public async Task<bool> Delete(string id)
        {
            var statusChangeRepo = _unitOfWork.GetRepository<StatusChange>();
            var statusChangeEntity = await statusChangeRepo.Entities.FirstOrDefaultAsync(x => x.Id == id);
            if (statusChangeEntity == null || statusChangeEntity.DeletedTime.HasValue || statusChangeEntity.DeletedBy != null)
            {
                throw new BaseException.NotFoundException("not_found", "Status Change not found");
            }
            statusChangeEntity.DeletedTime = DateTime.UtcNow;
            statusChangeEntity.DeletedBy = "user";

            await statusChangeRepo.UpdateAsync(statusChangeEntity);
            await _unitOfWork.SaveAsync();
            return true;
        }

        // Validate the ChangeTime based on the order and status
        private async Task ValidateChangeTime(string orderId, string newStatus, DateTime changeTime)
        {
            if (changeTime > DateTime.UtcNow)
            {
                throw new BaseException.BadRequestException("invalid_change_time", "ChangeTime cannot be in the future.");
            }

            // Retrieve the order and its current status
            var orderRepo = _unitOfWork.GetRepository<Order>();
            var order = await orderRepo.Entities
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.DeletedTime.HasValue);

            if (order == null)
            {
                throw new BaseException.NotFoundException("order_not_found", "Order not found.");
            }

            // If status is 'Pending', ChangeTime must match the order creation time
            //if (newStatus == "Pending" && changeTime != order.CreatedTime)
            //{
            //    throw new BaseException.BadRequestException("invalid_pending_time", "For Pending status, ChangeTime must match the order creation time.");
            //}

            // Get the latest status change for the given order
            var lastStatusChange = await _unitOfWork.GetRepository<StatusChange>().Entities
                .Where(sc => sc.OrderId == orderId && !sc.DeletedTime.HasValue)
                .OrderByDescending(sc => sc.ChangeTime)
                .FirstOrDefaultAsync();

            // If there's a previous status change, ChangeTime must be after the last status change
            if (lastStatusChange != null && changeTime <= lastStatusChange.ChangeTime)
            {
                throw new BaseException.BadRequestException("invalid_change_time", "ChangeTime must be after the previous status change.");
            }
        }
    }
}
