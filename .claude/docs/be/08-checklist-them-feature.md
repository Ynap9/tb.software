# 08 — Checklist thêm một feature mới

Ví dụ xuyên suốt: thêm resource **`ThongBao`** (thông báo hiển thị trên màn hình) với CRUD + phân trang.

## 1. Entity

`traobang.be.domain/TraoBang/ThongBao.cs`

```csharp
[Table(nameof(ThongBao), Schema = DbSchemas.TraoBang)]
[Index(nameof(Id), IsUnique = false, Name = $"IX_{nameof(ThongBao)}")]
public class ThongBao : ISoftDelted
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int IdPlan { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsShow { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }
    public bool Deleted { get; set; }
    public string? DeletedBy { get; set; }
}
```

## 2. DbContext

`infrastructure.data/TbDbContext.cs` — thêm `DbSet` + cấu hình default:

```csharp
public DbSet<ThongBao> ThongBaos { get; set; }

// trong OnModelCreating
modelBuilder.Entity<ThongBao>(entity =>
{
    entity.Property(e => e.Deleted).HasDefaultValue(0);
    entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
});
```

## 3. Migration

```bash
cd traobang.be
dotnet ef migrations add AddThongBao -p traobang.be.infrastructure.data -s traobang.be
dotnet ef database update           -p traobang.be.infrastructure.data -s traobang.be
```

Tên migration trong repo không thống nhất (`AddGiaoDienMigration`, `update-tbl`, `qrinfo`) — đặt tên mô tả được là được.

## 4. DTO

`application/TraoBang/Dtos/ThongBao/` — namespace `traobang.be.application.TraoBang.Dtos.ThongBao`

```csharp
public class CreateThongBaoDto
{
    public int IdPlan { get; set; }
    public required string NoiDung { get; set; }
    public bool IsShow { get; set; }
}

// DTO trong repo không kế thừa nhau — khai lại đầy đủ field
public class UpdateThongBaoDto
{
    public int Id { get; set; }
    public required string NoiDung { get; set; }
    public bool IsShow { get; set; }
}

public class FindPagingThongBaoDto : BaseRequestPagingDto
{
    public int? IdPlan { get; set; }
}

public class ViewThongBaoDto
{
    public int Id { get; set; }
    public int IdPlan { get; set; }
    public string NoiDung { get; set; } = String.Empty;
    public int Order { get; set; }
    public bool IsShow { get; set; }
    public DateTime? CreatedDate { get; set; }
}
```

## 5. AutoMapper

`application/Base/MappingProfile.cs`

```csharp
CreateMap<ThongBao, ViewThongBaoDto>();
```

## 6. Mã lỗi

`shared/HttpRequest/Error/ErrorCodes.cs`

```csharp
public const int TraoBangErrorThongBaoNotFound = 1201;
```

`ErrorMessages.cs`

```csharp
{ ErrorCodes.TraoBangErrorThongBaoNotFound, "Không tìm thấy thông báo" },
```

## 7. Permission

`shared/Constants/Auth/PermissionKeys.cs` — **cả hai chỗ**:

```csharp
#region chức năng trong menu cấu hình thông báo
public const string CategoryCauHinhThongBao = "QL ThongBao";
public const string ThongBaoAdd = Function + "ThongBaoAdd";
public const string ThongBaoUpdate = Function + "ThongBaoUpdate";
public const string ThongBaoDelete = Function + "ThongBaoDelete";
public const string ThongBaoView = Function + "ThongBaoView";
#endregion

// trong mảng All
(ThongBaoAdd,    "[Cấu hình Thông báo] Thêm",     CategoryCauHinhThongBao),
(ThongBaoUpdate, "[Cấu hình Thông báo] Cập nhật", CategoryCauHinhThongBao),
(ThongBaoDelete, "[Cấu hình Thông báo] Xoá",      CategoryCauHinhThongBao),
(ThongBaoView,   "[Cấu hình Thông báo] Xem",      CategoryCauHinhThongBao),
```

## 8. Interface + Service

`application/TraoBang/Interfaces/IThongBaoService.cs`

```csharp
namespace traobang.be.application.TraoBang.Interfaces
{
    public interface IThongBaoService
    {
        public void Create(CreateThongBaoDto dto);
        public void Update(UpdateThongBaoDto dto);
        public void Delete(int id);
        public BaseResponsePagingDto<ViewThongBaoDto> FindPaging(FindPagingThongBaoDto dto);
        public ViewThongBaoDto FindById(int id);
    }
}
```

(Repo khai báo `public` cả trong interface — giữ nguyên style đó.)

`application/TraoBang/Implements/ThongBaoService.cs` — theo khung ở [03-service.md](03-service.md): kế thừa `BaseService`, log dòng đầu, `!x.Deleted` mọi query, `Order = maxOrder + 1`, soft delete, `UserFriendlyException` khi không tìm thấy.

## 9. Đăng ký DI

`Program.cs`, trong `#region service`:

```csharp
builder.Services.AddScoped<IThongBaoService, ThongBaoService>();
```

## 10. Controller

`traobang.be/Controllers/ThongBaoController.cs` — theo khung ở [02-controller-va-route.md](02-controller-va-route.md):

```csharp
[Route("api/core/trao-bang/thong-bao")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ThongBaoController : BaseController
{
    private readonly IThongBaoService _thongBaoService;

    public ThongBaoController(ILogger<ThongBaoController> logger, IThongBaoService thongBaoService) : base(logger)
    {
        _thongBaoService = thongBaoService;
    }

    [Permission(PermissionKeys.ThongBaoAdd)]
    [HttpPost("")]
    public ApiResponse Create(CreateThongBaoDto dto)
    {
        try { _thongBaoService.Create(dto); return new(); }
        catch (Exception ex) { return OkException(ex); }
    }
    // ... Update / FindPaging / FindById / Delete
}
```

## 11. Nếu có real-time

Màn hình chiếu cần cập nhật ngay khi dữ liệu đổi:

1. `infrastructure.external/SignalR/Hub/Interfaces/ITraoBangHub.cs` → thêm `Task ReceiveThongBao();`
2. `Service/Interfaces/ITraoBangService.cs` + `Implements/TraoBangService.cs` → thêm `NotifyThongBao()` gọi `_hubContext.Clients.All.ReceiveThongBao()`
3. Service nghiệp vụ: inject `ITraoBangService`, đổi method thành `async Task`, gọi `await _traoBangService.NotifyThongBao()` **sau** `SaveChanges`/`Commit`
4. FE: thêm hằng vào `TraoBangHubConst` (`src/app/shared/constants/sv-nhan-bang.constants.ts`) — **tên phải khớp chính xác chuỗi ở BE**

## 12. Đồng bộ FE

| BE | FE |
|---|---|
| `PermissionKeys` | `src/app/shared/constants/permission.constants.ts` |
| `ITraoBangHub` event | `TraoBangHubConst` trong `sv-nhan-bang.constants.ts` |
| Route + DTO | `src/app/service/<resource>.service.ts` (`api = '/api/core/trao-bang/thong-bao'`) + `src/app/models/traobang/<resource>.models.ts` |

## Checklist rút gọn

- [ ] Entity + `ISoftDelted` + `[Table(Schema = DbSchemas.TraoBang)]`
- [ ] `DbSet` + `OnModelCreating`
- [ ] Migration đã add & update
- [ ] DTO (Create / Update / FindPaging / View) trong subfolder riêng
- [ ] `CreateMap` trong `MappingProfile`
- [ ] `ErrorCodes` + `ErrorMessages`
- [ ] `PermissionKeys`: const **và** mảng `All`
- [ ] Interface + Service (log, `!Deleted`, soft delete, `UserFriendlyException`)
- [ ] **`AddScoped` trong `Program.cs`**
- [ ] Controller (`ApiResponse`, `try/catch OkException`, `[Permission]`)
- [ ] SignalR nếu cần, notify sau `SaveChanges`
- [ ] Đồng bộ hằng số sang FE
- [ ] Báo lại cho user những chỗ cần chú ý khi build (**không tự chạy `dotnet build`**)
