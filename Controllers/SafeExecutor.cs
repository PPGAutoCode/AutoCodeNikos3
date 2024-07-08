
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Types;
using ProjectName.ControllersExceptions;

namespace ProjectName.Controllers
{
    public static class SafeExecutor
    {
        public static async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (BusinessException bex)
            {
                return new OkObjectResult(new
                {
                    exception = new
                    {
                        code = bex.Code,
                        description = bex.Description
                    }
                });
            }
            catch (TechnicalException tex)
            {
                return new OkObjectResult(new
                {
                    exception = new
                    {
                        code = tex.Code,
                        description = tex.Description
                    }
                });
            }
            catch (Exception ex)
            {
                return new OkObjectResult(new
                {
                    exception = new
                    {
                        code = "1001",
                        description = "A technical exception has occurred, please contact your system administrator"
                    }
                });
            }
        }
    }
}
