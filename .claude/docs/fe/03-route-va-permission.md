# 03 — Route & Permission

## Sơ đồ route

`src/app.routes.ts`:

```ts
export const appRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', redirectTo: 'trao-bang', pathMatch: 'full' },
            { path: 'user-management', loadChildren: () => import('./app/pages/user-management/user-management.routes') },
            { path: 'trao-bang',       loadChildren: () => import('./app/pages/trao-bang/trao-bang.routes') },
            { path: 'uikit',           loadChildren: () => import('./app/pages/uikit/uikit.routes') },
            { path: 'documentation',   component: Documentation },
            { path: 'pages',           loadChildren: () => import('./app/pages/pages.routes') }
        ]
    },
    { path: 'guest', children: [{ path: 'trao-bang/profile',      component: GuestProfile, title: 'Thông tin sinh viên nhận bằng' }] },
    { path: 'guest', children: [{ path: 'trao-bang/main-screen',  component: MainScreen,   title: 'Sân khấu' }] },
    { path: 'guest', children: [{ path: 'trao-bang/side-screen',  component: SideScreen,   title: 'Cánh gà' }] },
    { path: 'landing',  component: Landing },
    { path: 'notfound', component: Notfound },
    { path: 'auth',     loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
```

Hai vùng tách bạch:

| Vùng | Layout | Guard | Dùng cho |
|---|---|---|---|
| `''` (root) | `AppLayout` | `authGuard` | Toàn bộ màn hình quản trị và điều khiển |
| `/guest/**` | không có layout | **không guard** | Màn chiếu sân khấu, cánh gà, trang SV tra cứu bằng QR |

`/guest/**` **cố ý không xác thực** — máy chiếu và điện thoại sinh viên không đăng nhập. API tương ứng ở BE gắn `[AllowAnonymous]`. Đừng thêm guard vào đây.

## Route con của feature

`pages/trao-bang/trao-bang.routes.ts` — `export default [...] as Routes`:

```ts
export default [
    {
        path: 'config/plan',
        title: 'Chương trình',
        data: { breadcrumb: 'plan', permission: PermissionConstants.MenuCauHinhChuongTrinh },
        component: Plan,
        canActivate: [permissionGuard]
    },
    { path: 'config/sub-plan',  title: 'Khoa',        data: { ..., permission: PermissionConstants.MenuCauHinhKhoa },      component: SubPlan,   canActivate: [permissionGuard] },
    { path: 'scan-qr-sv',       title: 'Checkin',     data: { ..., permission: PermissionConstants.MenuManHinhCheckin },   component: ScanQrSv,  canActivate: [permissionGuard] },
    { path: 'mc-screen',        title: 'Điều khiển',  data: { ..., permission: PermissionConstants.MenuManHinhDieuKhien }, component: McScreen,  canActivate: [permissionGuard] },
] as Routes
```

Bốn thứ mỗi route phải có:

1. `path` — kebab-case; nhóm cấu hình đặt dưới `config/` (`config/plan`, `config/sub-plan`, `config/slide`, `config/giao-dien`).
2. `title` — tiếng Việt có dấu, hiện trên tab trình duyệt.
3. `data: { breadcrumb, permission }`.
4. `canActivate: [permissionGuard]`.

Component import **tĩnh** ở đầu file (không lazy từng component) — lazy đã xảy ra ở mức `loadChildren` của route cha.

## `authGuard`

`shared/guard/auth-guard.ts` — chạy ở route gốc, làm 3 việc:

```ts
export const authGuard: CanActivateFn = async (route, state) => {
    const authObject = Utils.getLocalStorage('auth');
    const accessToken = authObject?.accessToken;

    try {
        const res = await firstValueFrom(_userService.getMe());
        _sharedService.setRoles(res.data.roles || []);
        _sharedService.setPermissions(res.data.permissions || []);
    } catch (error) {
        router.navigate(['auth/login'], { queryParams: { redirect_uri: state.url } });
    }

    if (!accessToken) {
        router.navigate(['auth/login'], { queryParams: { redirect_uri: state.url } });
    }

    const jwtPayload = Utils.getDecodedJwtPayload();
    if (jwtPayload.user_type === 'SV') router.navigate(['auth/access']);

    return true;
};
```

Quan trọng: **quyền được nạp vào `SharedService` ở đây**, mỗi lần vào vùng đã đăng nhập. Không có nơi nào khác nạp quyền → `permissionGuard` và menu phụ thuộc hoàn toàn vào việc `authGuard` chạy trước.

## `permissionGuard`

```ts
export const permissionGuard: CanActivateFn = (route, state) => {
    const requiredPermission = route.data['permission'] as string;

    if (_sharedService.roles.includes(AuthConstants.SUPER_ADMIN_ROLE)) return true;
    if (_sharedService.permissions?.includes(requiredPermission)) return true;

    router.navigate(['auth/access']);
    return false;
};
```

Một route = **một** permission key. Không hỗ trợ danh sách OR (khác BE — `[Permission]` ở BE nhận nhiều key).

## `SharedService` — kho quyền phía client

```ts
@Injectable({ providedIn: 'root' })
export class SharedService {
    get permissions(): string[]
    get roles(): string[]
    isGranted(permission: string): boolean   // true nếu là SuperAdmin hoặc có permission
    setPermissions(data: string[])
    setRoles(data: string[])
    clearAll()
}
```

Ẩn/hiện nút, cột, menu trong component → dùng `_sharedService.isGranted(PermissionConstants.X)`.

## Permission constants

`shared/constants/permission.constants.ts` là **bản chép tay** của `PermissionKeys.cs` bên BE:

```ts
export class PermissionConstants {
    static Menu = "Menu.";
    static Function = "Function.";

    static MenuManHinh = this.Menu + "ManHinh";
    static MenuManHinhSanKhau = this.MenuManHinh + "_SanKhau";
    static MenuCauHinh = this.Menu + "_CauHinh";
    static MenuCauHinhChuongTrinh = this.MenuCauHinh + "_ChuongTrinh";

    static UserAdd = this.Function + "UserAdd";
    static PlanAdd = this.Function + "PlanAdd";
    ...
}
```

**Thêm/sửa key thì phải sửa cả hai phía** (`traobang.be.shared/Constants/Auth/PermissionKeys.cs`). Chuỗi ghép ra phải khớp từng ký tự, nếu không quyền sẽ luôn fail.

## Menu sidebar

`layout/component/app.menu.ts` — mảng `model` dựng trong `ngOnInit`, mỗi mục có `visible` = `isGranted(...)`:

```ts
{
    label: 'Cấu hình',
    visible: this._sharedService.isGranted(PermissionConstants.MenuCauHinh),
    expanded: true,
    styleClass: 'header-label',
    icon: 'pi pi-cog',
    items: [
        {
            label: 'Chương trình',
            visible: this._sharedService.isGranted(PermissionConstants.MenuCauHinhChuongTrinh),
            routerLink: ['/trao-bang/config/plan'],
            icon: 'pi pi-list'
        },
        {
            label: 'Giao diện',
            visible: this._sharedService.isGranted(PermissionConstants.MenuCauHinhGiaoDien),
            routerLink: ['/trao-bang/config/giao-dien'],
            heroIcon: 'heroPaintBrush'          // dùng ng-icons thay cho primeicons
        }
    ]
}
```

Icon: `icon: 'pi pi-*'` (primeicons) hoặc `heroIcon: 'heroXxx'` (ng-icons/heroicons, phải có trong `config/icons.ts`).

Thêm màn hình mới mà quên thêm mục menu → truy cập được bằng URL nhưng không ai tìm thấy.

## Điều hướng trong code

```ts
this.router.navigate(['trao-bang/config/giao-dien/create'], {
    queryParams: {
        id: encodeURIComponent(JSON.stringify(data.idGiaoDien)),
        isPlan: true
    }
});
```

`this.router` có sẵn từ `BaseComponent` (`protected router = inject(Router)`), không cần inject lại. Đọc query param qua `this._activatedRoute` (cũng có sẵn).

## Gotcha

- `data.breadcrumb` được khai ở mọi route nhưng **không có code nào đọc**. `app-breadcrumb` chỉ dùng thủ công ở đúng một trang (`create-giao-dien`). Cứ khai theo cho đồng bộ, đừng kỳ vọng nó hiển thị.
- `authGuard` **luôn `return true`** kể cả khi đã gọi `router.navigate` sang login — chuyển hướng vẫn xảy ra nhưng guard không chặn. Đây là hành vi hiện tại.
- `authGuard` gọi `Utils.getDecodedJwtPayload()` không có token sẽ ném lỗi; đường đi thực tế được che bởi `try/catch` phía trên. Đừng dựa vào guard này để bảo vệ dữ liệu — BE mới là nơi chặn thật.
- Route `/guest/**` bị tách thành **ba object riêng** cùng `path: 'guest'` thay vì gộp `children`. Angular vẫn khớp đúng; thêm route guest mới thì thêm object thứ tư cho đồng bộ, hoặc gộp vào object đầu — cả hai đều chạy.
