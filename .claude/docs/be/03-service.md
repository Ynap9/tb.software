# 03 — Service layer

## Khung chuẩn

Mỗi service là một cặp `I<Ten>Service` (folder `Interfaces`) + `<Ten>Service` (folder `Implements`), kế thừa `BaseService`.

```csharp
namespace traobang.be.application.TraoBang.Implements
{
    public class PlanService : BaseService, IPlanService
    {
        private static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public PlanService(
            TbDbContext tbDbContext,
            ILogger<PlanService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper
        )
            : base(tbDbContext, logger, httpContextAccessor, mapper)
        {
        }

        // ... methods

        private static DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }
    }
}
```

Dependency thêm thì nối vào **sau** 4 tham số base rồi gán vào field `private readonly`:

```csharp
private readonly ITraoBangService _traoBangService;
private readonly IExcelService _excelService;

public SubPlanService(
    TbDbContext tbDbContext,
    ILogger<SubPlanService> logger,
    IHttpContextAccessor httpContextAccessor,
    IExcelService excelService,
    IMapper mapper,
    IConfiguration configuration,
    ITraoBangService traoBangService
) : base(tbDbContext, logger, httpContextAccessor, mapper) { ... }
```

## `BaseService` cho sẵn những gì

| Thành viên | Dùng để |
|---|---|
| `_tbDbContext` | Truy vấn DB trực tiếp (không có repository) |
| `_logger` | Log |
| `_httpContextAccessor` | Lấy claim của user hiện tại |
| `_mapper` | AutoMapper |
| `getCurrentUserId()` | Lấy `NameIdentifier` / `sub` — id của user |
| `getCurrentName()` | Lấy claim `name` = `FullName`, dùng gán `CreatedBy` / `DeletedBy` |
| `IsSuperAdmin()` | Kiểm tra role super admin |

## Đăng ký DI — BẮT BUỘC

Không có assembly scanning. Thêm service mới thì phải tự thêm dòng vào `#region service` trong `Program.cs`:

```csharp
builder.Services.AddScoped<IPlanService, PlanService>();
```

Quên bước này → app crash lúc resolve controller.

## Các pattern bắt buộc trong method

### 1. Log ở dòng đầu tiên

```csharp
public void Create(CreatePlanDto dto)
{
    _logger.LogInformation($"{nameof(Create)}, dto = {JsonSerializer.Serialize(dto)}");
```

Biến thể theo tham số:

```csharp
_logger.LogInformation($"{nameof(Delete)}, id = {id}");
_logger.LogInformation($"{nameof(DiemDanhNhanBang)}, mssv= {mssv} ");
_logger.LogInformation($"{nameof(GetListPlan)}");
```

(Service trong `Auth/` dùng `dto=` không có khoảng trắng, `TraoBang/` dùng `, dto = `. Không quan trọng, miễn có log.)

### 2. Tìm entity → không thấy thì ném `UserFriendlyException`

Hai cách viết, cả hai đều dùng trong repo:

```csharp
var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == id && !x.Deleted);
if (plan == null)
{
    throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
}
```

```csharp
var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == idPlan && !x.Deleted)
    ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
```

**Mọi query đều phải có `!x.Deleted`.** Không có global query filter trong `TbDbContext`.

### 3. Soft delete

```csharp
plan.DeletedDate = vietnameNow;
plan.Deleted = true;
_tbDbContext.Plans.Update(plan);
_tbDbContext.SaveChanges();
```

Xóa hàng loạt thì gán thêm `DeletedBy = username` (`username = getCurrentName()`).

### 4. Create

```csharp
var vietnameNow = GetVietnamTime();
var plan = new domain.TraoBang.Plan
{
    Ten = dto.Ten,
    MoTa = dto.MoTa,
    CreatedDate = vietnameNow,
    TrangThai = TrangThaiPlan.KhoiTao,
    Deleted = false,
};
_tbDbContext.Plans.Add(plan);
_tbDbContext.SaveChanges();
```

Gán tường minh từng field (không `_mapper.Map` chiều DTO → entity, trừ `CreateSlideSinhVienDto`). Luôn set `Deleted = false` và `CreatedDate` dù DB đã có default.

Cần trả id vừa tạo thì trả DTO nhỏ:

```csharp
return new CreateResultDto { Id = giaoDien.Id };
```

### 5. Trường `Order` — luôn tính max + 1

```csharp
var maxOrder = _tbDbContext.SubPlans
    .Where(x => x.IdPlan == idPlan && !x.Deleted)
    .Max(x => (int?)x.Order) ?? 0;
// ...
Order = maxOrder + 1
```

Ép `(int?)` để tránh exception khi bảng rỗng.

### 6. Phân trang

```csharp
public BaseResponsePagingDto<ViewPlanDto> FindPaging(FindPagingPlanDto dto)
{
    _logger.LogInformation($"{nameof(FindPaging)}, dto = {JsonSerializer.Serialize(dto)}");

    var query = from p in _tbDbContext.Plans
                where !p.Deleted
                orderby p.CreatedDate descending
                select p;

    var data = query.Paging(dto).ToList();
    var items = _mapper.Map<List<ViewPlanDto>>(data);

    return new BaseResponsePagingDto<ViewPlanDto>
    {
        TotalItems = query.Count(),
        Items = items
    };
}
```

- `.Paging(dto)` là extension trong `shared/HttpRequest/BaseRequest/PagingExtension.cs`. Nếu `PageSize == -1` thì **không phân trang** (trả hết).
- `TotalItems` đếm trên query **trước** khi `Paging` — đúng, nhưng chạy thêm 1 query nữa.
- Query kiểu LINQ query-syntax (`from ... where ... select`) dùng phổ biến ở tầng TraoBang; method-syntax (`.Where().OrderBy()`) dùng ở tầng Auth. Cả hai đều chấp nhận được.

### 7. Danh sách không phân trang → projection thẳng, `AsNoTracking`

```csharp
var plans = await _tbDbContext.Plans
    .AsNoTracking()
    .Where(x => !x.Deleted)
    .OrderByDescending(x => x.CreatedDate)
    .Select(x => new GetListPlanResponseDto
    {
        Id = x.Id,
        Ten = x.Ten,
        TrangThai = x.TrangThai,
    })
    .ToListAsync();
```

### 8. Transaction

Dùng khi sửa nhiều bảng hoặc nhiều bản ghi cùng lúc:

```csharp
using (var tran = _tbDbContext.Database.BeginTransaction())
{
    // ... thay đổi
    _tbDbContext.SaveChanges();
    tran.Commit();
}
```

Bản async (tầng Auth):

```csharp
var trans = await _tbDbContext.Database.BeginTransactionAsync();
// ...
await _tbDbContext.SaveChangesAsync();
await trans.CommitAsync();
```

Không có `Rollback` tường minh ở đâu cả — dựa vào `Dispose` của `using`. Lưu ý bản async ở tầng Auth **không** có `using` nên nếu ném exception giữa chừng thì transaction không được dispose ngay; đây là code hiện có, giữ nguyên style khi sửa cùng file.

### 9. Bắn SignalR sau khi lưu

Thứ tự luôn là: **thay đổi DB → `SaveChanges` (→ `Commit`) → `Notify`**.

```csharp
_tbDbContext.TienDoTraoBangs.Add(tienDoTraoBang);
_tbDbContext.SaveChanges();

await _traoBangService.NotifyCheckIn();

return new DiemDanhNhanBangDto { ... };
```

Ba notify hiện có (`ITraoBangService`):

| Method | Bắn khi |
|---|---|
| `NotifyCheckIn()` | Có SV được đưa vào/thay đổi hàng đợi (màn checkin, điều khiển) |
| `NotifyChonKhoa()` | Chuyển sang khoa (SubPlan) khác |
| `NotifySinhVienDangTrao()` | Đổi SV đang được trao bằng (màn sân khấu, cánh gà) |

Method nào có notify thì phải là `async Task` / `async Task<T>`.

### 10. Sync hay async?

Trong repo: **mặc định viết sync** (`void`, `BaseResponsePagingDto<T>`). Chỉ dùng `async Task` khi thật sự cần `await`:
- gọi `_traoBangService.Notify*`
- gọi `UserManager` / `RoleManager` (Identity toàn async)
- dùng `ToListAsync` / `BulkInsertAsync`

Không async hóa method chỉ vì "cho hiện đại" — sẽ lệch với phần còn lại.

## Thời gian

```csharp
private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
private static DateTime GetVietnamTime() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
```

Khối này bị **copy lặp lại** trong `PlanService`, `GiaoDienService`, `SubPlanService`, `SlideService`. Service mới thì copy tiếp cho đồng bộ. Một vài chỗ (`PlanService.DeleteConfig`, `TienDoTraoBang.CreatedDate`) dùng `DateTime.Now` trực tiếp — ưu tiên `GetVietnamTime()` cho code mới.

## Tích hợp ngoài

```csharp
_excelService.ReadExcelFile(dto.File, "Sheet1")     // -> List<List<string>>
_qrCodeService.GenerateQrWithText(content, notice)  // -> Stream
await _fileS3Service.WriteStreamFileAsync(filename, qrcode)
sv.LinkQR = $"{_fileS3Config.BucketName}/{filename}";
```

Import số lượng lớn dùng EFCore.BulkExtensions:

```csharp
await _tbDbContext.BulkInsertAsync(danhSachList);
```

Cập nhật hàng loạt một cột dùng `ExecuteUpdate`:

```csharp
_tbDbContext.TienDoTraoBangs
    .Where(...)
    .ExecuteUpdate(setter => setter.SetProperty(x => x.Order, x => x.Order + 1));
```

## Validate nghiệp vụ nằm ở service

```csharp
if (dto.LoaiSlide == LoaiSlides.TEXT && string.IsNullOrEmpty(dto.NoiDung))
{
    throw new UserFriendlyException(ErrorCodes.TraoBangErrorLoaiSlideBinhThuongPhaiCoNoiDung);
}
if (dto.LoaiSlide == LoaiSlides.SINH_VIEN && dto.SinhVien == null)
{
    throw new UserFriendlyException(ErrorCodes.TraoBangErrorLoaiSlideSinhVienPhaiCoSinhVien);
}
```

Lý do: DataAnnotation trên DTO sẽ khiến `[ApiController]` trả 400 ProblemDetails, không đi qua `ApiResponse` (xem file 02).
