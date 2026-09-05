# 07 — Quy ước đặt tên

## Nguyên tắc chung

- **Tên file, thư mục, route, selector**: kebab-case, tiếng Việt **không dấu** — `sub-plan`, `giao-dien`, `sv-nhan-bang`, `scan-qr-sv`, `mc-screen`, `cau-hinh`.
- **Label, title, message hiển thị**: tiếng Việt **có dấu** — `'Chương trình'`, `'Không được bỏ trống'`, `'Đã xóa'`.
- **Class, interface, biến**: tiếng Anh hoặc tiếng Việt không dấu, PascalCase / camelCase.

## Bảng tra nhanh

| Loại | Quy tắc | Ví dụ |
|---|---|---|
| Thư mục màn hình | kebab-case | `pages/trao-bang/cau-hinh/sub-plan/` |
| File component | trùng tên thư mục, 3 file | `sub-plan.ts` / `.html` / `.scss` |
| Class component | PascalCase, **không hậu tố** `Component` | `SubPlan`, `McScreen`, `TblAction`, `DataTable` |
| Selector | `app-<ten-file>` | `app-sub-plan`, `app-data-table`, `app-tbl-action` |
| File service | `<resource>.service.ts` | `plan.service.ts` |
| Class service | `<Resource>Service` | `UserService`, `SlideService` |
| File model | `<resource>.models.ts` (số nhiều `models`) | `plan.models.ts` |
| Interface | `I` + PascalCase | `IViewRowConfigPlan`, `ICreateUser` |
| File constants | `<nhom>.constants.ts` | `sv-nhan-bang.constants.ts` |
| Class constants | PascalCase | `PlanTrangThai`, `AuthConstants`, `TraoBangHubConst` |
| Giá trị constant | UPPER_SNAKE_CASE | `KHOI_TAO`, `DANG_TRAO_BANG`, `SUPER_ADMIN_ROLE` |
| Guard | `<ten>-guard.ts`, hàm camelCase | `auth-guard.ts` → `authGuard` |
| Directive | `<ten>.directive.ts`, selector `app<Ten>` | `enter-key.directive.ts` → `[appEnterKey]` |
| File route | `<feature>.routes.ts`, `export default` | `trao-bang.routes.ts` |

## Thư mục con của một màn hình

```
plan/
├── plan.ts / plan.html / plan.scss     ← trang danh sách
├── create/                             ← dialog thêm + sửa (dùng chung)
│   └── create.ts / .html / .scss       ← class Create
├── tbl-action/                         ← cột thao tác
│   └── tbl-action.ts / .html           ← class TblAction + const TblActionTypes
└── upload/                             ← dialog import Excel (nếu có)
    └── upload.ts / .html / .scss       ← class Upload
```

Tên `create`, `tbl-action`, `upload` **lặp lại y hệt** ở mọi màn hình — đó là chủ đích, không phải trùng lặp cần sửa. Class `Create` ở `plan/create/` và ở `user/create/` là hai class khác nhau, phân biệt bằng đường dẫn import.

## Biến trong component

```ts
_planService = inject(TraoBangPlanService);       // service: _camelCase (public)
private _ref = inject(DynamicDialogRef);          // của dialog: private _camelCase
private _config = inject(DynamicDialogConfig);

columns: IColumn[] = [...];                       // cấu hình bảng: columns
data: IViewRowConfigPlan[] = [];                  // dữ liệu bảng: data
query: IFindPagingConfigPlan = {...};             // tham số tìm kiếm: query
searchForm: FormGroup = ...;                      // form tìm kiếm: searchForm
listTrangThai = PlanTrangThai.ListTrangThai;      // dữ liệu dropdown: list<Ten>
listGiaoDien: IViewGiaoDien[] = [];
selectedFile: File | null = null;
```

Tên cố định nên giữ nguyên giữa các màn hình: `columns`, `data`, `query`, `searchForm`, `list...`, `loading` (từ `BaseComponent`), `totalRecords` (từ `BaseComponent`).

## Tên method trong component

| Tiền tố | Dùng cho | Ví dụ |
|---|---|---|
| `get...` | Gọi API lấy dữ liệu | `getData()`, `getListGiaoDien()`, `getHangDoi()`, `getCurrentSubPlan()` |
| `on...` | Handler sự kiện từ template | `onSearch()`, `onSubmit()`, `onPageChanged()`, `onOpenCreate()`, `onDelete()`, `onCustomEmit()` |
| `onSubmit...` | Nhánh submit | `onSubmitCreate()`, `onSubmitUpdate()` |
| `init...` | Khởi tạo trong `ngOnInit` | `initData()`, `initOnCreate()`, `initOnUpdate()` |
| `connect...` | Mở kết nối | `connectHub()` |
| `is...` / `get isX()` | Trạng thái | `get isUpdate()`, `isLoadingNext` |

## Tên method trong service

Cố định theo CRUD, dùng lại y hệt ở mọi service:

```ts
findPaging(query)      // GET  list phân trang
getById(id)            // GET  1 bản ghi
getList()              // GET  danh sách đầy đủ (dropdown)
create(body)           // POST
update(body)           // PUT
delete(id)             // DELETE
downloadFileTemplate() // GET blob
uploadFile(body)       // POST FormData
```

Action nghiệp vụ đặt tên theo hành động, tiếng Việt không dấu hoặc tiếng Anh: `restart()`, `genQrCode(id)`, `getHangDoi()`, `getListSubPlanActive()`, `getByMssv(mssv)`.

## Class trong template

Tailwind utility, không viết CSS riêng trừ khi bắt buộc (file `.scss` thường rỗng):

```html
<div class="card">
<div class="flex flex-row justify-between items-center mb-4">
<div class="font-semibold text-xl uppercase">Chương trình</div>
<label class="block text-surface-900 dark:text-surface-0 font-medium mb-2">
<span class="text-red-500">*</span>
<div class="mt-5 flex flex-row justify-end">
```

Luôn kèm biến thể `dark:` cho màu chữ/nền (theme có chế độ tối qua `.app-dark`).

## Từ vựng nghiệp vụ

| Tiếng Việt | Trong code |
|---|---|
| Chương trình (buổi lễ) | `plan` / `Plan` |
| Khoa | `sub-plan` / `SubPlan` |
| Sinh viên nhận bằng | `sv-nhan-bang` / `SvNhanBang`, viết tắt `sv` |
| Mã số sinh viên | `mssv` |
| Hàng đợi / tiến độ | `hangDoi`, `tienDo` |
| Giao diện (template màn chiếu) | `giao-dien` / `GiaoDien` |
| Màn điều khiển (MC) | `mc-screen` / `McScreen` |
| Màn checkin | `scan-qr-sv` / `ScanQrSv` |
| Sân khấu | `main-screen` / `san-khau-main` |
| Cánh gà | `side-screen` / `SideScreen` |
| Trang tra cứu của SV | `guest-profile` / `GuestProfile` |

## Gotcha — chỗ đang không nhất quán

Biết để **không bắt chước ở file mới**, và cũng **không đi sửa file cũ**:

- Tên class service: `TraoBangPlanService`, `TraoBangSubPlanService`, `TraoBangSvService` có prefix `TraoBang`; còn `SlideService`, `GiaoDienService`, `UserService` thì không. **File mới: dùng `<Resource>Service` không prefix.**
- Hai service thiếu hậu tố `.service` ở tên file: `system-trao-bang.ts`, `guest-sv-nhan-bang.ts`. **File mới: luôn `<ten>.service.ts`.**
- `SlideScreen` là class của `pages/trao-bang/cau-hinh/slide/slide.ts` (không phải `Slide`) vì tránh trùng tên với model.
- ESLint config khai `prefix: 'p'` cho component/directive và `component-class-suffix: ['']` — **không khớp** code thực tế (`app-` và không suffix). Theo code, không theo config.
- Một biến trong `mc-screen.ts` đặt tên có dấu tiếng Việt (`svChuanBiTiếpTheo`). Đừng làm theo.
