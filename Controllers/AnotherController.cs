
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnotherController : ControllerBase
    {
        private readonly IAnotherService _anotherService;

        public AnotherController(IAnotherService anotherService)
        {
            _anotherService = anotherService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAnother([FromBody] Request<CreateAnotherDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _anotherService.CreateAnother(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetAnother([FromBody] Request<AnotherRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _anotherService.GetAnother(request.Payload);
                return Ok(new Response<AnotherDto> { Payload = result });
            });
        }
    }
}
