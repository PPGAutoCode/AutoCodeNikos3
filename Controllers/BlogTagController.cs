
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogTagController : ControllerBase
    {
        private readonly IBlogTagService _blogTagService;

        public BlogTagController(IBlogTagService blogTagService)
        {
            _blogTagService = blogTagService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBlogTag([FromBody] Request<CreateBlogTagDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogTagService.CreateBlogTag(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetBlogTag([FromBody] Request<BlogTagRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogTagService.GetBlogTag(request.Payload);
                return Ok(new Response<BlogTag> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateBlogTag([FromBody] Request<UpdateBlogTagDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogTagService.UpdateBlogTag(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteBlogTag([FromBody] Request<DeleteBlogTagDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogTagService.DeleteBlogTag(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListBlogTag([FromBody] Request<ListBlogTagRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogTagService.GetListBlogTag(request.Payload);
                return Ok(new Response<List<BlogTag>> { Payload = result });
            });
        }
    }
}
