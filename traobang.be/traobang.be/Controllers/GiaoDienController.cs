using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.TraoBang.Dtos.GiaoDien;
using traobang.be.application.TraoBang.Interfaces;
using traobang.be.Attributes;
using traobang.be.Controllers.Base;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest;

namespace traobang.be.Controllers
{
    [Route("api/core/trao-bang/giao-dien")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GiaoDienController : BaseController
    {
        private readonly IGiaoDienService _giaoDienService;

        public GiaoDienController(
            ILogger<GiaoDienController> logger,
            IGiaoDienService giaoDienService
        )
            : base(logger)
        {
            _giaoDienService = giaoDienService;
        }

        [Permission(PermissionKeys.GiaoDiennAdd)]
        [HttpPost("")]
        public ApiResponse Create(CreateGiaoDienDto dto)
        {
            try
            {
                var data = _giaoDienService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.GiaoDienUpdate)]
        [HttpPut("")]
        public ApiResponse Update(UpdateGiaoDienDto dto)
        {
            try
            {
                _giaoDienService.Update(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.GiaoDienView)]
        [HttpGet("")]
        public ApiResponse FindPaging([FromQuery] FindPagingGiaoDienDto dto)
        {
            try
            {
                var result = _giaoDienService.FindPaging(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.GiaoDienView)]
        [HttpGet("{id}")]
        public ApiResponse FindById([FromRoute] int id)
        {
            try
            {
                var result = _giaoDienService.FindById(id);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Lấy giao diện cho chương trình đang hoạt động
        /// </summary>
        /// <returns></returns>
        [HttpGet("active-plan")]
        [AllowAnonymous]
        public ApiResponse FindByActivePlan()
        {
            try
            {
                var result = _giaoDienService.FindByActivePlan();
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.GiaoDienDelete)]
        [HttpDelete("{id}")]
        public ApiResponse Delete([FromRoute] int id)
        {
            try
            {
                _giaoDienService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.GiaoDienView)]
        [HttpGet("list")]
        public async Task<ApiResponse> ListGiaoDien()
        {
            try
            {
                var result = await _giaoDienService.GetListGiaoDien();
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
