
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateImage([FromBody] Request<CreateImageDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _imageService.CreateImage(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetImage([FromBody] Request<ImageRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _imageService.GetImage(request.Payload);
                return Ok(new Response<Image> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateImage([FromBody] Request<UpdateImageDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _imageService.UpdateImage(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteImage([FromBody] Request<DeleteImageDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _imageService.DeleteImage(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetListImage([FromBody] Request<ListImageRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _imageService.GetListImage(request.Payload);
                return Ok(new Response<List<Image>> { Payload = result });
            });
        }
    }
}
