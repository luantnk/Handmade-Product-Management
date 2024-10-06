﻿using HandmadeProductManagement.Contract.Services.Interface;
using HandmadeProductManagement.Core.Base;
using Microsoft.AspNetCore.Mvc;
using HandmadeProductManagement.Core.Constants;
using HandmadeProductManagement.ModelViews.VariationOptionModelViews;

namespace HandmadeProductManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VariationOptionController : ControllerBase
    {
        private readonly IVariationOptionService _variationOptionService;

        public VariationOptionController(IVariationOptionService variationOptionService)
        {
            _variationOptionService = variationOptionService;
        }

        // GET: api/variationoption/page?page=1&pageSize=10
        [HttpGet("page")]
        public async Task<IActionResult> GetByPage([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var response = new BaseResponse<IList<VariationOptionDto>>
            {
                Code = "Success",
                StatusCode = StatusCodeHelper.OK,
                Message = "Get Variation Options successfully.",
                Data = await _variationOptionService.GetByPage(page, pageSize)
            };
            return Ok(response);
        }

        // GET: api/variationoption/variation/{variationId}
        [HttpGet("variation/{variationId}")]
        public async Task<IActionResult> GetByVariationId(string variationId)
        {
            var response = new BaseResponse<IList<VariationOptionDto>>
            {
                Code = "Success",
                StatusCode = StatusCodeHelper.OK,
                Message = "Get Variation Options by Variation ID successfully.",
                Data = await _variationOptionService.GetByVariationId(variationId)
            };
            return Ok(response);
        }

        // POST: api/variationoption
        [HttpPost]
        public async Task<IActionResult> CreateVariationOption([FromBody] VariationOptionForCreationDto variationOption)
        {
            var result = await _variationOptionService.Create(variationOption);
            var response = new BaseResponse<bool>
            {
                Code = "Success",
                StatusCode = StatusCodeHelper.OK,
                Message = "Created Variation Option successfully.",
                Data = result
            };
            return Ok(response);
        }

        // PUT: api/variationoption/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVariationOption(string id, [FromBody] VariationOptionForUpdateDto variationOption)
        {
            var result = await _variationOptionService.Update(id, variationOption);
            var response = new BaseResponse<bool>
            {
                Code = "Success",
                StatusCode = StatusCodeHelper.OK,
                Message = "Updated Variation Option successfully.",
                Data = result
            };
            return Ok(response);
        }

        // DELETE: api/variationoption/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVariationOption(string id)
        {
            await _variationOptionService.Delete(id);

            var response = new BaseResponse<string>
            {
                Code = "Success",
                StatusCode = StatusCodeHelper.OK,
                Message = $"Variation Option with ID {id} has been successfully deleted.",
                Data = null
            };
            return Ok(response);
        }
    }
}