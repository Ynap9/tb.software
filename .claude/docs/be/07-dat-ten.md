# 07 — Quy ước đặt tên

## Nguyên tắc chung

- **Định danh code**: tiếng Việt **không dấu**, PascalCase — `HoVaTen`, `MaSoSinhVien`, `TrangThai`, `ThoiGianBatDau`, `DanhSachSinhVienNhanBang`.
- **Comment, message lỗi, `<summary>`**: tiếng Việt **có dấu**.
- **Method kỹ thuật CRUD**: tiếng Anh — `Create`, `Update`, `Delete`, `FindPaging`, `FindById`, `GetList`.
- **Method nghiệp vụ**: tiếng Việt không dấu — `DiemDanhNhanBang`, `NextSinhVienTraoBang`, `GetTienDoTraoBang`, `CutSlideNormal`.

Trộn Anh–Việt trong một tên là bình thường ở repo này: `GetInforSinhVienChuanBiDuocTraoBangResponseDto`, `UpdateTrangThaiSubPlan`, `ImportDanhSachSinhVienNhanBang`.

## Bảng tra nhanh

| Loại | Quy tắc | Ví dụ |
|---|---|---|
| Project | `traobang.be.<tầng>` (lowercase) | `traobang.be.infrastructure.data` |
| Namespace | Theo đường dẫn folder | `traobang.be.application.TraoBang.Dtos.Slide` |
| Controller | `<Resource>Controller` | `PlanController`, `ConfigSlideController` |
| Service | `<Resource>Service` + `I<Resource>Service` | `SubPlanService` / `ISubPlanService` |
| DTO | `<Loại><Resource>Dto` | `CreatePlanDto`, `ViewSinhVienNhanBangDto` |
| Entity | Danh từ số ít | `Plan`, `Slide`, `TienDoTraoBang` |
| `DbSet` | Số nhiều của entity | `Plans`, `Slides`, `DanhSachSinhVienNhanBangs` |
| Route resource | kebab-case, số ít | `sub-plan`, `giao-dien`, `tien-do-trao-bang` |
| Constant class | `<Nhóm>Constants` / `<Nhóm>Keys` | `TraoBangConstants`, `PermissionKeys` |
| Constant value | UPPER_SNAKE hoặc PascalCase (không nhất quán) | `ROLE_SUPER_ADMIN`, `TrangThaiPlan.KhoiTao` |
| Mã lỗi | `<Nhóm>Error<MôTả>` | `TraoBangErrorPlanNotFound` |
| Permission | `Function + "<Resource><HànhĐộng>"` | `Function.PlanAdd` |

## Field & biến

```csharp
private readonly IPlanService _planService;      // DI field: _camelCase
public readonly TbDbContext _tbDbContext;        // (BaseService để public — không bắt chước cho class mới)
private string _tenGiaoDien = string.Empty;      // backing field: _camelCase

var vietnameNow = GetVietnamTime();              // biến local: camelCase
var maxOrder = ...;
var activePlan = ...;
```

Method của `BaseService` dùng **camelCase** (`getCurrentUserId`, `getCurrentName`) — lệch chuẩn C# nhưng đang là như vậy, gọi đúng tên đó.

## Tiền tố Id

Khóa ngoại luôn là `Id<Entity>`, không phải `<Entity>Id`:

```csharp
public int IdSubPlan { get; set; }
public int IdPlan { get; set; }
public int? IdSinhVienNhanBang { get; set; }
public int IdSlide { get; set; }
public int? IdGiaoDien { get; set; }
```

Tham số route theo cùng quy ước: `[HttpDelete("{idSubPlan}/plan/{idPlan}")]`.

## Từ vựng nghiệp vụ

| Tiếng Việt | Trong code | Nghĩa |
|---|---|---|
| Chương trình / kế hoạch | `Plan` | Một buổi lễ trao bằng |
| Khoa | `SubPlan` | Một khoa trong buổi lễ |
| Sinh viên nhận bằng | `DanhSachSinhVienNhanBang`, `sv` | Bản ghi sinh viên |
| Hàng đợi / tiến độ | `TienDoTraoBang` | Hàng đợi đang chạy trên màn hình |
| Slide | `Slide` | Slide text hoặc slide sinh viên |
| Giao diện | `GiaoDien` | Template màn hình (GrapesJS) |
| Điểm danh / check-in | `DiemDanhNhanBang` | Quét QR đưa SV vào hàng đợi |
| Cánh gà | `SideScreen` (FE) | Màn hình chờ bên cạnh sân khấu |
| Sân khấu | `MainScreen` / `SanKhauMain` (FE) | Màn hình chiếu chính |
| Điều khiển | `McScreen` (FE) | Màn hình MC điều khiển |

Viết tắt hay gặp: `sv` (sinh viên), `mssv` (mã số sinh viên), `sp` (subplan), `sl` (slide), `qr`.

## Hằng số trạng thái

Không dùng `enum`, dùng `const int` trong `shared/Constants/TraoBang/TraoBangConstants.cs`:

```csharp
public static class TraoBangConstants   // trạng thái slide / sinh viên trong hàng đợi
{
    public const int XepHang = 1;        // trong hàng đợi
    public const int ChuanBi = 2;        // chuẩn bị lên bục
    public const int DangTraoBang = 3;
    public const int DaTraoBang = 4;
    public const int ThamGiaTraoBang = 5;// sẽ tham gia (không dùng trong bảng tiến độ)
    public const int VangMat = 6;
}

public static class TrangThaiSubPlan { /* 1..6, giá trị giống trên */ }
public static class TrangThaiPlan   { KhoiTao = 1, DangHoatDong = 2, DaKetThuc = 3 }
public static class LoaiSlides      { TEXT = 1, SINH_VIEN = 2 }
public static class ViewSvTypeConstants { SV = 1, MO_BAI = 2, KET_BAI = 3 }
```

Entity tham chiếu bằng XML doc để IDE nhảy được:

```csharp
/// <summary>
/// <see cref="LoaiSlides"/>
/// </summary>
public int LoaiSlide { get; set; }
```

Thêm trạng thái mới → thêm `const` ở đây, **không hardcode số** trong service.

## Entity — khung chuẩn

```csharp
using thongbao.be.shared.Interfaces;    // chú ý namespace thongbao.*
using traobang.be.shared.Constants.Db;

namespace traobang.be.domain.TraoBang
{
    [Table(nameof(Slide), Schema = DbSchemas.TraoBang)]
    [Index(nameof(Id), IsUnique = false, Name = $"IX_{nameof(Slide)}")]
    public class Slide : ISoftDelted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int IdSubPlan { get; set; }
        public string? NoiDung { get; set; }
        public int TrangThai { get; set; }
        public int Order { get; set; }
        public bool IsShow { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
        public string? DeletedBy { get; set; }
    }
}
```

- 5 field audit ở cuối, implement `ISoftDelted` (hoặc `IFullAudited` nếu cần `ModifiedBy/ModifiedDate`).
- `[Table(nameof(X), Schema = DbSchemas.TraoBang)]` — schema `tb` cho bảng nghiệp vụ; default schema của DbContext là `core`.
- Default value (`getdate()`, `Deleted = 0`) khai trong `TbDbContext.OnModelCreating`, **không** dùng attribute.
