# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Đây là gì

"Trao bằng" — phần mềm điều hành lễ trao bằng tốt nghiệp trực tiếp (HUCE). Người điều khiển (màn hình MC) đẩy buổi lễ tiến từng sinh viên; màn hình sân khấu, màn hình cánh gà và trạm check-in QR đều bám theo real-time qua SignalR. Thuật ngữ nghiệp vụ dùng tiếng Việt xuyên suốt code, constants và commit message — cứ tiếp tục viết như vậy.

Hai app độc lập trong cùng một repo:

- `traobang.be/` — ASP.NET Core 8 Web API (SQL Server, EF Core, OpenIddict, SignalR, Hangfire, MinIO)
- `traobang.fe/` — Angular 20 SPA (PrimeNG "Sakai" template, Tailwind 4, GrapesJS)
- `deploy/` — docker-compose build cả hai image

## Commands

### Backend (`traobang.be/`)

File solution nằm sâu hơn một cấp so với dự đoán: `traobang.be/traobang.be/traobang.be.sln`.

```bash
cd traobang.be/traobang.be
dotnet build traobang.be.sln
dotnet run                      # http://localhost:5165, Swagger tại /swagger (chỉ ở Development)
```

EF Core migrations — `TbDbContext` nằm trong `traobang.be.infrastructure.data`, nên luôn phải truyền cả hai project:

```bash
cd traobang.be
dotnet ef migrations add <Name> -p traobang.be.infrastructure.data -s traobang.be
dotnet ef database update       -p traobang.be.infrastructure.data -s traobang.be
```

Không có test project.

### Frontend (`traobang.fe/`)

```bash
npm start                       # ng serve → http://localhost:4200 (config development)
npm run build                   # production
npm run build:staging
npm run format                  # prettier (4 space, single quote, printWidth 250)
npm test                        # Karma + Jasmine
npx ng test --include='**/data-table.spec.ts'   # chạy một spec đơn lẻ
```

`eslint.config.js` đang ở format legacy và các plugin của nó chưa được cài — không có script `lint` nào chạy được. Format chỉ được đảm bảo bằng Prettier.

### Deploy

```bash
cd deploy && cp .env.example .env   # điền APP_ENV / APP_FE_ENV / APP_BE_ENV + INFISICAL_*
docker compose up -d --build        # FE :7200, BE :7100
```

## Tài liệu convention chi tiết

Trước khi viết code, đọc thư mục tương ứng: `.claude/docs/be/` cho backend, `.claude/docs/fe/` cho frontend.

**Khi code: bám đúng style và pattern sẵn có, không tự bịa cách làm mới.** Mở file cùng loại đang có ra copy khung rồi sửa tên. Không thêm layer/abstraction/thư viện repo chưa dùng, không refactor code đang chạy được khi không được yêu cầu. Thấy chỗ đáng sửa thì nói ra, đừng tự làm.

**Không tự chạy build để kiểm tra.** Không chạy `dotnet build` hay `npm run build` sau khi sửa code — user tự build tay cả BE lẫn FE. Sửa xong thì báo cáo thay đổi và nêu rõ chỗ cần chú ý, không tự verify bằng build. Chỉ build khi được yêu cầu rõ ràng.

### Backend — `.claude/docs/be/`

| File | Nội dung |
|---|---|
| `README.md` | Index + quy tắc "không tự bịa" + 5 quy tắc quan trọng nhất |
| `01-tong-quan-tang.md` | 6 project, chiều phụ thuộc, file nào đặt ở đâu |
| `02-controller-va-route.md` | Khung controller, đặt route, `[Permission]`, upload/download |
| `03-service.md` | Khung service, DI, logging, transaction, soft delete, SignalR |
| `04-dto.md` | Phân loại DTO, đặt tên, paging, validation |
| `05-permission-va-auth.md` | `PermissionKeys`, `PermissionAttribute`, role, token OpenIddict |
| `06-error-va-response.md` | `ApiResponse`, `ErrorCodes`, `UserFriendlyException` |
| `07-dat-ten.md` | Quy ước đặt tên + từ vựng nghiệp vụ + hằng số trạng thái |
| `08-checklist-them-feature.md` | Checklist end-to-end thêm resource mới |

### Frontend — `.claude/docs/fe/`

| File | Nội dung |
|---|---|
| `README.md` | Index + quy tắc "không tự bịa" + 5 quy tắc quan trọng nhất |
| `01-tong-quan.md` | Stack, cấu trúc thư mục, alias `@/`, bootstrap, layout, environment |
| `02-component.md` | Tạo component: trang danh sách, dialog, tbl-action, upload, màn real-time |
| `03-route-va-permission.md` | Route, lazy load, `authGuard`/`permissionGuard`, `SharedService`, menu |
| `04-service-goi-api.md` | Khung service, envelope, interceptor, upload/download, auth flow |
| `05-base-component-va-data-table.md` | API của `BaseComponent` và `DataTable` |
| `06-models-va-constants.md` | Interface model, class constants, `Utils`, directives |
| `07-dat-ten.md` | Quy ước đặt tên file/class/selector/biến + từ vựng nghiệp vụ |
| `08-checklist-them-man-hinh.md` | Checklist end-to-end thêm màn hình mới |

## Architecture

### Phân tầng backend

Sáu project; dependency chỉ chảy vào trong:

```
traobang.be (Controllers, Program.cs, Attributes, Workers)
  → traobang.be.application            (service + DTO, tách Auth/ và TraoBang/)
  → traobang.be.infrastructure.data    (TbDbContext, Migrations, Seeder)
  → traobang.be.infrastructure.external(SignalR, Excel, MinIO File, QrCode, Hangfire)
  → traobang.be.domain                 (entities)
  → traobang.be.shared                 (constants, ApiResponse, exceptions, paging)
```

Quy ước cần theo khi thêm một endpoint:

- Service đặt trong `application/<Area>/Implements` + `Interfaces`, kế thừa `BaseService` (có sẵn `_tbDbContext`, `_mapper`, `_logger`, `getCurrentUserId()`, `IsSuperAdmin()`), và **phải đăng ký thủ công** trong block `#region service` của `Program.cs` — không có assembly scanning.
- Controller kế thừa `BaseController`, route theo dạng `api/core/trao-bang/<resource>`, trả về `ApiResponse` (không dùng `IActionResult`), và bọc toàn bộ thân action trong `try/catch` trả `OkException(ex)`. Lỗi được trả về dưới dạng **HTTP 200 kèm `status: 0`** trong envelope — frontend kiểm tra `res.status === 1` chứ không nhìn HTTP status code.
- Ném `UserFriendlyException` cho các lỗi lường trước được; code/message nằm ở `shared/HttpRequest/Error`.
- Phân quyền dùng `[Permission(PermissionKeys.X)]` (một `IAuthorizationFilter` tự viết, query thẳng role claim từ DB), đặt trên nền `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` ở mức class. `ROLE_SUPER_ADMIN` bỏ qua mọi permission check.
- Entity dùng soft delete (`Deleted`, `DeletedDate`, `DeletedBy`) và default `getdate()` được cấu hình trong `TbDbContext.OnModelCreating` chứ không phải bằng attribute. Schema mặc định là `core`; các bảng của buổi lễ được ghim vào schema `tb` qua `[Table(..., Schema = DbSchemas.TraoBang)]`.

### State machine của buổi lễ

`Plan` (một buổi lễ) → `SubPlan` (một khoa) → `DanhSachSinhVienNhanBang` (danh sách sinh viên) → `TienDoTraoBang` (hàng đợi tiến độ trực tiếp mà các màn hình thực sự đọc) và `Slide` (slide text/sinh viên theo từng khoa). `GiaoDien` lưu template màn hình được tạo bằng GrapesJS.

Các giá trị trạng thái (số nguyên) của tất cả những thứ trên đều lấy từ `shared/Constants/TraoBang/TraoBangConstants.cs` (`XepHang` 1 → `ChuanBi` 2 → `DangTraoBang` 3 → `DaTraoBang` 4, cộng thêm `ThamGiaTraoBang`/`VangMat`). `ISubPlanService` là trái tim của hệ thống — `NextSinhVienTraoBang`, `PrevSinhVienTraoBang`, `DiemDanhNhanBang`, `CutSlideNormal`, `NextSubPlan`, `Restart` thay đổi hàng đợi rồi bắn notification qua SignalR.

### Contract real-time

`TraoBangHub` được map tại `/hub/trao-bang` (dùng CORS policy riêng `SignalRPolicy`, cho phép mọi origin + credentials). Hub chỉ broadcast một chiều: server gọi `ITraoBangService.Notify*`, client chỉ lắng nghe. Tên event bị viết tay trùng lặp ở cả hai phía và phải được giữ đồng bộ:

- BE: `ITraoBangHub` (`ReceiveSinhVienDangTrao`, `ReceiveChonKhoa`, `ReceiveCheckIn`)
- FE: `TraoBangHubConst` trong `src/app/shared/constants/sv-nhan-bang.constants.ts`

Tương tự, `PermissionKeys.cs` (BE) và `permission.constants.ts` (FE) là hai bản chép tay của cùng một tập string — sửa thì phải sửa cả hai.

### Frontend

Standalone component, không dùng NgModule. Path alias `@/*` → `src/app/*` (lưu ý: `src/environments/*` được import bằng đường dẫn đầy đủ, không qua alias).

- `src/app.routes.ts` — cấp cao nhất. Phần app đã đăng nhập nằm dưới `AppLayout` + `authGuard`; **các route `/guest/**` (`main-screen`, `side-screen`, `trao-bang/profile`) cố ý nằm ngoài layout và không yêu cầu auth** — đây là các màn hình chiếu/sân khấu.
- Route của từng feature là các file lazy `loadChildren` (`trao-bang.routes.ts`, `user-management.routes.ts`), mỗi route mang `data: { permission: PermissionConstants.X }` để `permissionGuard` đọc.
- `config/auth.interceptor.ts` tự thêm `environment.baseUrl` vào trước mọi URL tương đối, gắn bearer token, và tự động refresh qua `/connect/token` khi gặp 401 (redirect về `/auth/login` nếu refresh thất bại). Vì vậy service chỉ khai báo path trần: `api = '/api/core/trao-bang/plan'`.
- Component trang kế thừa `BaseComponent` (`shared/components/base/base-component.ts`) để dùng `form`, `isFormInvalid()`, `getError()`, `isResponseSucceed(res)`, `messageSuccess/Error`, `confirmDelete/confirmAction`. Dùng những cái này thay vì inject trực tiếp `MessageService`/`ConfirmationService`.
- Service tổ chức một file cho một resource trong `src/app/service/`, là wrapper mỏng quanh `HttpClient`, typed bằng `IBaseResponse` / `IBaseResponsePaging` / `IBaseResponseWithData`.
- `shared/components/data-table` là wrapper table PrimeNG dùng chung cho các trang danh sách.

### Config & environment

Backend: `appsettings.json` chỉ dùng ở Development. Ở **Staging/Production toàn bộ configuration được overlay từ Infisical** (`Program.cs`, điều khiển bằng các env var `INFISICAL_URL` / `INFISICAL_PROJECT_ID` / `INFISICAL_CLIENT_ID` / `INFISICAL_CLIENT_SECRET`, với environment slug `staging` / `prod`). Thêm config key mới thì phải thêm cả trên Infisical, nếu không app sẽ throw ngay lúc khởi động. `AllowedHosts` ở đây bị dùng sai mục đích: nó là danh sách CORS origin ngăn cách bằng `;`, không phải host filter.

Frontend: `src/environments/environment.ts` (prod), `.staging.ts`, `.development.ts`, được hoán đổi bằng `fileReplacements` trong `angular.json`. Lưu ý Angular project vẫn mang tên `sakai-ng` và build ra `dist/sakai-ng`.

## Gotchas

- Một số file `.cs` trong `traobang.be.shared` vẫn khai báo namespace `thongbao.be.*` (ví dụ `ISoftDelted`) — project này được fork từ một codebase khác. Cứ theo namespace sẵn có trong file đang sửa, đừng "sửa cho đúng".
- Nhiều comment và exception string trong `Program.cs` bị mojibake (tiếng Việt sai encoding). Đừng tiện tay sửa.
- Angular ESLint config khai báo component prefix `p` và class suffix rỗng, nhưng component thực tế dùng prefix `app` và không có suffix `Component` (`export class Plan`, `export class McScreen`). Theo code, đừng theo lint config.
