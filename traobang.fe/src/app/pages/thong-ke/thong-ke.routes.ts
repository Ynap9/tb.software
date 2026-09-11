import { Routes } from '@angular/router';
import { ThongKeLayout } from './layout/thong-ke-layout';
import { SoDoTraoBang } from './so-do-trao-bang/so-do-trao-bang';
import { DanhSachKhoa } from './danh-sach-khoa/danh-sach-khoa';
import { ChiTietKhoa } from './chi-tiet-khoa/chi-tiet-khoa';
import { DanhSachSinhVien } from './danh-sach-sinh-vien/danh-sach-sinh-vien';
import { ThongKeTraoBang } from './thong-ke-trao-bang/thong-ke-trao-bang';

/**
 * Nhóm màn thống kê trao bằng.
 * Cố ý không gắn authGuard / permissionGuard: đây là màn công khai,
 * ai có link đều xem được, giống các màn /guest của trao bằng.
 */
export default [
    {
        path: '',
        component: ThongKeLayout,
        children: [
            { path: '', redirectTo: 'so-do', pathMatch: 'full' },
            { path: 'so-do', title: 'Sơ đồ trao bằng', component: SoDoTraoBang },
            { path: 'khoa', title: 'Danh sách các khoa', component: DanhSachKhoa },
            { path: 'khoa/:id', title: 'Sinh viên nhận bằng theo khoa', component: ChiTietKhoa },
            { path: 'sinh-vien', title: 'Danh sách sinh viên nhận bằng', component: DanhSachSinhVien },
            { path: 'tong-hop', title: 'Thống kê trao bằng', component: ThongKeTraoBang }
        ]
    }
] as Routes;
