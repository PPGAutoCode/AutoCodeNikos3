
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorController : ControllerBase
    {
        private readonly IAuthorService _authorService;

        public AuthorController(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAuthor([FromBody] Request<CreateAuthorDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _authorService.CreateAuthor(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetAuthor([FromBody] Request<AuthorRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _authorService.GetAuthor(request.Payload);
                return Ok(new Response<AuthorDto> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateAuthor([FromBody] Request<UpdateAuthorDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _authorService.UpdateAuthor(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAuthor([FromBody] Request<DeleteAuthorDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _authorService.DeleteAuthor(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListAuthor([FromBody] Request<ListAuthorRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _authorService.GetListAuthor(request.Payload);
                return Ok(new Response<List<AuthorDto>> { Payload = result });
            });
        }
    }
}
