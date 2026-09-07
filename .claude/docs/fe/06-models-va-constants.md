# 06 — Models & Constants

## Models — nơi đặt

```
src/app/models/
├── auth/
│   ├── user.models.ts
│   ├── role.models.ts
│   └── permission.models.ts
├── traobang/
│   ├── plan.models.ts
│   ├── sub-plan.models.ts
│   ├── slide.models.ts
│   ├── giao-dien.models.ts
│   ├── sv-nhan-bang.models.ts
│   └── guest-sv-nhan-bang.models.ts
└── guest-sv-nhan-bang.models.ts        ← bản trùng ở gốc, còn được import; đừng xóa

src/app/shared/models/                  ← kiểu dùng chung, không theo domain
├── request-paging.base.models.ts
├── data-table.models.ts
├── jwt-payload.models.ts
└── environment.models.ts
```

Một file `<resource>.models.ts` chứa **tất cả** interface + class constants của resource đó.

## Bộ interface chuẩn cho một resource

```ts
import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

// 1. Dòng dữ liệu hiển thị — MỌI field optional
export interface IViewRowConfigPlan {
    id?: number;
    ten?: string;
    moTa?: string;
    trangThai?: number;
    trangThaiText?: string;      // field phụ do FE tự map để hiển thị
    thoiGianBatDau?: string;
    thoiGianKetThuc?: string;
    createdDate?: string;
    idGiaoDien?: number;
}

// 2. Query phân trang
export interface IFindPagingConfigPlan extends IBaseRequestPaging { }

// 3. Body tạo mới — field bắt buộc thì KHÔNG optional
export interface ICreateConfigPlan {
    ten: string;
    moTa?: string;
    trangThai?: number;
    thoiGianBatDau?: string | null;
    thoiGianKetThuc?: string | null;
}

// 4. Body cập nhật = Create + id
export interface IUpdateConfigPlan extends ICreateConfigPlan {
    id: number;
}
```

Quy ước:

| Loại | Tên | Ghi chú |
|---|---|---|
| Dòng danh sách | `IViewRow<X>` | field optional hết, khớp `View...Dto` của BE |
| Chi tiết (nhiều field hơn) | `IView<X>` | thường `extends IViewRow<X>` — xem `IViewUser` |
| Query phân trang | `IFindPaging<X>` | `extends IBaseRequestPaging` |
| Body tạo | `ICreate<X>` | |
| Body sửa | `IUpdate<X>` | `extends ICreate<X>` + `id` |
| Kết quả action riêng | `IView<X>Response` | ví dụ `IViewSvBatDauLuiResponse` |

Tất cả interface bắt đầu bằng **`I`**. Field viết **camelCase** khớp JSON của BE (BE trả PascalCase → System.Text.Json tự camelCase hóa).

Ví dụ có phân biệt Row/Detail:

```ts
export interface IViewRowUser {
    id?: string;
    userName?: string;
    email?: string;
    fullName?: string;
    roles?: string[];
}

export interface IViewUser extends IViewRowUser {
    passwordRandom?: string;
    permissions?: string[];
}

export interface ICreateUser {
    userName?: string,
    email?: string,
    phoneNumber?: string,
    password?: string,
    fullName?: string,
    roleNames?: string[],
}

export interface IUpdateUser extends ICreateUser { id: string }

export interface IFindPagingUser extends IBaseRequestPaging {}
```

## Constants — class tĩnh, không dùng `enum`

Hai chỗ đặt:

- **`models/<resource>.models.ts`** — constants gắn với một resource (`PlanTrangThai` nằm trong `plan.models.ts`).
- **`shared/constants/`** — constants dùng nhiều nơi (`sv-nhan-bang.constants.ts`, `auth.constants.ts`, `permission.constants.ts`, `data-table.constants.ts`).

### Mẫu constants trạng thái

```ts
export class PlanTrangThai {
    static KHOI_TAO = 1;
    static DANG_HOAT_DONG = 2;
    static DA_KET_THUC = 3;

    static ListTrangThai = [
        { name: 'Khởi tạo',       code: PlanTrangThai.KHOI_TAO,       severity: 'secondary' },
        { name: 'Đang hoạt động', code: PlanTrangThai.DANG_HOAT_DONG, severity: 'info' },
        { name: 'Đã kết thúc',    code: PlanTrangThai.DA_KET_THUC,    severity: 'danger' }
    ];

    static getSeverity(code: number) {
        const found = this.ListTrangThai.find((x) => x.code === code);
        return typeof found != 'undefined' ? found.severity : '';
    }

    static getName(code: number) {
        const found = this.ListTrangThai.find((x) => x.code === code);
        return typeof found != 'undefined' ? found.name : '';
    }
}
```

Cấu trúc bắt buộc:
1. Các `static <TÊN_HOA> = <số>` — **giá trị phải khớp `TraoBangConstants.cs` bên BE**.
2. `static List` (hoặc `ListTrangThai`) — mảng `{ name, code, severity }`; `name` tiếng Việt có dấu, `severity` là severity của PrimeNG (`secondary | info | success | warn | danger | contrast`).
3. `getName(code)` + `getSeverity(code)`.

Dùng:

```ts
listTrangThai = PlanTrangThai.ListTrangThai;      // đổ vào <p-select [options]>
```

```html
<p-select formControlName="trangThai" [options]="listTrangThai" optionLabel="name" optionValue="code" class="w-full" />
```

### Constants dùng chung — `shared/constants/sv-nhan-bang.constants.ts`

Chứa 5 class:

| Class | Nội dung |
|---|---|
| `SvNhanBangStatuses` | 1–6, khớp `TraoBangConstants.cs`. Lưu ý nhãn của code 2 là **"Đã checkin"** (BE gọi là `ChuanBi`) |
| `SubPlanStatuses` | 1–6, cùng code nhưng **severity khác** `SvNhanBangStatuses` |
| `ViewSvTypeConstants` | `SV = 1`, `MO_BAI = 2`, `KET_BAI = 3` |
| `SlideConst` | `TEXT = 1`, `SinhVien = 2` + `listLoaiSlide` |
| `TraoBangHubConst` | URL hub + tên 3 event SignalR |

### `auth.constants.ts`

```ts
export class AuthConstants {
    static SESSION_PKCE_CODE_VERIFIER = 'pkce_code_verifier'
    static PKCE_CODE_CHALLENGE_METHOD = 'S256'
    static REDIRECT_URI_AFTER_LOGIN = 'redirect_uri_after_success'
    static SUPER_ADMIN_ROLE = "SuperAdmin"
}
```

### `permission.constants.ts`

Bản chép tay của `PermissionKeys.cs`. Xem [03-route-va-permission.md](03-route-va-permission.md).

## `Utils` — helper tĩnh

`shared/utils.ts`, class toàn `static`, import `import { Utils } from '@/shared/utils'`.

| Nhóm | Method |
|---|---|
| Storage | `getLocalStorage(key)`, `setLocalStorage(key, data)`, `clearLocalStorage()`, `getSessionStorage`, `setSessionStorage`, `removeSessionStorage`, `clearSessionStorage` |
| Token | `getAccessToken()`, `getRefreshToken()`, `getDecodedJwtPayload()` |
| Ngày (moment) | `formatDateCallApi(date, 'YYYY-MM-DDTHH:mm:ss')`, `formatDateView(date, 'DD/MM/YYYY')`, `reverseDateString` |
| Chuỗi | `convertVietnameseToEng`, `convertLowerCase`, `replaceAll`, `makeRandom` |
| Số | `transformMoney`, `transformPercent` (locale `vi-VN`) |
| PKCE | `generatePKCECodes()`, `base64UrlEncode()` |
| Khác | `refreshData(data)` (deep clone qua JSON), `log`, `convertParamUrl` |

Gửi ngày lên API **luôn qua `Utils.formatDateCallApi`**:

```ts
const from = Utils.formatDateCallApi(this.form.value['time'][0]);
```

Hiển thị ngày trong bảng thì dùng `cellViewType: CellViewTypes.DATE` (dùng `DatePipe`), không dùng `Utils.formatDateView` trong template.

## Directives

`shared/directives/` — hai directive standalone, dùng khi cần bắt Enter trên input:

```ts
@Directive({ selector: '[appEnterKey]', standalone: true })
export class EnterKeyDirective {
    @Output() appEnterKey = new EventEmitter<void>();

    @HostListener('keydown.enter', ['$event'])
    onEnter(event: KeyboardEvent) { event.preventDefault(); this.appEnterKey.emit(); }
}
```

`ShiftEnterKeyDirective` tương tự cho Shift+Enter. Import trực tiếp vào `imports` của component cần dùng.

## Gotcha

- `SvNhanBangStatuses` và `SubPlanStatuses` **trùng code nhưng khác `severity` và khác nhãn** — chọn đúng class theo ngữ cảnh (sinh viên hay khoa).
- Có **hai** file `guest-sv-nhan-bang.models.ts` (một ở `models/`, một ở `models/traobang/`). Trước khi import, kiểm tra file nào đang được dùng ở màn hình tương tự.
- Model không có validation runtime — sai tên field so với BE thì `undefined` im lặng, không lỗi. Đối chiếu với `View...Dto` bên BE khi thêm field.
