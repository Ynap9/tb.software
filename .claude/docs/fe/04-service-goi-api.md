# 04 — Service & gọi API

## Khung chuẩn

Một file / một resource, đặt trong `src/app/service/`. Không kế thừa base nào.

```ts
import { IFindPagingConfigPlan, IViewRowConfigPlan, ICreateConfigPlan, IUpdateConfigPlan } from '@/models/traobang/plan.models';
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class TraoBangPlanService {
    api = '/api/core/trao-bang/plan';
    http = inject(HttpClient);

    findPaging(query: IFindPagingConfigPlan) {
        return this.http.get<IBaseResponsePaging<IViewRowConfigPlan>>(this.api, {
            params: { ...query }
        });
    }

    getById(id: string) {
        return this.http.get<IBaseResponseWithData<IViewRowConfigPlan>>(`${this.api}/${id}`);
    }

    getList() {
        return this.http.get<IBaseResponseWithData<IViewRowConfigPlan[]>>(`${this.api}/list`);
    }

    create(body: ICreateConfigPlan) {
        return this.http.post<IBaseResponse>(`${this.api}`, body);
    }

    update(body: IUpdateConfigPlan) {
        return this.http.put<IBaseResponse>(`${this.api}/${body.id}`, body);
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }
}
```

Bốn thứ bắt buộc:

1. `@Injectable({ providedIn: 'root' })` — không khai trong `providers` của component.
2. `api = '<path trần>'` — **không có `environment.baseUrl`**, interceptor tự ghép.
3. `http = inject(HttpClient)` (public, không `private`).
4. Trả thẳng `Observable` từ `this.http.*`, **không `subscribe`, không `pipe(map)`** — component tự xử lý.

## Kiểu trả về

`shared/models/request-paging.base.models.ts`:

```ts
export type IBaseResponse = {
    status: number      // 1 = thành công, 0 = lỗi
    code: number
    message: string
}

export type IBaseResponseWithData<T> = IBaseResponse & { data: T }

export type IBaseResponsePaging<T> = IBaseResponse & { data: IBaseResponsePagingData<T> }

export type IBaseResponsePagingData<T> = {
    custommData: any    // (sic — BE trả "customData", field này gõ sai)
    items: T[]
    totalItems: number
}

export interface IBaseRequestPaging {
    pageSize: number
    pageNumber: number
    keyword?: string
}
```

Chọn kiểu:

| Endpoint | Kiểu |
|---|---|
| List phân trang | `IBaseResponsePaging<IViewRowX>` |
| Lấy 1 bản ghi / danh sách đầy đủ | `IBaseResponseWithData<IViewRowX>` / `IBaseResponseWithData<IViewRowX[]>` |
| Create / Update / Delete (không trả data) | `IBaseResponse` |
| Create có trả data | `IBaseResponseWithData<IViewX>` |

## Bảng prefix API (khớp BE)

| Nhóm | Prefix |
|---|---|
| Nghiệp vụ trao bằng | `/api/core/trao-bang/<resource>` |
| Màn cấu hình slide | `/api/config/slide` |
| User / role / permission | `/api/app/<resource>` |
| Token OpenIddict | `/connect/token` (gọi trực tiếp full URL trong `AuthService`) |

Service hiện có:

| File | Class | `api` |
|---|---|---|
| `plan.service.ts` | `TraoBangPlanService` | `/api/core/trao-bang/plan` |
| `sub-plan.service.ts` | `TraoBangSubPlanService` | `/api/core/trao-bang/sub-plan` |
| `sv-nhan-bang.service.ts` | `TraoBangSvService` | `/api/core/trao-bang/sub-plan` |
| `system-trao-bang.ts` | `SystemTraoBangService` | `/api/core/trao-bang/sub-plan` |
| `guest-sv-nhan-bang.ts` | `GuestSvNhanBangService` | `/api/core/trao-bang/sub-plan/sinh-vien-nhan-bang` |
| `slide.service.ts` | `SlideService` | `/api/config/slide` |
| `slide-drag-drop.service.ts` | `SlideDragDropService` | `/api/core/trao-bang/slide` |
| `giao-dien.service.ts` | `GiaoDienService` | `/api/core/trao-bang/giao-dien` |
| `user.service.ts` / `role.service.ts` / `permission.service.ts` | `UserService` / `RoleService` / `PermissionService` | `/api/app/*` |
| `auth.service.ts` | `AuthService` | (gọi `/connect/token` bằng full URL) |
| `shared.service.ts` | `SharedService` | — (kho quyền client) |
| `scan-qr.service.ts` | `ScanQrService` | — (nghe máy quét QR) |

Nhiều service cùng trỏ `/api/core/trao-bang/sub-plan` vì BE gom nhiều chức năng vào một controller — cứ tách service theo **màn hình sử dụng**, không theo controller.

## Các dạng gọi khác

### Query param

```ts
findPaging(query: IFindPagingConfigSubPlan, dataFilter?: any) {
    return this.http.get<IBaseResponsePaging<IViewRowConfigSubPlan>>(this.api, {
        params: { ...query, ...dataFilter }
    });
}
```

### Path lồng nhau

```ts
getList(idPlan: number) {
    return this.http.get<IBaseResponseWithData<IViewRowConfigSubPlan[]>>(`${this.api}/plan/${idPlan}/list`);
}

delete(id: number, idPlan: number) {
    return this.http.delete<IBaseResponse>(`${this.api}/${id}/plan/${idPlan}`);
}
```

### POST không body

```ts
restart() {
    return this.http.post<IBaseResponse>(`${this.api}/restart`, null);
}

genQrCode(id: number) {
    return this.http.post<IBaseResponse>(`${this.api}/qr/slide/${id}`, {});
}
```

### Tải file (blob)

```ts
downloadFileTemplate() {
    return this.http.get(`${this.api}/export/template-import-slide`, { responseType: 'blob' });
}
```

Tải và lưu file luôn (mẫu trong `GuestSvNhanBangService`):

```ts
downloadQr(imageUrl: string, fileName: string) {
    this.http.get(imageUrl, { responseType: 'blob' }).subscribe((blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        window.URL.revokeObjectURL(url);
    });
}
```

### Upload file

```ts
uploadFile(body: any) {
    return this.http.post<IBaseResponse>(`${this.api}/import/slide`, body);   // body là FormData
}
```

Không set `Content-Type` thủ công — để trình duyệt tự sinh boundary.

## `authInterceptor` — cái làm mọi thứ tự động

`src/config/auth.interceptor.ts`, đăng ký trong `app.config.ts`.

Với **mọi** request:
1. Ghép URL: `req.url.startsWith('http') ? req.url : environment.baseUrl + req.url` → vì vậy service chỉ khai path trần.
2. Gắn `Authorization: Bearer <accessToken>` lấy từ `localStorage.auth`.

Khi response **401**:
1. Lấy `refreshToken`; không có thì xóa `auth` và về `/auth/login`.
2. Gọi `POST {baseUrl}/connect/token` với `grant_type=refresh_token`.
3. Lưu token mới, **retry request gốc**.
4. Refresh thất bại → xóa local/session storage, về `/auth/login`.

Khi response **403**: tự hiện toast `"Bạn không có quyền thực hiện thao tác này"` — component **không cần** tự xử lý 403.

Hệ quả cần nhớ:
- Gọi URL tuyệt đối (ảnh MinIO chẳng hạn) thì interceptor **vẫn gắn token** nhưng không ghép baseUrl.
- Token và refresh token lưu ở `localStorage` key `auth`, đọc bằng `Utils.getAccessToken()` / `Utils.getRefreshToken()`.

## Xử lý response trong component

```ts
this._planService.findPaging(query).subscribe({
    next: (res) => {
        if (this.isResponseSucceed(res, false)) {       // false = không tự hiện toast lỗi
            this.data = res.data.items;
            this.totalRecords = res.data.totalItems;
        }
    },
    error: (err) => { this.messageError(err?.message); },
    complete: () => { this.loading = false; }
});
```

`isResponseSucceed(res, isShowErrorMsg = true, successMsg = '')` từ `BaseComponent`:
- Trả `true` khi `res.status === 1`; nếu truyền `successMsg` thì hiện toast xanh.
- Trả `false` khi lỗi; mặc định hiện toast đỏ với `res.message` của BE.

Quy ước dùng:

| Tình huống | Gọi |
|---|---|
| Load danh sách (không muốn spam toast) | `isResponseSucceed(res, false)` |
| Load dữ liệu phụ (dropdown) | `isResponseSucceed(res)` |
| Sau khi tạo/sửa/xóa | `isResponseSucceed(res, true, 'Đã lưu')` |

Chuỗi nhiều request phụ thuộc nhau → dùng `concatMap`:

```ts
this._roleService.getList()
    .pipe(
        concatMap(res => {
            if (this.isResponseSucceed(res)) this.listRole = res.data;
            return this._userService.getById(this._config.data.id);
        })
    )
    .subscribe({ next: (res) => { ... } });
```

## Auth flow (`AuthService`)

```ts
login(username, password) {
    const body = new HttpParams()
        .set('username', username)
        .set('password', password)
        .set('grant_type', environment.authGrantType)      // 'password'
        .set('client_id', environment.authClientId)
        .set('client_secret', environment.authClientSecret ?? '')
        .set('scope', environment.authScope);

    const headers = new HttpHeaders({
        'Content-Type': 'application/x-www-form-urlencoded',
        Accept: 'text/plain'
    });

    return this.http.post(`${this.baseUrl}/connect/token`, body.toString(), { headers }).pipe(
        concatMap((res: any) => {
            Utils.setLocalStorage('auth', { accessToken: res.access_token, refreshToken: res.refresh_token });
            const redirect_uri = Utils.getSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN) || '/';
            this.router.navigate([redirect_uri]);
            return of(res);
        })
    );
}

logout() {
    Utils.clearLocalStorage();
    Utils.clearSessionStorage();
    this.router.navigate(['auth/login']);
}
```

Có sẵn cả luồng authorization code + PKCE (`postConnectAuthorize`, `Utils.generatePKCECodes()`) cho đăng nhập kiểu SSO.

## SignalR

Không có service dùng chung — từng màn hình tự tạo connection. Xem [02-component.md](02-component.md) mục "Màn hình real-time". Hằng số ở `shared/constants/sv-nhan-bang.constants.ts`:

```ts
export class TraoBangHubConst {
    static HUB = environment.baseUrl + '/hub/trao-bang';
    static ReceiveSinhVienDangTrao = 'ReceiveSinhVienDangTrao';
    static ReceiveChonKhoa = 'ReceiveChonKhoa';
    static ReceiveCheckIn = 'ReceiveCheckIn';
}
```

Tên event phải khớp `ITraoBangHub` bên BE.

## Gotcha

- Field `custommData` trong `IBaseResponsePagingData` **gõ sai 2 chữ m** so với `customData` mà BE trả về. Không ai dùng field này; đừng sửa nếu không rà hết chỗ dùng.
- `error` callback chỉ bắt lỗi **HTTP** (5xx, network). Lỗi nghiệp vụ về dưới dạng HTTP 200 nên **luôn phải kiểm `isResponseSucceed`** ở `next`.
- Một số service đặt tên file không có `.service` (`system-trao-bang.ts`, `guest-sv-nhan-bang.ts`). File mới nên đặt `<ten>.service.ts`.
- Tên class service không nhất quán (`TraoBangPlanService` vs `SlideService`). Xem [07-dat-ten.md](07-dat-ten.md).
