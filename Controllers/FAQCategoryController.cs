
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FAQCategoryController : ControllerBase
    {
        private readonly IFAQCategoryService _faqCategoryService;

        public FAQCategoryController(IFAQCategoryService faqCategoryService)
        {
            _faqCategoryService = faqCategoryService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateFAQCategory([FromBody] Request<CreateFAQCategoryDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqCategoryService.CreateFAQCategory(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetFAQCategory([FromBody] Request<FAQCategoryRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqCategoryService.GetFAQCategory(request.Payload);
                return Ok(new Response<FAQCategory> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateFAQCategory([FromBody] Request<UpdateFAQCategoryDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqCategoryService.UpdateFAQCategory(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteFAQCategory([FromBody] Request<DeleteFAQCategoryDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqCategoryService.DeleteFAQCategory(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListFAQCategory([FromBody] Request<ListFAQCategoryRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqCategoryService.GetListFAQCategory(request.Payload);
                return Ok(new Response<List<FAQCategory>> { Payload = result });
            });
        }
    }
}
