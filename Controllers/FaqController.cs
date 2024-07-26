
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("faq")]
    public class FaqController : ControllerBase
    {
        private readonly IFAQService _faqService;

        public FaqController(IFAQService faqService)
        {
            _faqService = faqService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateFAQ([FromBody] Request<CreateFAQDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqService.CreateFAQ(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetFAQ([FromBody] Request<FAQRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqService.GetFAQ(request.Payload);
                return Ok(new Response<FAQ> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateFAQ([FromBody] Request<UpdateFAQDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqService.UpdateFAQ(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteFAQ([FromBody] Request<DeleteFAQDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqService.DeleteFAQ(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListFAQ([FromBody] Request<ListFAQRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _faqService.GetListFAQ(request.Payload);
                return Ok(new Response<List<FAQ>> { Payload = result });
            });
        }
    }
}
