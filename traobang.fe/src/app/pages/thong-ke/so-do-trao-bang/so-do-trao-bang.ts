import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BO_PHAN, DS_BO_PHAN } from '../data/thong-ke.data';
import { IBoPhan } from '../models/thong-ke.models';

/** Một ghế trong khu vực chờ làm thủ tục */
interface IGhe {
    x: number;
    y: number;
    w: number;
    h: number;
}

@Component({
    selector: 'app-so-do-trao-bang',
    imports: [],
    templateUrl: './so-do-trao-bang.html',
    styleUrl: './so-do-trao-bang.scss'
})
export class SoDoTraoBang {
    @ViewChild('mapcard') mapcard?: ElementRef<HTMLElement>;
    @ViewChild('tip') tipEl?: ElementRef<HTMLElement>;

    /** danh sách bộ phận hiển thị ở khung chú giải bên phải */
    dsBoPhan: IBoPhan[] = DS_BO_PHAN;

    /** mã bộ phận đang được trỏ tới, null nghĩa là không có gì đang chọn */
    active = signal<string | null>(null);

    /** vị trí tooltip so với khung sơ đồ */
    tipLeft = signal(0);
    tipTop = signal(0);

    /** ghế khu vực chờ: 3 khối, chừa 2 lối đi cho tuyến di chuyển */
    ghe: IGhe[] = this.dungGhe();

    /** các chặng của tuyến di chuyển, theo đúng thứ tự sinh viên đi qua */
    readonly SEG: string[] = [
        'M100.9 1019.9 L484.9 1019.9 L484.9 285.5',
        'M484.9 250.5 L217.6 250.5',
        'M197.2 250.5 L197.2 117.5 L437.0 117.5',
        'M451.1 117.5 L562.1 117.5 L562.1 215.1 L696.3 215.1',
        'M708.0 215.1 L970.7 215.1 L970.7 237.6',
        'M970.7 312.7 L970.7 412.7',
        'M970.7 501.2 L1091.7 501.2 L1091.7 913.1',
        'M1091.7 938.2 L1091.7 978.2 L1175.1 978.2 L1175.1 1001.6',
        'M1175.1 1079.9 L1175.1 1153.3 L1072.5 1153.3 L1072.5 1193.4',
        'M1048.3 1236.7 L931.5 1236.7',
        'M879.0 1236.7 L837.3 1236.7 L837.3 886.5 L778.9 886.5',
        'M758.1 886.5 L758.1 819.7 L819.8 819.7'
    ];

    /** mũi tên phụ đặt giữa mỗi chặng dài, để chiều di chuyển luôn nhìn thấy được */
    muiTenGiua: string[] = this.dungMuiTenGiua();

    private router = inject(Router);

    /** thông tin bộ phận đang trỏ tới, dùng cho tooltip */
    get boPhanDangXem(): IBoPhan | null {
        const id = this.active();
        return id ? { id, ...BO_PHAN[id] } : null;
    }

    /** bỏ phần số ở mã bộ phận: E25 -> E */
    chuCai(id: string): string {
        return id.replace(/\d+/, '');
    }

    // ---------- tương tác ----------

    /** trỏ vào một bộ phận trên sơ đồ hoặc trong khung chú giải */
    activate(id: string, e: MouseEvent) {
        this.active.set(id);
        this.datViTri(e.clientX, e.clientY);
    }

    /** di chuột trong vùng một bộ phận: tooltip bám theo con trỏ */
    place(e: MouseEvent) {
        if (this.active()) {
            this.datViTri(e.clientX, e.clientY);
        }
    }

    /** tab bằng bàn phím tới một bộ phận: tooltip đặt ở giữa hình */
    focusPhanTu(id: string, el: EventTarget | null) {
        this.active.set(id);
        const box = (el as Element | null)?.getBoundingClientRect();
        if (box) {
            this.datViTri(box.left + box.width / 2, box.top + box.height / 2);
        }
    }

    /** trỏ vào một dòng trong khung chú giải: tooltip đặt trên hình tương ứng */
    hoverChuGiai(id: string) {
        this.active.set(id);
        const st = this.mapcard?.nativeElement.querySelector(`.station[data-id="${id}"]`);
        if (st) {
            const b = st.getBoundingClientRect();
            this.datViTri(b.left + b.width / 2, b.top + b.height / 2);
        }
    }

    clear() {
        this.active.set(null);
    }

    /** bấm vào nhãn trên sơ đồ để mở danh sách các khoa */
    moDanhSachKhoa() {
        this.router.navigate(['/thong-ke/khoa']);
    }

    /** giữ tooltip nằm gọn trong khung sơ đồ */
    private datViTri(clientX: number, clientY: number) {
        const card = this.mapcard?.nativeElement;
        if (!card) {
            return;
        }
        const r = card.getBoundingClientRect();
        const cao = this.tipEl?.nativeElement.offsetHeight ?? 0;

        let x = clientX - r.left + 18;
        let y = clientY - r.top + 16;

        if (x + 300 > r.width) {
            x = clientX - r.left - 306;
        }
        if (y + cao > r.height) {
            y = r.height - cao - 10;
        }
        if (y < 6) {
            y = 6;
        }

        this.tipLeft.set(x);
        this.tipTop.set(y);
    }

    // ---------- dựng hình ----------

    private dungGhe(): IGhe[] {
        const blocks = [
            { x0: 82.5, step: 30.6, w: 26.6, cols: 3 },
            { x0: 220.1, step: 34.6, w: 30.6, cols: 7 },
            { x0: 507.9, step: 30.6, w: 26.6, cols: 3 }
        ];

        const ds: IGhe[] = [];
        blocks.forEach((b) => {
            for (let c = 0; c < b.cols; c++) {
                for (let r = 0; r < 23; r++) {
                    const x = b.x0 + c * b.step;
                    const y = 324 + r * 25;
                    // chừa chỗ cho dòng chữ "KHU VỰC CHỜ LÀM THỦ TỤC"
                    if (x > 207.6 && x < 474.5 && y > 365.2 && y < 465.3) {
                        continue;
                    }
                    ds.push({ x: +x.toFixed(1), y, w: b.w, h: 17 });
                }
            }
        });
        return ds;
    }

    /** tách một chặng thành các đoạn thẳng thành phần */
    private cacDoan(d: string): number[][] {
        const pts = (d.match(/-?\d+(\.\d+)?/g) ?? []).map(Number);
        const out: number[][] = [];
        for (let i = 2; i < pts.length; i += 2) {
            out.push([pts[i - 2], pts[i - 1], pts[i], pts[i + 1]]);
        }
        return out;
    }

    private dungMuiTenGiua(): string[] {
        const out: string[] = [];
        this.SEG.forEach((d) => {
            this.cacDoan(d).forEach(([x1, y1, x2, y2]) => {
                const len = Math.hypot(x2 - x1, y2 - y1);
                if (len < 70) {
                    return;
                }
                const n = len > 240 ? 2 : 1;
                for (let k = 1; k <= n; k++) {
                    const t = k / (n + 1);
                    const mx = x1 + (x2 - x1) * t;
                    const my = y1 + (y2 - y1) * t;
                    const ux = (x2 - x1) / len;
                    const uy = (y2 - y1) / len;
                    out.push(`M${(mx - ux * 10).toFixed(1)} ${(my - uy * 10).toFixed(1)} L${mx.toFixed(1)} ${my.toFixed(1)}`);
                }
            });
        });
        return out;
    }
}
