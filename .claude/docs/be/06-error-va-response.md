# 06 — Response envelope & Error handling

## `ApiResponse`

`traobang.be.shared/HttpRequest/ApiResponse.cs`

```csharp
public class ApiResponse
{
    public StatusCodeE Status { get; set; }   // Success = 1, Error = 0
    public object? Data { get; set; }
    public int Code { get; set; }             // 200 khi OK, ErrorCode khi lỗi
    public string Message { get; set; }       // "Ok" hoặc message tiếng Việt
}

public enum StatusCodeE { Success = 1, Error = 0 }
```

JSON trả về FE:

```jsonc
// thành công
{ "status": 1, "data": { ... }, "code": 200, "message": "Ok" }

// lỗi nghiệp vụ — vẫn là HTTP 200
{ "status": 0, "data": null, "code": 1001, "message": "Kế hoạch không tồn tại" }
```

**Lỗi nghiệp vụ trả HTTP 200.** FE kiểm tra `res.status === 1` (`BaseComponent.isResponseSucceed`), không nhìn HTTP status code. Đừng đổi sang trả 400/404 — sẽ vỡ toàn bộ FE.

Ba constructor dùng trong controller:

```csharp
return new();            // Success, data = null
return new(result);      // Success, data = result
return OkException(ex);  // Error
```

Có `ApiResponse<T>` generic nhưng **chưa dùng ở đâu** trong controller.

## `OkException` — điểm bắt lỗi duy nhất

`BaseController.OkException(Exception ex)` phân loại exception rồi log + trả envelope:

| Loại exception | `Code` | `Message` |
|---|---|---|
| `UserFriendlyException` | `ErrorCode` của exception | `MessageLocalize` nếu có, không thì `ErrorMessages.GetMessage(errorCode)` |
| `DbUpdateException` | `500` | `InnerException.Message` (lộ chi tiết DB — cẩn thận) |
| `InvalidOperationException` | `500` | `InnerException.Message` |
| Còn lại | `500` | `"Internal server error"` |

Mọi nhánh đều `_logger.LogError(ex, ...)` kèm `Path` và query string.

Không có global exception middleware/filter → **quên `try/catch` trong action là lỗi rơi ra ngoài thành HTTP 500 trần**, FE không hiển thị được message.

## `UserFriendlyException`

`shared/HttpRequest/AppException/`

```csharp
throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

// message tự do, không tra từ điển
throw new UserFriendlyException(ErrorCodes.AuthErrorCreateRole,
    string.Join("; ", result.Errors.Select(e => e.Description)));
```

Tham số thứ hai là `MessageLocalize`: nếu truyền thì `OkException` dùng luôn chuỗi đó thay vì tra `ErrorMessages`.

Kế thừa `BaseException` (có sẵn `ErrorCode`, `MessageLocalize`, `ErrorMessage`, `ListParam` — hai field cuối chưa được dùng).

Trong service **không bao giờ** `throw new Exception("...")` cho lỗi nghiệp vụ.

## Thêm mã lỗi mới

Sửa **hai file**, đặt cạnh nhóm tương ứng:

### `shared/HttpRequest/Error/ErrorCodes.cs`

```csharp
public const int TraoBangErrorTienDoNotFound = 1015;
public const int TraoBangErrorGiaoDienNotFound = 1101;
```

Dải số đang dùng:

| Dải | Nhóm |
|---|---|
| `1`, `400`, `401`, `404`, `409`, `500` | Lỗi HTTP/hệ thống cơ bản |
| `101–109` | Auth (user, role, password) |
| `701–702` | Service account / Google Sheet |
| `801`, `809` | Import Excel |
| `901–903` | Quyền truy cập Google Sheet / service account |
| `1001–1015` | Nghiệp vụ trao bằng (plan, subplan, sinh viên, slide, tiến độ) |
| `1101+` | Giao diện |

Tên hằng: `<Nhóm>Error<MôTả>` — `TraoBangErrorPlanNotFound`, `AuthErrorRoleInUsed`, `TraoBangErrorSinhVienDaTonTaiTrongHangDoi`.

### `shared/HttpRequest/Error/ErrorMessages.cs`

```csharp
private static readonly Dictionary<int, string> _messages = new()
{
    { ErrorCodes.TraoBangErrorPlanNotFound, "Kế hoạch không tồn tại" },
    { ErrorCodes.TraoBangErrorTienDoNotFound, "Không tìm thấy slide trong hàng đợi" },
};
```

Message **tiếng Việt có dấu**, hướng tới người dùng cuối. Thiếu entry thì `GetMessage` trả `"Unknown error."` — luôn nhớ thêm.

Message có placeholder thì dùng `{0}`:

```csharp
{ ErrorCodes.ImportHeaderErrorInvalid, "Header không đúng định dạng tại dòng {0}" },
```

(Hiện `OkException` **không** format placeholder — `ListParam` chưa được dùng. Muốn có số dòng thật thì truyền chuỗi đã format qua tham số `MessageLocalize`.)

## Response phân trang

```csharp
public class BaseResponsePagingDto<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalItems { get; set; }
    public object? CustomData { get; set; }
}
```

Nằm trong `data` của envelope:

```jsonc
{ "status": 1, "code": 200, "message": "Ok",
  "data": { "items": [...], "totalItems": 42, "customData": null } }
```

FE map bằng `IBaseResponsePaging<T>`.
