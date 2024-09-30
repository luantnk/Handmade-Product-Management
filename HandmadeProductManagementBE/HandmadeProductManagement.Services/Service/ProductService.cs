﻿using HandmadeProductManagement.Contract.Repositories.Entity;
using HandmadeProductManagement.Contract.Repositories.Interface;
using HandmadeProductManagement.Contract.Services.Interface;
using HandmadeProductManagement.Core.Base;
using HandmadeProductManagement.ModelViews.ProductModelViews;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using HandmadeProductManagement.ModelViews.PromotionModelViews;

namespace HandmadeProductManagement.Services.Service
{
    public class ProductService : IProductService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<ProductForCreationDto> _creationValidator;
        private readonly IValidator<ProductForUpdateDto> _updateValidator;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<ProductForCreationDto> creationValidator, IValidator<ProductForUpdateDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _creationValidator = creationValidator;
            _updateValidator = updateValidator;
        }

        public async Task<BaseResponse<IEnumerable<ProductResponseModel>>> SearchProductsAsync(ProductSearchModel searchModel)
        {
            // Validate CategoryId and ShopId datatype (Guid)
            if (!string.IsNullOrEmpty(searchModel.CategoryId) && !IsValidGuid(searchModel.CategoryId))
            {
                return BaseResponse<IEnumerable<ProductResponseModel>>.OkResponse("Invalid Category ID");
            }

            if (!string.IsNullOrEmpty(searchModel.ShopId) && !IsValidGuid(searchModel.ShopId))
            {
                return BaseResponse<IEnumerable<ProductResponseModel>>.OkResponse("Invalid Shop ID");
            }

            // Validate MinRating limit (from 0 to 5)
            if (searchModel.MinRating.HasValue && (searchModel.MinRating < 0 || searchModel.MinRating > 5))
            {
                return BaseResponse<IEnumerable<ProductResponseModel>>.OkResponse("MinRating must be between 0 and 5.");
            }


            var query = _unitOfWork.GetRepository<Product>().Entities.AsQueryable();

            // Apply Search Filters
            if (!string.IsNullOrEmpty(searchModel.Name))
            {
                query = query.Where(p => p.Name.Contains(searchModel.Name));
            }

            if (!string.IsNullOrEmpty(searchModel.CategoryId))
            {
                query = query.Where(p => p.CategoryId == searchModel.CategoryId);
            }

            if (!string.IsNullOrEmpty(searchModel.ShopId))
            {
                query = query.Where(p => p.ShopId == searchModel.ShopId);
            }

            if (!string.IsNullOrEmpty(searchModel.Status))
            {
                query = query.Where(p => p.Status == searchModel.Status);
            }

            if (searchModel.MinRating.HasValue)
            {
                query = query.Where(p => p.Rating >= searchModel.MinRating.Value);
            }


            // Sort Logic
            if (searchModel.SortByPrice)
            {
                query = searchModel.SortDescending
                    ? query.OrderByDescending(p => p.ProductItems.Min(pi => pi.Price))
                    : query.OrderBy(p => p.ProductItems.Min(pi => pi.Price));
            }

            else
            {
                query = searchModel.SortDescending
                    ? query.OrderByDescending(p => p.Rating)
                    : query.OrderBy(p => p.Rating);
            }



            var productResponseModels = await query
                .GroupBy(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.CategoryId,
                    p.ShopId,
                    p.Rating,
                    p.Status,
                    p.SoldCount
                })
                .Select(g => new ProductResponseModel
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Description = g.Key.Description,
                    CategoryId = g.Key.CategoryId,
                    ShopId = g.Key.ShopId,
                    Rating = g.Key.Rating,
                    Status = g.Key.Status,
                    SoldCount = g.Key.SoldCount,
                    // Avoid duplicates
                    Price = g.SelectMany(p => p.ProductItems).Any() ? g.SelectMany(p => p.ProductItems).Min(pi => pi.Price) : 0
                }).OrderBy(pr => searchModel.SortByPrice
                    ? (searchModel.SortDescending ? -pr.Price : pr.Price) // Sort by price ascending or descending
                    : (searchModel.SortDescending ? -pr.Rating : pr.Rating)) // Sort by rating ascending or descending
                .ToListAsync();

            return BaseResponse<IEnumerable<ProductResponseModel>>.OkResponse(productResponseModels);

        }


        // Sort Function

        public async Task<BaseResponse<IEnumerable<ProductResponseModel>>> SortProductsAsync(ProductSortModel sortModel)
        {
            var query = _unitOfWork.GetRepository<Product>().Entities
                .Include(p => p.ProductItems)
                .Include(p => p.Reviews)
                .AsQueryable();

            // Sort by Price
            if (sortModel.SortByPrice)
            {
                query = sortModel.SortDescending
                    ? query.OrderByDescending(p => p.ProductItems.Min(pi => pi.Price))
                    : query.OrderBy(p => p.ProductItems.Min(pi => pi.Price));
            }

            // Sort by Rating
            else if (sortModel.SortByRating)
            {
                query = sortModel.SortDescending
                    ? query.OrderByDescending(p => p.Rating)
                    : query.OrderBy(p => p.Rating);
            }

            var products = await query.ToListAsync();

            var productResponseModels = products.Select(p => new ProductResponseModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                ShopId = p.ShopId,
                Rating = p.Rating,
                Status = p.Status,
                SoldCount = p.SoldCount,
                Price = p.ProductItems.Any() ? p.ProductItems.Min(pi => pi.Price) : 0
            });

            return BaseResponse<IEnumerable<ProductResponseModel>>.OkResponse(productResponseModels);

        }

        public async Task<IList<ProductDto>> GetAll()
        {
            var productRepo = _unitOfWork.GetRepository<Product>();
            var products = await productRepo.Entities
                .ToListAsync();
            var productsDto = _mapper.Map<IList<ProductDto>>(products);
            return productsDto;
        }

        public async Task<ProductDto> GetById(string id)
        {
            var product = await _unitOfWork.GetRepository<Product>().Entities
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                throw new KeyNotFoundException("Product not found");

            var productToReturn = _mapper.Map<ProductDto>(product);
            return productToReturn;

        }



        public async Task<ProductDto> Create(ProductForCreationDto product)
        {
            var result = _creationValidator.ValidateAsync(product);
            if (!result.Result.IsValid)
                throw new ValidationException(result.Result.Errors);
            var productEntity = _mapper.Map<Product>(product);
            productEntity.CreatedTime = DateTime.UtcNow;
            await _unitOfWork.GetRepository<Product>().InsertAsync(productEntity);
            await _unitOfWork.SaveAsync();
            var productToReturn = _mapper.Map<ProductDto>(productEntity);
            return productToReturn;

        }

        public async Task Update(string id, ProductForUpdateDto product)
        {
            var result = _updateValidator.ValidateAsync(product);
            if (!result.Result.IsValid)
                throw new ValidationException(result.Result.Errors);
            var productEntity = await _unitOfWork.GetRepository<Product>().Entities
                .FirstOrDefaultAsync(p => p.Id == id);
            if (productEntity == null)
                throw new KeyNotFoundException("Product not found");
            _mapper.Map(product, productEntity);
            productEntity.LastUpdatedTime = DateTime.UtcNow;
            await _unitOfWork.GetRepository<Product>().UpdateAsync(productEntity);
            await _unitOfWork.SaveAsync();
        }

        public async Task Delete(string id)
        {
            var productRepo = _unitOfWork.GetRepository<Product>();
            var productEntity = await productRepo.Entities.FirstOrDefaultAsync(x => x.Id == id);
            if (productEntity == null)
                throw new KeyNotFoundException("Product not found");
            productEntity.DeletedTime = DateTime.UtcNow;
            await productRepo.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
        }

        public async Task SoftDelete(string id)
        {
            var productRepo = _unitOfWork.GetRepository<Product>();
            var productEntity = await productRepo.Entities.FirstOrDefaultAsync(x => x.Id == id.ToString());
            if (productEntity == null)
                throw new KeyNotFoundException("Product not found");
            productEntity.DeletedTime = DateTime.UtcNow;
            await productRepo.UpdateAsync(productEntity);
            await _unitOfWork.SaveAsync();
        }

        private bool IsValidGuid(string input)
        {
            return Guid.TryParse(input, out _);
        }

    }
}
