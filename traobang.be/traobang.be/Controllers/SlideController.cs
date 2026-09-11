using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.TraoBang.Dtos.Slide;
using traobang.be.application.TraoBang.Interfaces;
using traobang.be.Attributes;
using traobang.be.Controllers.Base;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest;

namespace traobang.be.Controllers
{
    [Route("api/core/trao-bang/slide")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SlideController : BaseController
    {
        private readonly ISlideService _slideService;

        public SlideController(ILogger<SlideController> logger, ISlideService slideService) : base(logger)
        {
            _slideService = slideService;
        }

        [Permission(PermissionKeys.SlideTextAddFast)]
        [HttpPost("text/fast")]
        public ApiResponse CreateSlideTextFast(CreateSlideTextFastDto dto)
        {
            try
            {
                _slideService.CreateSlideTextFast(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SlideUpdateOrderTienDo)]
        [HttpPut("tien-do/order")]
        public ApiResponse UpdateTienDoOrder(UpdateTienDoOrderDto dto)
        {
            try
            {
                _slideService.UpdateTienDoOrder(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SlideRevertTienDo)]
        [HttpPut("tien-do/revert/{id}")]
        public async Task<ApiResponse> RevertTienDoTraoBang(int id)
        {
            try
            {
                await _slideService.RevertTienDoTraoBang(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
