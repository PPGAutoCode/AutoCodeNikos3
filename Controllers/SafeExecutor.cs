
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
            catch (BusinessException ex)
            {
                return new ObjectResult(new { exception = new { code = ex.Code, description = ex.Description } }) { StatusCode = 200 };
            }
            catch (TechnicalException ex)
            {
                return new ObjectResult(new { exception = new { code = ex.Code, description = ex.Description } }) { StatusCode = 200 };
            }
            catch (Exception)
            {
                return new ObjectResult(new { exception = new { code = "1001", description = "A technical exception has occurred, please contact your system administrator" } }) { StatusCode = 200 };
            }
        }
    }
}
