using traobang.be.application.Auth.Dtos.User;
using traobang.be.shared.HttpRequest.BaseRequest;

namespace traobang.be.application.Auth.Interfaces
{
    public interface IUsersService
    {
        public Task<ViewUserDto> Create(CreateUserDto dto);
        public Task Update(UpdateUserDto dto);
        public Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto);
        public Task<ViewUserDto> FindById(string id);
        public Task SetRoleForUser(SetRoleForUserDto dto);
        public Task<ViewMeDto> GetMe();
        public Task Delete(string id);
        public Task ToggleLockAccount(string id);
        /// <summary>
        /// Người dùng đang đăng nhập tự đổi mật khẩu của mình
        /// </summary>
        /// <param name="dto"></param>
        public Task ChangePassword(ChangePasswordDto dto);
    }
}
