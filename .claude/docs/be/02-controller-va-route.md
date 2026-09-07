# 02 — Controller & Route

## Khung chuẩn của một controller

Copy nguyên khung này, đổi tên là xong:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.TraoBang.Dtos;
using traobang.be.application.TraoBang.Interfaces;   // hoặc .Interface — xem file gốc
using traobang.be.Attributes;
using traobang.be.Controllers.Base;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest;

namespace traobang.be.Controllers
{
    [Route("api/core/trao-bang/plan")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PlanController : BaseController
    {
        private readonly IPlanService _planService;

        public PlanController(ILogger<PlanController> logger, IPlanService planService) : base(logger)
        {
            _planService = planService;
        }

        [Permission(PermissionKeys.PlanAdd)]
        [HttpPost("")]
        public ApiResponse Create(CreatePlanDto dto)
        {
            try
            {
                _planService.Create(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
```

### Bốn thứ bắt buộc

1. Kế thừa `BaseController`, constructor nhận `ILogger<TênController>` truyền vào `base(logger)`.
2. Ba attribute ở mức class: `[Route(...)]`, `[ApiController]`, `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`.
3. Kiểu trả về là `ApiResponse` hoặc `Task<ApiResponse>` — **không dùng `IActionResult`** (trừ khi trả file, xem cuối file).
4. Toàn bộ thân action nằm trong `try { ... } catch (Exception ex) { return OkException(ex); }`.

### Cách return

| Trường hợp | Viết |
|---|---|
| Không có data trả về | `return new();` |
| Có data | `return new(result);` |
| Lỗi | `return OkException(ex);` |

Không bao giờ `return Ok(...)`, không `new ApiResponse(...)` với đầy đủ tham số trong controller.

## Đặt route

### Prefix theo nhóm

| Nhóm | Prefix | Ví dụ |
|---|---|---|
| Nghiệp vụ trao bằng | `api/core/trao-bang/<resource>` | `api/core/trao-bang/plan`, `.../sub-plan`, `.../slide`, `.../giao-dien`, `.../prepare` |
| Quản trị user/role | `api/app/<resource>` | `api/app/users`, `api/app/permissions` |
| Màn cấu hình | `api/config/<resource>` | `api/config/slide` |
| OpenIddict | `~/connect/token`, `/connect/authorize` | trong `AuthorizationController` |

Resource viết **kebab-case** số ít: `sub-plan`, `giao-dien`, `sinh-vien-nhan-bang`, `tien-do-trao-bang`.

> Lưu ý: `SlideController` (`api/core/trao-bang/slide`) và `ConfigSlideController` (`api/config/slide`) cùng thao tác lên `ISlideService` nhưng tách theo màn hình sử dụng: `config/*` cho màn cấu hình, `core/*` cho chức năng chạy trong lúc trao bằng.

### Route của action

```csharp
[HttpPost("")]                                          // POST   /api/core/trao-bang/plan
[HttpPut("{id}")]                                       // PUT    /.../plan/5
[HttpGet("")]                                           // GET    /.../plan?pageNumber=1&pageSize=10
[HttpGet("list")]                                       // GET    /.../plan/list      (không phân trang)
[HttpDelete("{id}")]                                    // DELETE /.../plan/5
[HttpDelete("{id}/config")]                             // hành động phụ trên 1 resource
[HttpPut("tien-do/order")]                              // sub-resource + hành động
[HttpPost("{idSubPlan}/sinh-vien-nhan-bang/next-trao-bang")]  // nested resource
```

Quy tắc rút ra:
- `""` cho collection root (list phân trang + create).
- `"list"` cho danh sách đầy đủ dùng để đổ dropdown.
- Tên hành động không phải CRUD viết kebab-case tiếng Việt không dấu: `next-sub-plan`, `hang-doi`, `trang-thai`, `thong-tin-subplan`.
- Không đặt tên method trùng route pattern — tên method là tiếng Anh (`FindPaging`, `FindById`, `Create`, `Update`, `Delete`, `ListPlan`).

### Binding tham số

```csharp
public ApiResponse Create(CreatePlanDto dto)                                  // body, không cần [FromBody]
public ApiResponse Update([FromRoute] int id, [FromBody] UpdatePlanDto dto)
public ApiResponse FindPaging([FromQuery] FindPagingPlanDto dto)              // BẮT BUỘC [FromQuery]
public ApiResponse Delete([FromRoute] int id)
public async Task<ApiResponse> DiemDanhNhanBang([FromQuery] string mssv)
public ApiResponse ImportSlide([FromForm] ImportExcelSlideDto dto)            // có IFormFile
```

`[FromRoute]` được viết tường minh gần như mọi nơi kể cả khi không cần — cứ theo.

## Gán permission

Xem chi tiết ở [05-permission-va-auth.md](05-permission-va-auth.md). Cú pháp tại controller:

```csharp
[Permission(PermissionKeys.PlanView)]                                   // 1 quyền
[Permission(PermissionKeys.DieuKhienView, PermissionKeys.CheckinView)]  // OR: có 1 trong 2 là qua
```

Thứ tự attribute không thống nhất trong repo (`[Permission]` khi trên khi dưới `[HttpGet]`) — không quan trọng.

### Endpoint không cần đăng nhập

Màn hình chiếu (sân khấu, cánh gà, trang tra cứu SV bằng QR) gọi API mà không có token → dùng `[AllowAnonymous]` **thay cho** `[Permission]`:

```csharp
[AllowAnonymous]
[HttpGet("infor-sinh-vien-dang-trao")]
public async Task<ApiResponse> GetInforSinhVienDangTrao()
```

Các endpoint đang `AllowAnonymous` trong `SubPlanController`: `sinh-vien-nhan-bang/{mssv}`, `.../next`, `.../prev`, `infor-sinh-vien-dang-trao`, `{idSubPlan}/sinh-vien-nhan-bang/next-trao-bang`, `.../prev-trao-bang`.

## XML doc comment

Có thì tốt, viết tiếng Việt, chỉ cần `<summary>`:

```csharp
/// <summary>
/// Lấy giao diện cho chương trình đang hoạt động
/// </summary>
/// <returns></returns>
[HttpGet("active-plan")]
```

Controller nghiệp vụ (`PlanController`, `SlideController`) hầu như không comment; controller Auth và Config thì có. Không bắt buộc.

## Trả file (ngoại lệ duy nhất không dùng ApiResponse)

```csharp
[HttpGet("export/template-import-slide")]
[Permission(PermissionKeys.SlideAdd)]
public IActionResult ExportTemplateImportSlide()
{
    try
    {
        var excelTemplate = _slideService.DownloadTemplateImport();
        return File(
            excelTemplate,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ImportSlide.xlsx"
        );
    }
    catch (Exception ex)
    {
        return BadRequest(new ApiResponse(ex.Message));
    }
}
```

Service trả `byte[]`, file mẫu đặt trong `traobang.be/Templates/` và khai báo `CopyToOutputDirectory=PreserveNewest` trong `.csproj`.

## Gotcha

- **`[ApiController]` + DTO có validation attribute = phá vỡ envelope.** Khi `ModelState` invalid, ASP.NET tự trả **HTTP 400 ProblemDetails**, không phải `ApiResponse`. FE chỉ đọc `res.status` nên sẽ không hiển thị được message. Không có `SuppressModelStateInvalidFilter` trong `Program.cs`. Vì vậy: validate quan trọng thì **check trong service rồi ném `UserFriendlyException`**, đừng dựa vào DataAnnotation.
- Không có global exception filter. Quên `try/catch` là exception rơi ra ngoài thành 500 trần, FE không hiển thị được gì.
- `PermissionController` và `UsersController` inject `ILogger<BaseController>` thay vì `ILogger<TênController>` — không sai nhưng log sẽ mang tên `BaseController`. Với controller mới nên dùng `ILogger<TênController>`.
