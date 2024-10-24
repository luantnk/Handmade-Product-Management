﻿using HandmadeProductManagement.Core.Common;
using HandmadeProductManagement.Core.Constants;
using HandmadeProductManagement.Core.Store;
using HandmadeProductManagement.ModelViews.CategoryModelViews;
using HandmadeProductManagement.ModelViews.ProductModelViews;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UI.Pages
{
    public class HomePageModel : PageModel
    {
        private readonly ApiResponseHelper _apiResponseHelper;

        public HomePageModel(ApiResponseHelper apiResponseHelper)
        {
            _apiResponseHelper = apiResponseHelper ?? throw new ArgumentNullException(nameof(apiResponseHelper));
        }

        public List<TopSellingProducts> Top10SellingProducts { get; set; }
        public List<ProductForDashboard> Top10NewProducts { get; set; }
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

        public void OnGet()
        {
            Top10SellingProducts = GetTop10SellingProducts();
            Top10NewProducts = GetTop10NewProducts();
        }

        private List<TopSellingProducts> GetTop10SellingProducts()
        {
            var response = _apiResponseHelper.GetAsync<List<TopSellingProducts>>(Constants.ApiBaseUrl + "/api/dashboard/top-10-selling-products").Result; // Lấy dữ liệu từ API
            if (response.StatusCode == StatusCodeHelper.OK && response.Data != null)
            {
                return response.Data;
            }
            return new List<TopSellingProducts>();
        }

        private List<ProductForDashboard> GetTop10NewProducts()
        {
            var response = _apiResponseHelper.GetAsync<List<ProductForDashboard>>(Constants.ApiBaseUrl + "/api/dashboard/top-10-new-products").Result; // Lấy dữ liệu từ API
            if (response.StatusCode == StatusCodeHelper.OK && response.Data != null)
            {
                return response.Data;
            }
            return new List<ProductForDashboard>();
        }
    }

}