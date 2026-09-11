import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { Dashboard } from './app/pages/dashboard/dashboard';
import { Documentation } from './app/pages/documentation/documentation';
import { Landing } from './app/pages/landing/landing';
import { Notfound } from './app/pages/notfound/notfound';
import { authGuard } from '@/shared/guard/auth-guard';
import { GuestProfile } from '@/pages/trao-bang/guest-profile/guest-profile';
import { MainScreen } from '@/pages/trao-bang/main-screen/main-screen';
import { SideScreen } from '@/pages/trao-bang/side-screen/side-screen';
import { SanKhauMain } from '@/pages/trao-bang/san-khau-main/san-khau-main';

export const appRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', redirectTo: 'trao-bang', pathMatch: 'full' },

            { path: 'user-management', loadChildren: () => import('./app/pages/user-management/user-management.routes') },
            { path: 'trao-bang', loadChildren: () => import('./app/pages/trao-bang/trao-bang.routes') },
            { path: 'uikit', loadChildren: () => import('./app/pages/uikit/uikit.routes') },
            { path: 'documentation', component: Documentation },
            { path: 'pages', loadChildren: () => import('./app/pages/pages.routes') }
        ]
    },
    {
        path: 'guest',
        children: [{ path: 'trao-bang/profile', component: GuestProfile, title: 'Thông tin sinh viên nhận bằng' }]
    },
    {
        path: 'guest',
        children: [{ path: 'trao-bang/main-screen', component: MainScreen, title: 'Sân khấu' }]
    },
    {
        path: 'guest',
        children: [{ path: 'trao-bang/side-screen', component: SideScreen, title: 'Cánh gà' }]
    },
    // Thống kê nhập học K71: màn công khai, không cần đăng nhập
    { path: 'thong-ke', loadChildren: () => import('./app/pages/thong-ke/thong-ke.routes') },
    { path: 'landing', component: Landing },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
