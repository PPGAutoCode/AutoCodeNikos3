
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class APIEndpointController : ControllerBase
    {
        private readonly IAPIEndpointService _apiEndpointService;

        public APIEndpointController(IAPIEndpointService apiEndpointService)
        {
            _apiEndpointService = apiEndpointService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAPIEndpoint([FromBody] Request<CreateAPIEndpointDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiEndpointService.CreateAPIEndpoint(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetAPIEndpoint([FromBody] Request<APIEndpointRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiEndpointService.GetAPIEndpoint(request.Payload);
                return Ok(new Response<APIEndpoint> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateAPIEndpoint([FromBody] Request<UpdateAPIEndpointDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiEndpointService.UpdateAPIEndpoint(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAPIEndpoint([FromBody] Request<DeleteAPIEndpointDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiEndpointService.DeleteAPIEndpoint(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListAPIEndpoint([FromBody] Request<ListAPIEndpointRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _apiEndpointService.GetListAPIEndpoint(request.Payload);
                return Ok(new Response<List<APIEndpoint>> { Payload = result });
            });
        }
    }
}
