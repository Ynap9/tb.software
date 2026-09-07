---
name: be-migration
description: "Sinh EF Core migration cho backend traobang.be — sửa entity, chạy dotnet ef migrations add đúng đường dẫn project, kiểm tra file migration sinh ra."
risk: safe
source: self
date_added: "2026-09-07"
---

# Sinh migration cho traobang.be

Dùng khi cần thêm/sửa/xóa cột, bảng, index của backend — tức là khi vừa đụng vào entity trong `traobang.be.domain` hoặc cấu hình trong `TbDbContext.OnModelCreating`.

## Quy trình

### 1. Sửa entity trước

Entity nằm ở `traobang.be/traobang.be.domain/<Area>/`, các bảng buổi lễ ở `TraoBang/`.

Bám theo cột đã có cùng loại trong chính file đó — copy khung rồi sửa tên, đừng tự thêm attribute mới:

- `string` không nullable thì khởi tạo `= String.Empty;` → migration tự sinh `nullable: false, defaultValue: ""`.
- Có `[MaxLength(n)]` → `nvarchar(n)`; không có → `nvarchar(max)`.
- Cột soft delete (`Deleted`, `DeletedDate`, `DeletedBy`) và default `getdate()` được cấu hình trong `TbDbContext.OnModelCreating`, không đặt bằng attribute.
- Schema mặc định là `core`; bảng buổi lễ ghim schema `tb` bằng `[Table(nameof(X), Schema = DbSchemas.TraoBang)]`.

Cột bỏ đi thì đánh `[Obsolete("Sắp xóa")]` chứ không xóa thẳng — theo đúng cách các cột `IdSubPlan`, `IsShow`, `Order`, `TrangThai` đang làm.

### 2. Chạy lệnh

`TbDbContext` nằm trong `traobang.be.infrastructure.data` còn host là `traobang.be`, nên luôn phải truyền cả `-p` lẫn `-s`:

```bash
cd C:/Code/traobang/traobang.be
dotnet ef migrations add <ten-migration> -p traobang.be.infrastructure.data -s traobang.be
```

Lưu ý cấu trúc thư mục: repo root là `C:/Code/traobang`, các project nằm ở `C:/Code/traobang/traobang.be/*`, còn file solution ở sâu hơn một cấp — `traobang.be/traobang.be/traobang.be.sln`. Chạy lệnh từ `C:/Code/traobang/traobang.be`.

Đặt tên migration kebab-case theo nội dung thay đổi, giống các migration gần đây: `add-link-qr-only`, `update-tbl-tien-dotraobang`, `tbl-plan-add-trang-thai`.

### 3. Kiểm tra file sinh ra

Đọc file `traobang.be.infrastructure.data/Migrations/<timestamp>_<ten>.cs` xem `Up`/`Down` có đúng ý không: đúng `schema`, đúng kiểu cột, `defaultValue` hợp lý cho dữ liệu cũ. Báo lại nội dung cho user.

Sai thì gỡ ra sửa entity rồi sinh lại:

```bash
dotnet ef migrations remove -p traobang.be.infrastructure.data -s traobang.be
```

### 4. Không tự chạy database update

User tự chạy `dotnet ef database update` / build tay. Chỉ chạy khi được yêu cầu rõ ràng:

```bash
dotnet ef database update -p traobang.be.infrastructure.data -s traobang.be
```

## Gotchas

- **Lần chạy đầu có thể báo `Build failed. Use dotnet build to see the errors.` dù code không lỗi.** Chạy lại lệnh là được. Trước khi đi sửa code, cứ chạy `dotnet build traobang.be/traobang.be.sln` để xác nhận thật sự có error hay không — thường là file bị khóa chứ không phải lỗi biên dịch.
- `dotnet ef` khởi động host thật ở environment `Development` để lấy service provider, nên nó **kết nối DB dev và Hangfire sẽ tự cài SQL objects vào đó**. Đây là hành vi sẵn có của `Program.cs`, không phải tác dụng phụ của migration — nhưng nhớ là lệnh này có đụng DB dev.
- Warning `NU1903` về AutoMapper là warning có sẵn của repo, kệ nó.
- Thêm cột vào entity **không** tự động thêm vào DTO. `ViewSinhVienNhanBangDto`, `Create*Dto`, `Update*Dto` và các chỗ map thủ công trong service phải sửa riêng — chỉ sửa khi user yêu cầu, còn không thì báo cho user biết là DTO chưa có trường mới.
- Mỗi migration sinh kèm một file `.Designer.cs` và cập nhật `TbDbContextModelSnapshot.cs`. Cả ba file đều phải commit.
