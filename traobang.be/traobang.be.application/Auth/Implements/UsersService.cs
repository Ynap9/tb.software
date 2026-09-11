using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using traobang.be.application.Auth.Dtos.User;
using traobang.be.application.Auth.Interfaces;
using traobang.be.application.Base;
using traobang.be.domain.Auth;
using traobang.be.infrastructure.data;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest.AppException;
using traobang.be.shared.HttpRequest.BaseRequest;
using traobang.be.shared.HttpRequest.Error;

using traobang.be.shared.Utils;

namespace traobang.be.application.Auth.Implements
{
    public class UsersService : BaseService, IUsersService
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersService(
            TbDbContext tbDbContext, ILogger<BaseService> logger, IHttpContextAccessor httpContextAccessor, IMapper mapper,
            UserManager<AppUser> userManager
        ) : base(tbDbContext, logger, httpContextAccessor, mapper)
        {
            _userManager = userManager;
        }

        public async Task<ViewUserDto> Create(CreateUserDto dto)
        {
            _logger.LogInformation($"{nameof(Create)} dto={JsonSerializer.Serialize(dto)}");

            using var transaction = await _tbDbContext.Database.BeginTransactionAsync();

            bool isRandomPassword = string.IsNullOrEmpty(dto.Password);
            if (isRandomPassword)
            {
                dto.Password = CryptoUtils.GenerateRandomString(8);
            }

            var newUser = new AppUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName ?? "",
                PhoneNumber = dto.PhoneNumber,
                //MsAccount = dto.MsAccount ?? "",
            };
            var result = await _userManager.CreateAsync(newUser, dto.Password);

            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorCreateUser, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRolesAsync(newUser, dto.RoleNames);

            await _tbDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var rslt = _mapper.Map<ViewUserDto>(newUser);

            if (isRandomPassword)
            {
                rslt.PasswordRandom = dto.Password;
            }

            return rslt;
        }

        public async Task<ViewUserDto> FindById(string id)
        {
            _logger.LogInformation($"{nameof(FindById)} id={id}");

            var user = await _userManager.FindByIdAsync(id);

            var roles = await _userManager.GetRolesAsync(user);

            var data = _mapper.Map<ViewUserDto>(user);
            data.Roles = roles;
            return data;
        }

        public async Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)} dto={JsonSerializer.Serialize(dto)}");

            var query = _userManager.Users.AsNoTracking().AsQueryable();

            var totalCount = await query.CountAsync();

            var users = await query
                    .AsNoTracking()
                    .Where(x => x.Id != getCurrentUserId())
                    .OrderBy(x => x.UserName)
                    .Paging(dto)
                    .ToListAsync();
            var items = _mapper.Map<List<ViewUserDto>>(users);

            return new BaseResponsePagingDto<ViewUserDto>
            {
                Items = items,
                TotalItems = totalCount,
            };
        }

        public async Task SetRoleForUser(SetRoleForUserDto dto)
        {
            _logger.LogInformation($"{nameof(SetRoleForUser)} dto={JsonSerializer.Serialize(dto)}");

            var transaction = await _tbDbContext.Database.BeginTransactionAsync();
            var user = await _userManager.FindByIdAsync(dto.Id)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            await _userManager.RemoveFromRolesAsync(user, (await _userManager.GetRolesAsync(user)).ToArray());
            await _userManager.AddToRolesAsync(user, dto.RoleNames);
            await _tbDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task Update(UpdateUserDto dto)
        {
            _logger.LogInformation($"{nameof(Update)} dto={JsonSerializer.Serialize(dto)}");

            var user = await _userManager.FindByIdAsync(dto.Id)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            var transaction = await _tbDbContext.Database.BeginTransactionAsync();
            user.FullName = dto.FullName ?? user.FullName;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
            user.Email = dto.Email ?? user.Email;

            await _userManager.UpdateAsync(user);
            await _userManager.RemoveFromRolesAsync(user, (await _userManager.GetRolesAsync(user)).ToArray());
            await _userManager.AddToRolesAsync(user, dto.RoleNames);

            await _tbDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<ViewMeDto> GetMe()
        {
            _logger.LogInformation($"{nameof(GetMe)}");
            var userId = getCurrentUserId();

            var data = await _userManager.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var user = _mapper.Map<ViewMeDto>(data);

            var roles = await _userManager.GetRolesAsync(data);
            user.Roles = roles;

            var permissions = (from u in _tbDbContext.Users
                               join userRole in _tbDbContext.UserRoles on u.Id equals userRole.UserId
                               join role in _tbDbContext.Roles on userRole.RoleId equals role.Id
                               join roleClaims in _tbDbContext.RoleClaims on role.Id equals roleClaims.RoleId
                               where u.Id == userId
                                 && roleClaims.ClaimType == CustomClaimTypes.Permission
                               select roleClaims.ClaimValue).ToList();
            user.Permissions = permissions;

            return user;
        }

        public async Task Delete(string id)
        {
            _logger.LogInformation($"{nameof(Delete)} id={id}");

            var user = await _userManager.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            var userRoles = await _userManager.GetRolesAsync(user);

            var isSuperAdmin = userRoles.Any(x => x == RoleConstants.ROLE_SUPER_ADMIN);
            if (isSuperAdmin)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorCannotDeleteSuperAdmin);
            }

            if (user.Id == getCurrentUserId())
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorCannotDeleteCurrentUser);
            }

            await _userManager.DeleteAsync(user);
        }

        public async Task ChangePassword(ChangePasswordDto dto)
        {
            // không log dto vì chứa mật khẩu
            _logger.LogInformation($"{nameof(ChangePassword)}");

            var user = await _userManager.FindByIdAsync(getCurrentUserId())
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorChangePassword, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task ToggleLockAccount(string id)
        {
            _logger.LogInformation($"{nameof(ToggleLockAccount)} id={id}");

            var user = await _userManager.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // Unlock account
                user.LockoutEnd = DateTimeOffset.UtcNow;
            }
            else
            {
                // Lock account for 100 years
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            }

            await _userManager.UpdateAsync(user);
        }
    }
}
