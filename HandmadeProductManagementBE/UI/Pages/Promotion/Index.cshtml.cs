using HandmadeProductManagement.Core.Store;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HandmadeProductManagement.ModelViews.PromotionModelViews;
using HandmadeProductManagement.Core.Common;
namespace UI.Pages.Promotion
{
    public class PromotionModel : PageModel
    {
        private readonly ApiResponseHelper _apiHelper;

        public PromotionModel(ApiResponseHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public List<PromotionDto> Promotions { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public async Task OnGetAsync(int pageNumber = 1, int pageSize = 10)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;

            var response = await _apiHelper.GetAsync<List<PromotionDto>>($"{Constants.ApiBaseUrl}/api/promotions?pageNumber={PageNumber}&pageSize={PageSize}");
            if (response != null && response.Data != null)
            {
                Promotions = response.Data;
            }
            else
            {
                Promotions = new List<PromotionDto>();
            }
        }
    }
}