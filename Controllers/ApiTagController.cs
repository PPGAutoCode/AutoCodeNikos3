
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiTagController : ControllerBase
    {
        private readonly IApiTagService _apiTagService;

        public ApiTagController(IApiTagService apiTagService)
        {
            _apiTagService = apiTagService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateApiTag([FromBody] Request<CreateApiTagDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiTagService.CreateApiTag(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetApiTag([FromBody] Request<ApiTagRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiTagService.GetApiTag(request.Payload);
                return Ok(new Response<ApiTag> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateApiTag([FromBody] Request<UpdateApiTagDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiTagService.UpdateApiTag(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteApiTag([FromBody] Request<DeleteApiTagDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiTagService.DeleteApiTag(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListApiTag([FromBody] Request<ListApiTagRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiTagService.GetListApiTag(request.Payload);
                return Ok(new Response<List<ApiTag>> { Payload = result });
            });
        }
    }
}
