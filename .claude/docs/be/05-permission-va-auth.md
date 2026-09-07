# 05 — Permission & Auth

## Cơ chế

- Xác thực: **OpenIddict** phát JWT tại `POST /connect/token` (`AuthorizationController`). Hỗ trợ `password`, `refresh_token`, `authorization_code` (PKCE), `client_credentials`.
- Phân quyền: **không dùng policy của ASP.NET**. Dùng attribute tự viết `PermissionAttribute` (`traobang.be/Attributes/PermissionAttribute.cs`), là một `AuthorizeAttribute` + `IAuthorizationFilter` **query thẳng vào DB** mỗi request.

Chuỗi kiểm tra trong `PermissionAttribute.OnAuthorization`:

1. Chưa authenticated → `401 Unauthorized`.
2. Lấy claim `username` → join `Users` × `UserRoles` × `Roles`. Nếu có role `ROLE_SUPER_ADMIN` → **cho qua toàn bộ**.
3. Join tiếp `RoleClaims` với `ClaimType == CustomClaimTypes.Permission` ("Permission"). Nếu `ClaimValue` nằm trong danh sách `Permissions` truyền vào attribute → cho qua.
4. Ngược lại → `403 Forbid`.

Hệ quả quan trọng: **permission được lưu ở role claim trong DB, không nằm trong token**. Đổi quyền của role có hiệu lực ngay, không cần user đăng nhập lại. Ngược lại, mỗi request có quyền = thêm vài query DB.

## Gán permission ở controller

```csharp
[Permission(PermissionKeys.PlanView)]
[HttpGet("")]
public ApiResponse FindPaging([FromQuery] FindPagingPlanDto dto) { ... }
```

Truyền nhiều key = quan hệ **OR** (có bất kỳ quyền nào là qua):

```csharp
[Permission(PermissionKeys.GetTienDo, PermissionKeys.DieuKhienView, PermissionKeys.CheckinView)]
[HttpGet("sinh-vien-nhan-bang/tien-do")]
```

Không cần quyền (màn hình chiếu, trang tra cứu QR):

```csharp
[AllowAnonymous]
[HttpGet("infor-sinh-vien-dang-trao")]
```

Không gắn gì cả = chỉ cần đăng nhập (do `[Authorize]` ở mức class), ví dụ `GET api/app/users/me`.

## Thêm một permission key mới

File: `traobang.be.shared/Constants/Auth/PermissionKeys.cs`. Phải sửa **hai chỗ**:

### 1. Khai báo const trong đúng `#region`

```csharp
#region chức năng trong menu cấu hình plan
public const string CategoryCauHinhPlan = "QL Plan";
public const string PlanAdd = Function + "PlanAdd";
public const string PlanUpdate = Function + "PlanUpdate";
public const string PlanDelete = Function + "PlanDelete";
public const string PlanView = Function + "PlanView";
#endregion
```

Hai prefix:

```csharp
public const string Menu = "Menu.";        // quyền hiển thị menu / vào được màn hình
public const string Function = "Function."; // quyền thực hiện chức năng
```

Quy ước tên: `Function + "<Resource><Hành động>"` — `PlanAdd`, `PlanUpdate`, `PlanDelete`, `PlanView`. Chức năng đặc thù thì đặt theo hành động: `PushSinhVienVaoHangDoi`, `NextSubPlan`, `RestartActivePlan`, `DemoMode`.

Menu: `Menu + "ManHinh"` rồi nối `_<Con>`: `MenuManHinhSanKhau = MenuManHinh + "_SanKhau"`.

### 2. Thêm vào mảng `All`

```csharp
public static readonly (string Key, string Name, string Category)[] All =
{
    (PlanAdd,    "[Cấu hình chương trình] Thêm",     CategoryCauHinhPlan),
    (PlanUpdate, "[Cấu hình chương trình] Cập nhật", CategoryCauHinhPlan),
    ...
};
```

Đây là nguồn duy nhất cho `GET api/app/permissions` (`PermissionsService.GetAllPermissions()`) — màn gán quyền cho role trên FE đọc từ đây. **Quên bước 2 = quyền tồn tại nhưng admin không gán được cho role nào.**

`Name` theo format `[Nhóm] Hành động`. `Category` dùng const `Category*` sẵn có (`CategoryUser`, `CategoryRole`, `CategoryCauHinhPlan`, `CategoryCauHinhSubPlan`, `CategoryCauHinhSlide`, `CategoryCauHinhGiaoDien`, `CategoryMainFunction`) hoặc `"Menu"` cho các key menu.

### 3. Đồng bộ sang FE

`traobang.fe/src/app/shared/constants/permission.constants.ts` là bản chép tay của cùng tập string, dùng cho `permissionGuard` và ẩn/hiện menu. Sửa BE thì phải sửa FE.

## Role

`RoleConstants.ROLE_SUPER_ADMIN` = `"SuperAdmin"`. **Nội dung hai file bị hoán đổi**: class `RoleConstants` nằm trong `CustomClaimTypes.cs`, còn class `CustomClaimTypes` nằm trong `RoleConstants.cs`. Cả hai cùng namespace `traobang.be.shared.Constants.Auth` nên compile bình thường — chỉ cần biết để không tìm nhầm file.

- `BaseService.IsSuperAdmin()` đọc từ claim `ClaimTypes.Role` trong token.
- `PermissionAttribute` đọc role từ DB (không phải token).
- Role được seed trong `infrastructure.data/Seeder/SeedUser.cs`, chạy ở đầu `Program.cs`.

Quản lý role: `RoleService`. Gán quyền cho role = ghi `IdentityRoleClaim` với `ClaimType = CustomClaimTypes.Permission`:

```csharp
var oldRoleClaims = await _tbDbContext.RoleClaims.Where(rc => rc.RoleId == role.Id).ToListAsync();
_tbDbContext.RoleClaims.RemoveRange(oldRoleClaims);

var newRoleClaims = dto.PermissionKey.Select(per => new IdentityRoleClaim<string>
{
    RoleId = role.Id,
    ClaimType = CustomClaimTypes.Permission,
    ClaimValue = per
}).ToList();
_tbDbContext.RoleClaims.AddRange(newRoleClaims);
```

(Update = xóa hết rồi ghi lại, không diff.)

## Claim trong token

`AuthorizationController.Exchange` set các claim sau cho password grant / authorization code grant:

| Claim | Giá trị |
|---|---|
| `Claims.Subject` (`sub`) | `user.Id` |
| `Claims.Name` (`name`) | `user.FullName` |
| `Claims.Username` (`username`) | `user.UserName` — **`PermissionAttribute` dùng claim này** |
| `ClaimTypes.Role` | mỗi role một claim |
| `CustomClaimTypes.UserType` | `"SV"` (chỉ ở authorization code flow) |

`SetDestinations` quyết định claim nào vào access token / identity token. Thêm claim mới thì phải khai báo trong `SetDestinations`, không thì claim bị rơi.

Token ký bằng **symmetric key** lấy từ config `AuthServer:SecretKey` (Infisical ở Staging/Production).

## Lấy thông tin user hiện tại trong service

```csharp
var userId   = getCurrentUserId();   // sub / NameIdentifier
var username = getCurrentName();     // claim name = FullName -> dùng cho CreatedBy / DeletedBy
var isAdmin  = IsSuperAdmin();
```

## Gotcha

- `MenuUserManagementUser` và `MenuUserManagementRole` **cùng giá trị** `"Menu.UserManagement_User"` (copy-paste sót). Sửa sẽ làm sai lệch quyền đã gán trong DB — không tự sửa.
- `RoleAdd/RoleUpdate/RoleDelete/RoleView` = `"Function.Add"` / `"Function.Update"` / … (thiếu tiền tố `Role`), khác quy ước của các nhóm còn lại. Đây là dữ liệu đã nằm trong DB, giữ nguyên.
- Tên hằng `GiaoDiennAdd` sai chính tả (2 chữ n) nhưng **value** là `"Function.GiaoDienAdd"` đúng. Cứ dùng tên hằng như đang có.
- `PermissionAttribute` query DB mỗi request và **không cache**. Đừng gắn `[Permission]` lên endpoint bị gọi ở tần suất cao trong lúc trao bằng nếu không cần thiết.
