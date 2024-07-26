
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppEnvironmentController : ControllerBase
    {
        private readonly IAppEnvironmentService _appEnvironmentService;

        public AppEnvironmentController(IAppEnvironmentService appEnvironmentService)
        {
            _appEnvironmentService = appEnvironmentService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAppEnvironment([FromBody] Request<CreateAppEnvironmentDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _appEnvironmentService.CreateAppEnvironment(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetAppEnvironment([FromBody] Request<AppEnvironmentRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _appEnvironmentService.GetAppEnvironment(request.Payload);
                return Ok(new Response<AppEnvironment> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateAppEnvironment([FromBody] Request<UpdateAppEnvironmentDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _appEnvironmentService.UpdateAppEnvironment(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAppEnvironment([FromBody] Request<DeleteAppEnvironmentDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _appEnvironmentService.DeleteAppEnvironment(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListAppEnvironment([FromBody] Request<ListAppEnvironmentRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _appEnvironmentService.GetListAppEnvironment(request.Payload);
                return Ok(new Response<List<AppEnvironment>> { Payload = result });
            });
        }
    }
}
