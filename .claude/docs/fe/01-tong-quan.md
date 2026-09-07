# 01 — Tổng quan

## Stack

| Thứ | Phiên bản / ghi chú |
|---|---|
| Angular | 20, **standalone** hoàn toàn, không có `NgModule` |
| UI | PrimeNG 20 + theme Aura (`@primeuix/themes`), template gốc là **Sakai** |
| CSS | Tailwind 4 (qua `@tailwindcss/postcss`) + `tailwindcss-primeui` |
| Icon | `primeicons` (`pi pi-*`) + `@ng-icons/heroicons` (`heroIcon`) |
| Real-time | `@microsoft/signalr` |
| Khác | `moment` (format ngày), `jwt-decode`, `chart.js`, GrapesJS (`@grapesjs/studio-sdk`) cho màn giao diện |

Angular project vẫn mang tên `sakai-ng`, build ra `dist/sakai-ng`.

## Cấu trúc thư mục

```
src/
├── main.ts, index.html
├── app.component.ts          root component
├── app.config.ts             provider toàn app
├── app.routes.ts             route gốc
├── config/
│   ├── auth.interceptor.ts   gắn token + baseUrl + refresh 401
│   └── icons.ts              đăng ký heroicons
├── environments/             environment.ts (prod) / .staging.ts / .development.ts
├── assets/styles.scss
└── app/
    ├── layout/               shell của Sakai (topbar, sidebar, menu, footer, layout.service)
    ├── models/               interface & class constants theo domain
    │   ├── auth/             user, role, permission
    │   └── traobang/         plan, sub-plan, slide, giao-dien, sv-nhan-bang
    ├── service/              service gọi API (một file / một resource)
    ├── shared/
    │   ├── components/       base-component, data-table, breadcrumb
    │   ├── constants/        auth, permission, data-table, sv-nhan-bang
    │   ├── directives/       enter-key, shift-enter-key
    │   ├── guard/            auth-guard, permission-guard
    │   ├── models/           kiểu dùng chung (paging, column, jwt, environment)
    │   ├── import.shared.ts  mảng SharedImports
    │   └── utils.ts          class Utils (static)
    └── pages/
        ├── auth/             login, callback, access, error
        ├── trao-bang/        nghiệp vụ chính
        │   ├── cau-hinh/     plan, sub-plan, slide, giao-dien, sv-nhan-bang
        │   ├── mc-screen/    màn điều khiển
        │   ├── scan-qr-sv/   màn checkin
        │   ├── main-screen/ side-screen/ san-khau-main/  màn chiếu
        │   └── guest-profile/
        ├── user-management/  user, role
        └── (crud, dashboard, uikit, landing, documentation, empty, notfound)  ← demo của Sakai, KHÔNG phải code nghiệp vụ
```

> `pages/uikit/`, `pages/crud/`, `pages/dashboard/`, `pages/landing/`, `pages/service/` là phần demo còn lại của template Sakai. **Không sửa, không lấy làm mẫu** — style ở đó khác hẳn code nghiệp vụ.

## Path alias

`tsconfig.json`:

```json
"paths": { "@/*": ["src/app/*"] }
```

Cách import đang dùng:

```ts
import { BaseComponent } from '@/shared/components/base/base-component';
import { TraoBangPlanService } from '@/service/plan.service';
import { IViewRowConfigPlan } from '@/models/traobang/plan.models';
import { environment } from 'src/environments/environment';   // ← environment KHÔNG dùng alias
```

Import trong cùng thư mục thì dùng relative (`./create/create`, `../scan-qr-sv/student-list/student-list`).

## Bootstrap — `src/app.config.ts`

```ts
export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(appRoutes, withInMemoryScrolling({...}), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
        provideAnimationsAsync(),
        providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: '.app-dark' } } }),
        provideNgIconsConfig({ size: '1.5em' }),
        provideIcons({ ...heroIcons }),
        MessageService,
        DialogService,
        ConfirmationService,
    ]
};
```

`MessageService`, `DialogService`, `ConfirmationService` provide ở root → `BaseComponent` inject được ở mọi nơi mà không cần khai lại trong `providers` của component.

## Layout

`AppLayout` (`app/layout/component/app.layout.ts`) là shell của mọi trang đã đăng nhập:

```
app-topbar → app-sidebar → <router-outlet> + <p-toast> + <p-confirmdialog> → app-footer
```

`<p-toast>` và `<p-confirmdialog>` đặt **một lần** ở đây → component con chỉ cần gọi `messageSuccess()` / `confirmDelete()`, không tự đặt thẻ toast.

Hệ quả: màn hình nằm ngoài `AppLayout` (các route `/guest/**`) **không có toast/confirm dialog**, nên không dùng `messageError()` ở đó được.

## Environment

```ts
export const environment: IEnvironment = {
    production: true,
    baseUrl: 'https://traobangapi.huce.edu.vn',
    authGrantType: 'password',
    authClientId: 'client-web',
    authClientSecret: '...',
    authScope: 'openid offline_access',
    appUrl: 'https://traobang.huce.edu.vn',
    grapeJsLicense: '...',
    minioUrl: 'https://s3-2.huce.edu.vn:9000'
};
```

Ba file: `environment.ts` (production), `.staging.ts`, `.development.ts`. Hoán đổi bằng `fileReplacements` trong `angular.json`. Thêm key mới → phải thêm vào **cả ba file** và interface `IEnvironment` (`shared/models/environment.models.ts`).

## Lệnh

```bash
npm start                     # ng serve → localhost:4200, config development
npm run build                 # production
npm run build:staging
npm run format                # prettier: 4 space, single quote, printWidth 250
npm test                      # Karma + Jasmine
```

Không có script `lint` chạy được (`eslint.config.js` là format legacy, plugin chưa cài). Format bằng Prettier là đủ.
