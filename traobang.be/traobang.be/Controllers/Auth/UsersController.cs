using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.Auth.Dtos.User;
using traobang.be.application.Auth.Interfaces;
using traobang.be.Attributes;
using traobang.be.Controllers.Base;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest;


namespace traobang.be.Controllers.Auth
{
    [Route("api/app/users")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UsersController : BaseController
    {
        private readonly IUsersService _usersService;
        public UsersController(ILogger<BaseController> logger, IUsersService usersService) : base(logger)
        {
            _usersService = usersService;
        }

        /// <summary>
        /// Tạo user
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("")]
        [Permission(PermissionKeys.UserAdd)]
        public async Task<ApiResponse> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                var data = await _usersService.Create(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Cập nhật user
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("")]
        [Permission(PermissionKeys.UserUpdate)]
        public async Task<ApiResponse> UpdateUser([FromBody] UpdateUserDto dto)
        {
            try
            {
                await _usersService.Update(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Tìm user phân trang
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet("")]
        [Permission(PermissionKeys.UserView)]
        public async Task<ApiResponse> Find([FromQuery] FindPagingUserDto dto)
        {
            try
            {
                var data = await _usersService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Tìm user theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [Permission(PermissionKeys.UserView)]
        public async Task<ApiResponse> GetById([FromRoute] string id)
        {
            try
            {
                var data = await _usersService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Gán role cho user
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("set-to-role")]
        [Permission(PermissionKeys.UserSetRoles)]
        public async Task<ApiResponse> SetToRole([FromBody] SetRoleForUserDto dto)
        {
            try
            {
                await _usersService.SetRoleForUser(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Lấy thông tin chính mình
        /// </summary>
        /// <returns></returns>
        [HttpGet("me")]
        public async Task<ApiResponse> GetMe()
        {
            try
            {
                var data = await _usersService.GetMe();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Tự đổi mật khẩu của chính mình
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("change-password")]
        public async Task<ApiResponse> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                await _usersService.ChangePassword(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Xóa user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Permission(PermissionKeys.UserDelete)]
        [HttpDelete("{id}")]
        public async Task<ApiResponse> DeleteUser([FromRoute] string id)
        {
            try
            {
                await _usersService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Khóa/mở khóa user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Permission(PermissionKeys.UserUpdate)]
        [HttpPut("{id}/toggle-lock")]
        public async Task<ApiResponse> ToggleLockUser([FromRoute] string id)
        {
            try
            {
                await _usersService.ToggleLockAccount(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
