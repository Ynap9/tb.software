import { IThongKeKhoa } from '@/models/traobang/thong-ke.models';
import { ThongKeService } from '@/service/thong-ke.service';
import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { BUOC_NHAN_BANG, DAY_GHE, HANG_GHE, IBuocNhanBang, KHU_VUC_NGOI, VI_TRI_KHOA } from '../data/so-do.data';

/** Một ghế trên sơ đồ */
interface IGhe {
    x: number;
    y: number;
    w: number;
    h: number;
    mau: string;
}

/** Một khu vực chỗ ngồi đã tính sẵn toạ độ để vẽ */
interface IKhuVucVe {
    id: string;
    ten: string;
    mau: string;
    x: number;
    y: number;
    w: number;
    h: number;
    tenY: number;
    hangDau: string;
    hangCuoi: string;
    soHang: number;
    soGhe: number;
    ghe: IGhe[];
}

/** Nhãn hàng ghế ở hai mép hội trường */
interface INhanHang {
    label: string;
    y: number;
}

/** Một dòng trong danh sách khoa: số thứ tự, tên khoa, vị trí */
interface IDongKhoa {
    id?: number;
    /** thứ tự lên nhận bằng, đếm từ 1 */
    stt: number;
    ten: string;
    /** vị trí ngồi hoặc phòng chờ, null nếu chưa khai báo cho khoa này */
    viTri: string | null;
}

// ---------- kích thước mặt bằng hội trường ----------
/** tường hội trường */
const HALL = { x: 120, y: 100, w: 880, h: 880 };
/** sảnh phía sau hội trường */
const SANH = { y: 980, h: 110 };
/** hai hàng ghế đại biểu, ngay dưới bậc thềm sân khấu */
const DAI_BIEU_Y0 = 397;
const DAI_BIEU_CAO = 35;
/** các hàng ghế sinh viên, bắt đầu từ hàng B */
const HANG_Y0 = 480;
const HANG_CAO = 34;
const GHE_CAO = 15;
/** bề rộng lối đi giữa ba dãy ghế */
const LOI_DI = 44;

@Component({
    selector: 'app-so-do-trao-bang',
    imports: [],
    templateUrl: './so-do-trao-bang.html',
    styleUrl: './so-do-trao-bang.scss'
})
export class SoDoTraoBang implements OnInit {
    @ViewChild('mapcard') mapcard?: ElementRef<HTMLElement>;
    @ViewChild('tip') tipEl?: ElementRef<HTMLElement>;

    _thongKeService = inject(ThongKeService);

    readonly HALL = HALL;
    readonly SANH = SANH;

    /** id khu vực đang trỏ tới */
    khuVucOn = signal<string | null>(null);

    /** số bước đang trỏ tới */
    buocOn = signal<number | null>(null);

    tipLeft = signal(0);
    tipTop = signal(0);

    // ---------- khu vực chỗ ngồi ----------

    khuVucs: IKhuVucVe[] = this.dungKhuVuc();
    nhanHang: INhanHang[] = this.dungNhanHang();

    /** cột nhãn hàng ghế ở hai mép */
    nhanTraiX = HALL.x - 16;
    nhanPhaiX = HALL.x + HALL.w + 16;

    /** tâm hội trường, dùng để căn giữa các nhãn */
    giuaX = HALL.x + HALL.w / 2;

    // ---------- danh sách khoa lấy từ DB ----------

    /** các khoa của plan đang active */
    dsKhoa = signal<IThongKeKhoa[]>([]);
    dangTaiKhoa = signal(true);
    loiKhoa = signal<string | null>(null);

    /** tổng số khoa, là mẫu số của số thứ tự */
    tongKhoa = computed(() => this.dsKhoa().length);

    /** mỗi khoa kèm số thứ tự theo Order và vị trí tra từ bảng vị trí */
    dongKhoa = computed<IDongKhoa[]>(() =>
        [...this.dsKhoa()]
            .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
            .map((k, i) => ({
                id: k.id,
                stt: i + 1,
                ten: k.ten ?? '',
                viTri: this.timViTri(k.ten ?? '')
            }))
    );

    // ---------- luồng các bước lên nhận bằng ----------

    buocs: IBuocNhanBang[] = BUOC_NHAN_BANG;

    /** toàn bộ chặng đường đi, gộp từ bốn bước */
    tatCaSeg: string[] = BUOC_NHAN_BANG.flatMap((b) => b.seg);

    /** mũi tên phụ đặt giữa mỗi chặng dài, để chiều di chuyển luôn nhìn thấy được */
    muiTenGiua: string[] = this.dungMuiTenGiua();

    /** các đường kẻ mô tả bậc thềm bước lên sân khấu */
    bacSanKhau: number[] = [345, 357, 369, 381];

    /** thông tin khu vực đang trỏ tới, dùng cho tooltip */
    get khuVucDangXem(): IKhuVucVe | null {
        const id = this.khuVucOn();
        return id ? (this.khuVucs.find((x) => x.id === id) ?? null) : null;
    }

    /** thông tin bước đang trỏ tới, dùng cho tooltip */
    get buocDangXem(): IBuocNhanBang | null {
        const so = this.buocOn();
        return so ? (this.buocs.find((x) => x.so === so) ?? null) : null;
    }

    ngOnInit(): void {
        this.getDanhSachKhoa();
    }

    getDanhSachKhoa() {
        this.loiKhoa.set(null);

        // chỉ cần tên và thứ tự khoa, không kèm danh sách sinh viên
        this._thongKeService.getAllKhoa().subscribe({
            next: (res) => {
                this.dangTaiKhoa.set(false);
                if (res.status === 1) {
                    this.dsKhoa.set(res.data ?? []);
                } else {
                    this.loiKhoa.set(res.message || 'Không lấy được danh sách khoa.');
                }
            },
            error: () => {
                this.dangTaiKhoa.set(false);
                this.loiKhoa.set('Không kết nối được máy chủ. Vui lòng thử lại.');
            }
        });
    }

    /** bỏ dấu tiếng Việt, đưa về chữ thường để dò từ khoá */
    private boDau(s: string): string {
        return s.normalize('NFD').replace(/\p{M}/gu, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase();
    }

    /** tra vị trí của một khoa theo từ khoá trong tên khoa */
    private timViTri(ten: string): string | null {
        const t = this.boDau(ten);
        return VI_TRI_KHOA.find((v) => v.tuKhoa.some((k) => t.includes(k)))?.viTri ?? null;
    }

    // ---------- tương tác ----------

    hoverKhuVuc(id: string, e: MouseEvent) {
        this.buocOn.set(null);
        this.khuVucOn.set(id);
        this.datViTri(e.clientX, e.clientY);
    }

    hoverBuoc(so: number, e: MouseEvent) {
        this.khuVucOn.set(null);
        this.buocOn.set(so);
        this.datViTri(e.clientX, e.clientY);
    }

    /** di chuột trong vùng đang trỏ: tooltip bám theo con trỏ */
    place(e: MouseEvent) {
        if (this.khuVucOn() || this.buocOn()) {
            this.datViTri(e.clientX, e.clientY);
        }
    }

    clear() {
        this.khuVucOn.set(null);
        this.buocOn.set(null);
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

    /** toạ độ y của hàng ghế thứ i: hai hàng đầu là ghế đại biểu, còn lại là ghế sinh viên */
    private yHang(i: number): number {
        return i < 2 ? DAI_BIEU_Y0 + i * DAI_BIEU_CAO : HANG_Y0 + (i - 2) * HANG_CAO;
    }

    /**
     * Ba dãy ghế trong một hàng, chừa hai lối đi ở giữa.
     * Trả về toạ độ x của từng ghế.
     */
    private toaDoGhe(): number[] {
        const x0 = HALL.x + 12;
        const rong = HALL.w - 24;
        const tongGhe = DAY_GHE.reduce((a, n) => a + n, 0);
        const buoc = (rong - LOI_DI * (DAY_GHE.length - 1)) / tongGhe;

        const xs: number[] = [];
        let x = x0;
        DAY_GHE.forEach((soGhe, i) => {
            for (let g = 0; g < soGhe; g++) {
                xs.push(x + g * buoc);
            }
            x += soGhe * buoc + (i < DAY_GHE.length - 1 ? LOI_DI : 0);
        });
        return xs;
    }

    private dungKhuVuc(): IKhuVucVe[] {
        const xs = this.toaDoGhe();
        const tongGhe = DAY_GHE.reduce((a, n) => a + n, 0);
        const rongGhe = ((HALL.w - 24 - LOI_DI * 2) / tongGhe) * 0.78;

        return KHU_VUC_NGOI.map((kv) => {
            const soHang = kv.den - kv.tu + 1;
            const y = this.yHang(kv.tu) - 6;
            const h = this.yHang(kv.den) + GHE_CAO + 6 - y;

            const ghe: IGhe[] = [];
            for (let i = kv.tu; i <= kv.den; i++) {
                const gy = this.yHang(i);
                xs.forEach((gx) => ghe.push({ x: gx, y: gy, w: rongGhe, h: GHE_CAO, mau: kv.mauGhe }));
            }

            return {
                id: kv.id,
                ten: kv.ten,
                mau: kv.mau,
                x: HALL.x + 4,
                y,
                w: HALL.w - 8,
                h,
                tenY: y + h / 2 + 5,
                hangDau: HANG_GHE[kv.tu],
                hangCuoi: HANG_GHE[kv.den],
                soHang,
                soGhe: ghe.length,
                ghe
            };
        });
    }

    private dungNhanHang(): INhanHang[] {
        return HANG_GHE.map((label, i) => ({ label, y: this.yHang(i) + GHE_CAO / 2 + 4 }));
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
        this.tatCaSeg.forEach((d) => {
            this.cacDoan(d).forEach(([x1, y1, x2, y2]) => {
                const len = Math.hypot(x2 - x1, y2 - y1);
                if (len < 120) {
                    return;
                }
                const n = len > 360 ? 2 : 1;
                for (let k = 1; k <= n; k++) {
                    const t = k / (n + 1);
                    const mx = x1 + (x2 - x1) * t;
                    const my = y1 + (y2 - y1) * t;
                    const ux = (x2 - x1) / len;
                    const uy = (y2 - y1) / len;
                    out.push(`M${(mx - ux * 12).toFixed(1)} ${(my - uy * 12).toFixed(1)} L${mx.toFixed(1)} ${my.toFixed(1)}`);
                }
            });
        });
        return out;
    }
}
