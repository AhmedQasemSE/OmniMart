using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Common;

namespace OmniMart.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseController : ControllerBase
    {
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            if (result.ErrorType == ErrorType.Validation)
            {
                return UnprocessableEntity(new { Errors = result.ValidationErrors });
            }

            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(new { Error = result.ErrorMessage }),
                ErrorType.Conflict => Conflict(new { Error = result.ErrorMessage }),
                ErrorType.Unauthorized => Unauthorized(new { Error = result.ErrorMessage }),   
                _ => BadRequest(new { Error = result.ErrorMessage })
            };
        }
    }
}