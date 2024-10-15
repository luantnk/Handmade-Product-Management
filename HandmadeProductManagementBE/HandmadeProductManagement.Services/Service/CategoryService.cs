﻿using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using HandmadeProductManagement.Contract.Repositories.Entity;
using HandmadeProductManagement.Contract.Repositories.Interface;
using HandmadeProductManagement.Contract.Services.Interface;
using HandmadeProductManagement.Core.Base;
using HandmadeProductManagement.ModelViews.CategoryModelViews;
using Microsoft.EntityFrameworkCore;

namespace HandmadeProductManagement.Services.Service
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CategoryForCreationDto> _creationValidator;
        private readonly IValidator<CategoryForUpdateDto> _updateValidator;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper,
            IValidator<CategoryForCreationDto> creationValidator, IValidator<CategoryForUpdateDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _creationValidator = creationValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IList<CategoryDto>> GetAll()
        {
            var categories = await _unitOfWork.GetRepository<Category>().Entities
                .Include(c => c.Promotion)
                .Where(c => c.DeletedTime == null)
                .ToListAsync();
            return _mapper.Map<IList<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> GetById(string id)
        {
            var category = await _unitOfWork.GetRepository<Category>().Entities
                .Include(c => c.Promotion)
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedTime == null);
            if (category == null)
                throw new BaseException.NotFoundException("404", "Category not found");
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> Create(CategoryForCreationDto category)
        {
            var validationResult = await _creationValidator.ValidateAsync(category);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var existedCategory = await _unitOfWork.GetRepository<Category>().Entities
                .FirstOrDefaultAsync(c => c.Name == category.Name && c.DeletedTime == null);
            if (existedCategory is not null)
                throw new ValidationException("Category name already exists");

            var categoryEntity = _mapper.Map<Category>(category);
            categoryEntity.PromotionId = null;
            categoryEntity.CreatedTime = DateTime.UtcNow;
            categoryEntity.Status = "active";
            categoryEntity.CreatedBy = "user";
            categoryEntity.LastUpdatedBy = "user";
            await _unitOfWork.GetRepository<Category>().InsertAsync(categoryEntity);
            await _unitOfWork.SaveAsync();
            _mapper.Map<CategoryDto>(categoryEntity);

            return true; 
        }




        public async Task<CategoryDto> Update(string id, CategoryForUpdateDto category)
        {
            var validationResult = await _updateValidator.ValidateAsync(category);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);
            var categoryEntity = await _unitOfWork.GetRepository<Category>().Entities
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedTime == null);
            if (categoryEntity == null)
                throw new KeyNotFoundException("Category not found");
            var existedCategory = await _unitOfWork.GetRepository<Category>().Entities
                .FirstOrDefaultAsync(c => c.Name == category.Name && c.DeletedTime == null);
            if (existedCategory is not null)
                throw new ValidationException("Category name already exists");
            _mapper.Map(category, categoryEntity);
            categoryEntity.LastUpdatedTime = DateTime.UtcNow;
            await _unitOfWork.GetRepository<Category>().UpdateAsync(categoryEntity);
            await _unitOfWork.SaveAsync();
            return _mapper.Map<CategoryDto>(categoryEntity);
        }

        public async Task<bool> SoftDelete(string id)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category>();
            var categoryEntity = await categoryRepo.Entities.FirstOrDefaultAsync(c => c.Id == id);
            if (categoryEntity == null)
                throw new KeyNotFoundException("Category not found");
            categoryEntity.DeletedTime = DateTime.UtcNow;
            await categoryRepo.UpdateAsync(categoryEntity);
            await _unitOfWork.SaveAsync();
            return true;
        }
    }
}