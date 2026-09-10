import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/** Một mục trên thanh điều hướng bên trái */
interface ITabThongKe {
    path: string;
    label: string;
    /** đường dẫn của icon SVG, vẽ bằng stroke */
    icon: string[];
}

@Component({
    selector: 'app-thong-ke-layout',
    imports: [RouterOutlet, RouterLink, RouterLinkActive],
    templateUrl: './thong-ke-layout.html',
    styleUrl: './thong-ke-layout.scss'
})
export class ThongKeLayout {
    /** 'dark' hoặc 'light', mặc định tối như bản thiết kế gốc */
    theme = signal<'dark' | 'light'>('dark');

    /** logo tải lỗi thì hiện chữ HUCE thay ảnh */
    coLogo = signal(false);

    logoUrl = 'https://upload.wikimedia.org/wikipedia/commons/thumb/4/4b/LOGO_DHXD.png/500px-LOGO_DHXD.png?utm_source=vi.wikipedia.org&utm_campaign=index&utm_content=thumbnail';

    tabs: ITabThongKe[] = [
        {
            path: 'so-do',
            label: 'Sơ đồ trao bằng',
            icon: ['M1.6 4.4l4.3-2.1 4.2 2.1 4.3-2.1v9.3l-4.3 2.1-4.2-2.1-4.3 2.1z', 'M5.9 2.3v9.3M10.1 4.4v9.3']
        },
        {
            path: 'khoa',
            label: 'Danh sách các khoa',
            icon: ['M8 2.3l6.4 3.1L8 8.5 1.6 5.4z', 'M4.3 7v3.6C4.3 12 5.9 13 8 13s3.7-1 3.7-2.4V7']
        },
        {
            path: 'sinh-vien',
            label: 'Sinh viên nhận bằng',
            icon: ['M1.6 13.7c0-2.4 2-4 4.4-4s4.4 1.6 4.4 4', 'M11 3.1a2.5 2.5 0 010 4.4', 'M12.4 10.1c1.2.6 2 1.8 2 3.6']
        },
        {
            path: 'tong-hop',
            label: 'Thống kê trao bằng',
            icon: ['M8 8V2.2M8 8l4.1 2.4']
        }
    ];

    doiTheme() {
        this.theme.update((t) => (t === 'light' ? 'dark' : 'light'));
    }
}
