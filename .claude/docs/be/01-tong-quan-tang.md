# 01 — Tổng quan các tầng

## Cấu trúc project

Solution: `traobang.be/traobang.be/traobang.be.sln` (lưu ý file `.sln` nằm **trong** thư mục project web, sâu hơn một cấp).

```
traobang.be/
├── traobang.be/                        ← Web API (startup layer)
│   ├── Program.cs                        DI, auth, CORS, SignalR, Hangfire, Swagger, seed
│   ├── Controllers/                      Controllers (xem file 02)
│   │   ├── Base/BaseController.cs
│   │   ├── Auth/                         AuthorizationController, Users, Roles, Permission
│   │   ├── Config/                       ConfigSlideController
│   │   └── *.cs                          Plan, SubPlan, Slide, GiaoDien, PrepareData
│   ├── Attributes/PermissionAttribute.cs
│   ├── Workers/AuthWorker.cs             IHostedService seed OpenIddict client
│   └── Templates/*.xlsx                  File mẫu import (CopyToOutputDirectory)
│
├── traobang.be.application/            ← Business logic
│   ├── Base/BaseService.cs               Base cho mọi service
│   ├── Base/MappingProfile.cs            AutoMapper profile duy nhất
│   ├── Auth/{Dtos,Implements,Interfaces}
│   └── TraoBang/{Dtos,Implements,Interfaces}
│
├── traobang.be.infrastructure.data/    ← EF Core
│   ├── TbDbContext.cs
│   ├── Migrations/
│   └── Seeder/SeedUser.cs
│
├── traobang.be.infrastructure.external/← Tích hợp ngoài
│   ├── SignalR/{Hub,Service}
│   ├── Excel/                            IExcelService.ReadExcelFile(IFormFile, sheetName)
│   ├── File/                             IFileS3Services (MinIO)
│   ├── QrCode/                           IQrCodeService
│   └── BackgroundJob/                    Cấu hình Hangfire
│
├── traobang.be.domain/                 ← Entity thuần
│   ├── Auth/AppUser.cs
│   └── TraoBang/*.cs
│
└── traobang.be.shared/                 ← Dùng chung, không phụ thuộc ai
    ├── Constants/{Auth,Db,TraoBang}
    ├── HttpRequest/                      ApiResponse, AppException, BaseRequest, Error
    ├── Interfaces/IFullAudited.cs
    ├── Validations/                      IntegerRangeAttribute, StringRangeAttribute
    └── Utils/
```

## Chiều phụ thuộc

```
traobang.be  →  application  →  infrastructure.data  →  domain  →  shared
             →  infrastructure.external ────────────────┘
```

- `domain` và `shared` không tham chiếu ngược lên trên.
- `application` được phép dùng cả `infrastructure.data` (DbContext) và `infrastructure.external` (SignalR, Excel, S3, QR).
- **Không có repository layer.** Service thao tác trực tiếp với `TbDbContext`.

## Thêm file mới thì đặt ở đâu

| Thứ cần thêm | Vị trí |
|---|---|
| Endpoint mới | `traobang.be/Controllers/<Ten>Controller.cs` |
| Logic nghiệp vụ | `application/<Area>/Implements/<Ten>Service.cs` + `Interfaces/I<Ten>Service.cs` |
| DTO | `application/<Area>/Dtos/` (nhóm lớn thì tạo subfolder, xem file 04) |
| Entity | `domain/TraoBang/<Ten>.cs` + `DbSet` trong `TbDbContext` + cấu hình trong `OnModelCreating` |
| Mã lỗi | `shared/HttpRequest/Error/ErrorCodes.cs` + message trong `ErrorMessages.cs` |
| Permission key | `shared/Constants/Auth/PermissionKeys.cs` (cả const lẫn mảng `All`) |
| Hằng số trạng thái | `shared/Constants/TraoBang/TraoBangConstants.cs` |
| Tích hợp ngoài | `infrastructure.external/<TenDichVu>/` theo cặp `IXService` + `XService` |

## Gotcha

- Một số file trong `traobang.be.shared` khai báo namespace **`thongbao.be.*`** (ví dụ `IFullAudited.cs` / `ISoftDelted`) vì repo được fork từ codebase khác. Khi sửa file nào thì giữ nguyên namespace của file đó, đừng "sửa cho đúng" — sẽ vỡ hàng loạt `using`.
- `traobang.be/WeatherForecast.cs` là rác từ template, chưa xóa.
- `traobang.be.application/TraoBang/Interfaces` là folder thật, nhưng namespace của một số interface lại là `...TraoBang.Interface` (số ít) — `IPlanService`, `ISubPlanService` nằm ở namespace `Interface`, còn `IGiaoDienService`, `ISlideService`, `IPrepareDataService` ở `Interfaces`. Khi thêm `using` phải nhìn kỹ file gốc; `Program.cs` phải `using` cả hai.
