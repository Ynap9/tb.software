# BE — Hướng dẫn code convention

Tài liệu mô tả **cách code backend đang được viết trong repo này** (ASP.NET Core 8, `traobang.be/`). Mục đích: đọc xong là viết được feature mới đúng style hiện tại, không phải mở lại 10 file để đoán convention.

Đây là mô tả *thực tế đang có*, không phải best practice lý tưởng. Chỗ nào code hiện tại lệch chuẩn hoặc có bug tiềm ẩn thì được ghi rõ ở mục "Gotcha" của từng file.

## Quy tắc số 0 — không tự bịa

**Khi code, đừng tự nghĩ ra cách làm mới. Cứ theo đúng style và pattern sẵn có trong repo.**

- Cần viết cái gì thì **mở file cùng loại đang có ra copy khung** rồi sửa tên: controller mới → xem `PlanController`; service mới → xem `PlanService` / `GiaoDienService`; DTO mới → xem `Dtos/GiaoDien/`.
- Không thêm layer, abstraction, helper, base class, thư viện, hay design pattern mà repo chưa dùng (không repository, không MediatR, không global exception filter, không FluentValidation…).
- Không "sửa cho đẹp" code đang chạy được: không đổi tên hằng sai chính tả, không dọn namespace `thongbao.be.*`, không async hóa method sync, không đổi lỗi nghiệp vụ sang trả HTTP 4xx.
- Chỗ nào repo có **hai cách viết** (LINQ query-syntax vs method-syntax, `?? throw` vs `if (x == null) throw`) thì theo cách của **file đang sửa**, không đồng bộ hóa cả repo.
- Thấy thứ đáng cải thiện thì **nói ra**, đừng tự làm — trừ khi được yêu cầu rõ ràng.

## Quy tắc số 0.1 — không tự build

**Không chạy `dotnet build` để kiểm tra sau khi sửa code.** User tự build tay. Sửa xong thì báo cáo thay đổi và nêu rõ chỗ cần chú ý (đổi signature, thêm `using`, đổi kiểu trả về…), không tự verify bằng build. Chỉ build khi được yêu cầu rõ ràng.

## Thứ tự đọc

| File | Nội dung |
|---|---|
| [01-tong-quan-tang.md](01-tong-quan-tang.md) | 6 project, phụ thuộc giữa các tầng, file nào đặt ở đâu |
| [02-controller-va-route.md](02-controller-va-route.md) | Khung controller, cách đặt route, `[Permission]`, upload/download file |
| [03-service.md](03-service.md) | Khung service, đăng ký DI, logging, transaction, soft delete, SignalR |
| [04-dto.md](04-dto.md) | Phân loại DTO, đặt tên, folder, validation, paging |
| [05-permission-va-auth.md](05-permission-va-auth.md) | `PermissionKeys`, `PermissionAttribute`, role, luồng token OpenIddict |
| [06-error-va-response.md](06-error-va-response.md) | `ApiResponse`, `ErrorCodes`, `UserFriendlyException`, `OkException` |
| [07-dat-ten.md](07-dat-ten.md) | Toàn bộ quy ước đặt tên: class, method, route, DTO, biến |
| [08-checklist-them-feature.md](08-checklist-them-feature.md) | Checklist end-to-end khi thêm một resource mới |

## 5 quy tắc quan trọng nhất

Nếu chỉ nhớ được 5 điều thì nhớ những điều này:

1. **Controller luôn trả `ApiResponse`**, mọi action bọc `try/catch` → `return OkException(ex)`. Lỗi trả về HTTP **200** kèm `status = 0`.
2. **Service phải đăng ký tay** trong `#region service` của `Program.cs` — không có auto-scan.
3. **Dòng đầu mỗi method service là log**: `_logger.LogInformation($"{nameof(Method)}, dto = {JsonSerializer.Serialize(dto)}");`
4. **Không xóa cứng**. Luôn `Deleted = true` + `DeletedDate` + `DeletedBy`, và mọi query đều phải có `!x.Deleted`.
5. **Ném `UserFriendlyException(ErrorCodes.X)`** cho lỗi nghiệp vụ, không `throw new Exception("...")`.

## Vocabulary

Tên miền nghiệp vụ viết **tiếng Việt không dấu** (`Plan`, `SubPlan`, `DanhSachSinhVienNhanBang`, `TienDoTraoBang`, `GiaoDien`, `TrangThai`, `HoVaTen`). Comment và message lỗi viết **tiếng Việt có dấu**. Giữ nguyên phong cách này.
