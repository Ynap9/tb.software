# 04 — DTO

## Nơi đặt

`traobang.be.application/<Area>/Dtos/`

```
Auth/Dtos/
├── Permission/ViewPermissionDto.cs
├── Role/{CreateRoleDto, UpdateRoleDto, FindPagingRoleDto, ViewRoleDto}.cs
└── User/{CreateUserDto, UpdateUserDto, FindPagingUserDto, SetRoleForUserDto, ViewUserDto, ViewMeDto}.cs

TraoBang/Dtos/
├── *.cs                      ← DTO của Plan / SubPlan / SinhVienNhanBang / TienDo nằm phẳng ở đây
├── GiaoDien/                 ← nhóm mới thì có subfolder riêng
├── Slide/
├── SubPlan/                  (một vài file, phần lớn SubPlan vẫn nằm phẳng)
└── PrepareData/
```

Quy tắc thực tế: **nhóm nào tạo sau thì có subfolder**, nhóm cũ (Plan, SubPlan, SinhVienNhanBang) nằm phẳng. Với feature mới → **tạo subfolder theo tên resource**, namespace theo folder: `traobang.be.application.TraoBang.Dtos.GiaoDien`.

Một file có thể chứa nhiều class DTO liên quan (`ImportSlideDto.cs` chứa cả `ImportExcelSlideDto` và `ImportExcelMapSlideSinhVienDto`) — tên file **không bắt buộc trùng tên class**.

## Phân loại và cách đặt tên

| Prefix | Dùng cho | Ví dụ |
|---|---|---|
| `Create...Dto` | Body của POST | `CreatePlanDto`, `CreateSubPlanDto`, `CreateSlideDto` |
| `Update...Dto` | Body của PUT | `UpdatePlanDto`, `UpdateSubPlanIsShowDto` |
| `FindPaging...Dto` | Query string của GET list phân trang | `FindPagingPlanDto` |
| `View...Dto` | Object trả về cho FE | `ViewPlanDto`, `ViewSinhVienNhanBangDto` |
| `Get...ResponseDto` | Kết quả của một action cụ thể, không phải view chung | `GetListPlanResponseDto`, `GetNextSubPlanResponseDto` |
| `...RequestDto` | Input phức tạp không phải Create/Update | `ViewTienDoNhanBangRequestDto` |
| `Import...Dto` | Upload file / import | `ImportExcelSlideDto`, `ImportDanhSachSinhVienNhanBangDto` |

Hậu tố `Dto` là bắt buộc ở mọi class.

## Ví dụ theo từng loại

### Create — plain properties, khởi tạo mặc định

```csharp
namespace traobang.be.application.TraoBang.Dtos
{
    public class CreatePlanDto
    {
        public string Ten { get; set; } = String.Empty;
        public string MoTa { get; set; } = String.Empty;
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
    }
}
```

Convention: `string` không nullable thì **luôn** khởi tạo `= String.Empty;` (viết hoa `String`, không phải `string.Empty` — style trong repo dùng cả hai nhưng `String.Empty` phổ biến hơn ở TraoBang).

### Update — thêm validate trạng thái

```csharp
using traobang.be.shared.Constants.TraoBang;
using traobang.be.shared.Validations;

public class UpdatePlanDto
{
    public string Ten { get; set; } = String.Empty;
    public DateTime? ThoiGianBatDau { get; set; }

    [IntegerRange(AllowableValues = new int[] { TrangThaiPlan.KhoiTao, TrangThaiPlan.DaKetThuc, TrangThaiPlan.DangHoatDong })]
    public int TrangThai { get; set; }
}
```

`IntegerRangeAttribute` / `StringRangeAttribute` nằm trong `shared/Validations/`. **Nhưng xem cảnh báo ở cuối file.**

### FindPaging — kế thừa `BaseRequestPagingDto`

```csharp
public class FindPagingPlanDto : BaseRequestPagingDto
{
}
```

`BaseRequestPagingDto` cho sẵn:

| Property | Query param | Ghi chú |
|---|---|---|
| `PageSize` | `pageSize` | `-1` = lấy hết, không phân trang |
| `PageNumber` | `pageNumber` | bắt đầu từ 1 |
| `Keyword` | `keyword` | tự `Trim()` trong setter |
| `Sort` | — | `List<string>`, **hiện chưa chỗ nào dùng** |
| `GetSkip()` | — | `(PageNumber - 1) * PageSize`, chặn âm |

DTO con chỉ thêm field lọc riêng:

```csharp
public class FindPagingSinhVienNhanBangDto : BaseRequestPagingDto
{
    public int IdSubPlan { get; set; }
}
```

Controller phải khai báo `[FromQuery]` thì `BaseRequestPagingDto` mới bind đúng (các property đã gắn sẵn `[FromQuery(Name = "...")]`).

### View — phẳng, không lồng entity

```csharp
public class ViewPlanDto
{
    public int Id { get; set; }
    public string Ten { get; set; } = String.Empty;
    public string MoTa { get; set; } = String.Empty;
    public int TrangThai { get; set; }
    public int? IdGiaoDien { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

`View...Dto` là đích của AutoMapper. Phải đăng ký trong `application/Base/MappingProfile.cs`:

```csharp
CreateMap<Plan, ViewPlanDto>();
CreateMap<GiaoDien, ViewGiaoDienDto>();
```

Không có `ReverseMap`, không có `ForMember` — chỉ map theo tên trùng nhau. Field nào tên không khớp thì gán tay sau khi map (xem `RoleService.FindById` gán `data.PermissionKey`).

### DTO có `required` + trim trong setter

Style mới hơn (`GiaoDien`, `Slide`):

```csharp
public class CreateGiaoDienDto
{
    private string _tenGiaoDien = string.Empty;
    public required string NoiDung { get; set; }
    public required string TenGiaoDien
    {
        get => _tenGiaoDien;
        set => _tenGiaoDien = value?.Trim() ?? string.Empty;
    }

    private string? _moTa;
    public string? MoTa
    {
        get => _moTa;
        set => _moTa = value?.Trim();
    }
}
```

```csharp
public class ImportExcelSlideDto
{
    required public int IdPlan { get; set; }
    required public IFormFile File { get; set; }
}
```

Dùng backing field + `Trim()` khi input là text người dùng nhập tay (tên, mô tả). Với DTO upload file thì kiểu `IFormFile` + `[FromForm]` ở controller.

### Response DTO nhỏ cho một action

```csharp
public class CreateResultDto
{
    public int Id { get; set; }
}

public class GetListPlanResponseDto
{
    public int Id { get; set; }
    public string Ten { get; set; } = String.Empty;
    public int TrangThai { get; set; }
}
```

Danh sách đổ dropdown thì chỉ trả 2–4 field, không tái dùng `View...Dto` đầy đủ.

## Cảnh báo về validation attribute

`[IntegerRange]`, `[Required]`… chạy qua `ModelState` của `[ApiController]` → khi fail, response là **HTTP 400 ProblemDetails**, không đi qua `ApiResponse`. FE chỉ đọc `res.status === 1` nên sẽ hiển thị sai hoặc không hiển thị gì.

Vì vậy:
- Validate quan trọng (ảnh hưởng nghiệp vụ) → **check trong service, ném `UserFriendlyException`**.
- DataAnnotation chỉ nên coi là lớp phòng thủ phụ.
