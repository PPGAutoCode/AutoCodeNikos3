
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectName.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAttachment([FromBody] Request<CreateAttachmentDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _attachmentService.CreateAttachment(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetAttachment([FromBody] Request<AttachmentRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _attachmentService.GetAttachment(request.Payload);
                return Ok(new Response<Attachment> { Payload = result });
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateAttachment([FromBody] Request<UpdateAttachmentDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _attachmentService.UpdateAttachment(request.Payload);
                return Ok(new Response<string> { Payload = result });
            });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAttachment([FromBody] Request<DeleteAttachmentDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _attachmentService.DeleteAttachment(request.Payload);
                return Ok(new Response<bool> { Payload = result });
            });
        }

        [HttpPost("getList")]
        public async Task<IActionResult> GetListAttachment([FromBody] Request<ListAttachmentRequestDto> request)
        {
            return await SafeExecutor.ExecuteAsync(async () =>
            {
                var result = await _attachmentService.GetListAttachment(request.Payload);
                return Ok(new Response<List<Attachment>> { Payload = result });
            });
        }
    }
}
