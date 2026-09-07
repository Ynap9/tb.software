# 08 — Checklist thêm màn hình mới

Ví dụ xuyên suốt: thêm màn hình quản lý **`ThongBao`** (danh sách + thêm/sửa/xóa), khớp với ví dụ BE ở `.claude/docs/be/08-checklist-them-feature.md`.

## 1. Model

`src/app/models/traobang/thong-bao.models.ts`

```ts
import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IViewRowThongBao {
    id?: number;
    idPlan?: number;
    noiDung?: string;
    order?: number;
    isShow?: boolean;
    createdDate?: string;
}

export interface IFindPagingThongBao extends IBaseRequestPaging {
    idPlan?: number;
}

export interface ICreateThongBao {
    idPlan: number;
    noiDung: string;
    isShow?: boolean;
}

export interface IUpdateThongBao extends ICreateThongBao {
    id: number;
}
```

Có trạng thái hiển thị dạng tag thì thêm class constants trong cùng file, theo mẫu `PlanTrangThai` (xem [06-models-va-constants.md](06-models-va-constants.md)).

## 2. Service

`src/app/service/thong-bao.service.ts`

```ts
import { ICreateThongBao, IFindPagingThongBao, IUpdateThongBao, IViewRowThongBao } from '@/models/traobang/thong-bao.models';
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ThongBaoService {
    api = '/api/core/trao-bang/thong-bao';
    http = inject(HttpClient);

    findPaging(query: IFindPagingThongBao) {
        return this.http.get<IBaseResponsePaging<IViewRowThongBao>>(this.api, { params: { ...query } });
    }

    getById(id: string) {
        return this.http.get<IBaseResponseWithData<IViewRowThongBao>>(`${this.api}/${id}`);
    }

    create(body: ICreateThongBao) {
        return this.http.post<IBaseResponse>(`${this.api}`, body);
    }

    update(body: IUpdateThongBao) {
        return this.http.put<IBaseResponse>(`${this.api}/${body.id}`, body);
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }
}
```

`api` là path trần — không ghép `environment.baseUrl`.

## 3. Permission constant

`src/app/shared/constants/permission.constants.ts` — thêm đúng chuỗi đã khai bên BE:

```ts
static MenuCauHinhThongBao = this.MenuCauHinh + "_ThongBao";

static ThongBaoAdd = this.Function + "ThongBaoAdd";
static ThongBaoUpdate = this.Function + "ThongBaoUpdate";
static ThongBaoDelete = this.Function + "ThongBaoDelete";
static ThongBaoView = this.Function + "ThongBaoView";
```

## 4. Thư mục màn hình

```
src/app/pages/trao-bang/cau-hinh/thong-bao/
├── thong-bao.ts / .html / .scss     ← class ThongBao (trang danh sách)
├── create/
│   └── create.ts / .html / .scss    ← class Create (dialog thêm + sửa)
└── tbl-action/
    └── tbl-action.ts / .html        ← class TblAction + const TblActionTypes
```

Copy khung từ `pages/trao-bang/cau-hinh/plan/` rồi đổi tên. Chi tiết từng mẫu ở [02-component.md](02-component.md).

Điểm không được quên trong trang danh sách:

```ts
export class ThongBao extends BaseComponent {
    _thongBaoService = inject(ThongBaoService);

    query: IFindPagingThongBao = {
        pageNumber: this.START_PAGE_NUMBER,
        pageSize: this.MAX_PAGE_SIZE
    };

    override ngOnInit(): void {          // ← `override` bắt buộc
        this.getData();
    }

    getData() {
        this.loading = true;
        this._thongBaoService.findPaging({ ...this.query, keyword: this.searchForm.get('search')?.value })
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, false)) {
                        this.data = res.data.items;
                        this.totalRecords = res.data.totalItems;   // ← không gán thì paginator sai
                    }
                }
            })
            .add(() => { this.loading = false; });
    }
}
```

## 5. Route

`src/app/pages/trao-bang/trao-bang.routes.ts` — import tĩnh component, thêm object route:

```ts
import { ThongBao } from "./cau-hinh/thong-bao/thong-bao";

export default [
    ...
    {
        path: 'config/thong-bao',
        title: 'Thông báo',
        data: { breadcrumb: 'thong-bao', permission: PermissionConstants.MenuCauHinhThongBao },
        component: ThongBao,
        canActivate: [permissionGuard]
    },
] as Routes
```

## 6. Menu sidebar

`src/app/layout/component/app.menu.ts` — thêm vào `items` của nhóm "Cấu hình":

```ts
{
    label: 'Thông báo',
    visible: this._sharedService.isGranted(PermissionConstants.MenuCauHinhThongBao),
    routerLink: ['/trao-bang/config/thong-bao'],
    icon: 'pi pi-bell'
}
```

Quên bước này thì màn hình chỉ vào được bằng URL.

## 7. Nếu cần real-time

Màn hình cần cập nhật khi BE bắn SignalR:

1. Thêm hằng vào `TraoBangHubConst` (`shared/constants/sv-nhan-bang.constants.ts`) — **tên khớp chính xác** `ITraoBangHub` bên BE.
2. Trong component: `connectHub()` + `hubConnection.on(...)` gọi lại hàm `get...()`, và `ngOnDestroy()` gọi `hubConnection?.stop()`. Mẫu ở [02-component.md](02-component.md).

## 8. Kiểm tra

**Không tự chạy build** — user tự build tay. Sửa xong thì báo lại những chỗ dễ vỡ khi build:

- `override ngOnInit()` — thiếu `override` là lỗi biên dịch (`noImplicitOverride`)
- Kiểu dữ liệu trong template — `strictTemplates: true` bắt rất chặt
- Tên field model phải khớp `View...Dto` bên BE

Lệnh để user tự chạy: `npm run build`, `npm run format`, `npm start`.

## Checklist rút gọn

- [ ] `models/traobang/<resource>.models.ts`: `IViewRow` / `IFindPaging` / `ICreate` / `IUpdate` (+ class constants nếu có trạng thái)
- [ ] `service/<resource>.service.ts`: `providedIn: 'root'`, `api` path trần, `http = inject(HttpClient)`
- [ ] `permission.constants.ts` — khớp từng ký tự với `PermissionKeys.cs`
- [ ] Thư mục màn hình + `create/` + `tbl-action/`, copy khung từ `plan/`
- [ ] Component `extends BaseComponent`, `override ngOnInit()`, `imports: [SharedImports, DataTable]`
- [ ] Gán `totalRecords` sau khi load; map field text cho cột STATUS
- [ ] Route: `path` + `title` + `data: { breadcrumb, permission }` + `canActivate: [permissionGuard]`
- [ ] Mục menu trong `app.menu.ts` với `visible: isGranted(...)`
- [ ] SignalR (nếu cần): hằng số + `connectHub()` + `ngOnDestroy()`
- [ ] Báo lại cho user những chỗ cần chú ý khi build (**không tự chạy `npm run build`**)
