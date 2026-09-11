using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.TraoBang.Dtos.PrepareData;
using traobang.be.application.TraoBang.Interfaces;
using traobang.be.Attributes;
using traobang.be.Controllers.Base;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest;

namespace traobang.be.Controllers
{
    [Route("api/core/trao-bang/prepare")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PrepareDataController : BaseController
    {
        private readonly IPrepareDataService _service;

        public PrepareDataController(ILogger<PrepareDataController> logger, IPrepareDataService service) : base(logger)
        {
            _service = service;
        }

        [Permission(PermissionKeys.DemoMode)]
        [HttpPost("demo")]
        public ApiResponse PrepareForDemo(PrepareDataForDemoDto dto)
        {
            try
            {
                _service.PrepareForDemo(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

    }
}
