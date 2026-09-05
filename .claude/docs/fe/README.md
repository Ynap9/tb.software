# FE — Hướng dẫn code convention

Tài liệu mô tả **cách code frontend đang được viết trong repo này** (Angular 20 standalone + PrimeNG, `traobang.fe/`). Đọc xong là viết được màn hình mới đúng style hiện tại.

Đây là mô tả *thực tế đang có*, không phải best practice lý tưởng. Chỗ nào code hiện tại lệch chuẩn hoặc có bug tiềm ẩn thì ghi ở mục "Gotcha" của từng file.

## Quy tắc số 0 — không tự bịa

**Khi code, đừng tự nghĩ ra cách làm mới. Cứ theo đúng style và pattern sẵn có trong repo.**

- Cần viết cái gì thì **mở màn hình cùng loại ra copy khung** rồi sửa tên: trang danh sách → `pages/trao-bang/cau-hinh/plan/`; dialog thêm/sửa → `plan/create/`; service → `service/plan.service.ts`.
- Không thêm thư viện, state management (NgRx/signal store), abstraction, hay base class mà repo chưa dùng.
- Không "hiện đại hóa" code đang chạy: không đổi `@Input()` sang `input()`, không chuyển `subscribe` sang `toSignal`, không đổi `*ngIf` sang `@if` ở file cũ, không đổi `NgModule` gì cả (repo đã standalone hết).
- Chỗ nào repo có **hai cách viết** thì theo cách của **file đang sửa**, không đồng bộ hóa cả repo.
- Thấy thứ đáng cải thiện thì **nói ra**, đừng tự làm — trừ khi được yêu cầu rõ ràng.

## Quy tắc số 0.1 — không tự build

**Không chạy `npm run build` / `npm start` để kiểm tra sau khi sửa code.** User tự build tay. Sửa xong thì báo cáo thay đổi và nêu rõ chỗ cần chú ý (đổi tên field model, thêm `override`, đổi input/output của component…), không tự verify bằng build. Chỉ build khi được yêu cầu rõ ràng.

## Thứ tự đọc

| File | Nội dung |
|---|---|
| [01-tong-quan.md](01-tong-quan.md) | Cấu trúc thư mục, alias `@/`, bootstrap, layout |
| [02-component.md](02-component.md) | Tạo component: trang danh sách, dialog, tbl-action, màn hình chiếu |
| [03-route-va-permission.md](03-route-va-permission.md) | Route, lazy load, guard, `data.permission`, menu sidebar |
| [04-service-goi-api.md](04-service-goi-api.md) | Service, interceptor, envelope, upload/download, SignalR |
| [05-base-component-va-data-table.md](05-base-component-va-data-table.md) | API của `BaseComponent` và `DataTable` |
| [06-models-va-constants.md](06-models-va-constants.md) | Interface model, class constants có `getName`/`getSeverity` |
| [07-dat-ten.md](07-dat-ten.md) | Quy ước đặt tên file, class, selector, biến |
| [08-checklist-them-man-hinh.md](08-checklist-them-man-hinh.md) | Checklist end-to-end thêm màn hình mới |

## 5 quy tắc quan trọng nhất

1. **Component trang luôn `extends BaseComponent`** — lấy `form`, `loading`, `isResponseSucceed`, `messageSuccess/Error`, `confirmDelete`, `_dialogService`.
2. **Kiểm tra kết quả API bằng `this.isResponseSucceed(res)`**, không nhìn HTTP status. BE trả lỗi nghiệp vụ dưới dạng HTTP 200 + `status: 0`.
3. **Service khai `api` là path trần** (`api = '/api/core/trao-bang/plan'`). `authInterceptor` tự ghép `environment.baseUrl` và gắn token.
4. **Import UI bằng `SharedImports`** (`@/shared/import.shared`), chỉ import lẻ module PrimeNG khi `SharedImports` chưa có.
5. **Thêm màn hình = thêm route có `data.permission` + `canActivate: [permissionGuard]` + thêm mục vào `app.menu.ts` với `visible: isGranted(...)`.**

## Vocabulary

Tên nghiệp vụ dùng tiếng Việt không dấu (`plan`, `sub-plan`, `sv-nhan-bang`, `giao-dien`, `trao-bang`, `scan-qr-sv`). Label và message hiển thị dùng tiếng Việt có dấu. Giữ nguyên phong cách này.
