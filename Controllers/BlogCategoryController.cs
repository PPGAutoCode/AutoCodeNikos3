
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogCategoryController : ControllerBase
    {
        private readonly IBlogCategoryService _blogCategoryService;

        public BlogCategoryController(IBlogCategoryService blogCategoryService)
        {
            _blogCategoryService = blogCategoryService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBlogCategory([FromBody] Request<CreateBlogCategoryDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogCategoryService.CreateBlogCategory(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetBlogCategory([FromBody] Request<BlogCategoryRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogCategoryService.GetBlogCategory(request.Payload);
                return Ok(new Response<BlogCategory> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateBlogCategory([FromBody] Request<UpdateBlogCategoryDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogCategoryService.UpdateBlogCategory(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteBlogCategory([FromBody] Request<DeleteBlogCategoryDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogCategoryService.DeleteBlogCategory(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListBlogCategory([FromBody] Request<ListBlogCategoryRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _blogCategoryService.GetListBlogCategory(request.Payload);
                return Ok(new Response<List<BlogCategory>> { Payload = result });
            });
        }
    }
}
