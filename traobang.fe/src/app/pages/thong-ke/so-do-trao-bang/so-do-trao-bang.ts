import { Component, ElementRef, ViewChild, signal } from '@angular/core';
import { BUOC_NHAN_BANG, DAY_GHE_TANG_2, HANG_GHE, IBuocNhanBang, IPhongCho, KHU_VUC_NGOI, PHONG_CHO, TANG_3 } from '../data/so-do.data';

/** Hai chế độ xem của màn sơ đồ */
type CheDo = 'cho-ngoi' | 'buoc-di';

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
    /** vị trí đặt tên khu vực */
    tenX: number;
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

/** Một dòng trong bảng phòng chờ, gộp các khoa dùng chung một phòng */
interface IDongPhongCho extends IPhongCho {
    /** chỉ dòng đầu của nhóm mới in tên phòng */
    hienPhong: boolean;
    gopDong: number;
}

// ---------- kích thước sơ đồ chỗ ngồi ----------
const T2 = { x: 60, y: 60, w: 520, h: 880 };
const T3 = { x: 680, y: 60, w: 520, h: 880 };
/** hàng ghế đầu tiên bắt đầu ngay dưới khu vực sân khấu */
const HANG_Y0 = 214;
const HANG_CAO = 30;
const GHE_CAO = 15;

@Component({
    selector: 'app-so-do-trao-bang',
    imports: [],
    templateUrl: './so-do-trao-bang.html',
    styleUrl: './so-do-trao-bang.scss'
})
export class SoDoTraoBang {
    @ViewChild('mapcard') mapcard?: ElementRef<HTMLElement>;
    @ViewChild('tip') tipEl?: ElementRef<HTMLElement>;

    readonly T2 = T2;
    readonly T3 = T3;

    cheDo = signal<CheDo>('cho-ngoi');

    /** id khu vực đang trỏ tới ở sơ đồ chỗ ngồi */
    khuVucOn = signal<string | null>(null);

    /** số bước đang trỏ tới ở sơ đồ đường đi */
    buocOn = signal<number | null>(null);

    tipLeft = signal(0);
    tipTop = signal(0);

    // ---------- sơ đồ chỗ ngồi ----------

    khuVucs: IKhuVucVe[] = this.dungKhuVuc();
    nhanHangTrai: INhanHang[] = this.dungNhanHang();
    nhanHangPhai: INhanHang[] = this.dungNhanHang();
    gheTang3: IGhe[] = this.dungGheTang3();

    /** khung xanh của khu vực phụ huynh tầng 3 */
    bangTang3 = { x: T3.x + 10, y: 572, w: T3.w - 20, h: 330 };

    /** mảng khối bê tông chéo ở tầng 3, vẽ theo bản in */
    wedgeTang3 = `M ${T3.x + 470} 230 L ${T3.x + 470} 560 L ${T3.x + 80} 560 Z`;

    phongCho: IDongPhongCho[] = this.dungPhongCho();

    // ---------- sơ đồ các bước lên nhận bằng ----------

    buocs: IBuocNhanBang[] = BUOC_NHAN_BANG;

    /** toàn bộ chặng đường đi, gộp từ bốn bước */
    tatCaSeg: string[] = BUOC_NHAN_BANG.flatMap((b) => b.seg);

    /** mũi tên phụ đặt giữa mỗi chặng dài, để chiều di chuyển luôn nhìn thấy được */
    muiTenGiua: string[] = this.dungMuiTenGiua();

    /** các đường kẻ mô tả bậc lên sân khấu */
    bacSanKhau: number[] = [560, 575, 590, 605, 620];

    /** chỉ số ghế của các dãy trong sơ đồ đường đi */
    ghe4 = [0, 1, 2, 3];
    ghe11 = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    ghe17 = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

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

    doiCheDo(v: CheDo) {
        this.cheDo.set(v);
        this.clear();
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

    /**
     * Ba dãy ghế trong một hàng, chừa hai lối đi ở giữa.
     * Trả về toạ độ x của từng ghế.
     */
    private toaDoGhe(x0: number, rong: number, day: number[], loiDi: number): number[] {
        const tongGhe = day.reduce((a, n) => a + n, 0);
        const buoc = (rong - loiDi * (day.length - 1)) / tongGhe;

        const xs: number[] = [];
        let x = x0;
        day.forEach((soGhe, i) => {
            for (let g = 0; g < soGhe; g++) {
                xs.push(x + g * buoc);
            }
            x += soGhe * buoc + (i < day.length - 1 ? loiDi : 0);
        });
        return xs;
    }

    private dungKhuVuc(): IKhuVucVe[] {
        const x0 = T2.x + 12;
        const rong = T2.w - 24;
        const xs = this.toaDoGhe(x0, rong, DAY_GHE_TANG_2, 26);
        const rongGhe = ((rong - 26 * 2) / DAY_GHE_TANG_2.reduce((a, n) => a + n, 0)) * 0.78;

        return KHU_VUC_NGOI.map((kv) => {
            const soHang = kv.den - kv.tu + 1;
            const y = HANG_Y0 + kv.tu * HANG_CAO - 5;
            const h = soHang * HANG_CAO;

            const ghe: IGhe[] = [];
            for (let i = kv.tu; i <= kv.den; i++) {
                const gy = HANG_Y0 + i * HANG_CAO;
                xs.forEach((gx) => ghe.push({ x: gx, y: gy, w: rongGhe, h: GHE_CAO, mau: kv.mauGhe }));
            }

            return {
                id: kv.id,
                ten: kv.ten,
                mau: kv.mau,
                x: T2.x + 4,
                y,
                w: T2.w - 8,
                h,
                tenX: T2.x + T2.w / 2,
                tenY: y + h / 2 + 4,
                hangDau: HANG_GHE[kv.tu],
                hangCuoi: HANG_GHE[kv.den],
                soHang,
                soGhe: ghe.length,
                ghe
            };
        });
    }

    private dungNhanHang(): INhanHang[] {
        return HANG_GHE.map((label, i) => ({ label, y: HANG_Y0 + i * HANG_CAO + GHE_CAO / 2 + 4 }));
    }

    /** hàng x cố định của cột nhãn hàng ghế */
    nhanTraiX = T2.x - 15;
    nhanPhaiX = T2.x + T2.w + 15;

    private dungGheTang3(): IGhe[] {
        const x0 = T3.x + 20;
        const rong = T3.w - 40;
        const xs = this.toaDoGhe(x0, rong, TANG_3.day, 22);
        const rongGhe = ((rong - 22 * 2) / TANG_3.day.reduce((a, n) => a + n, 0)) * 0.78;

        const ghe: IGhe[] = [];
        for (let i = 0; i < TANG_3.soHang; i++) {
            const gy = 592 + i * 44;
            xs.forEach((gx) => ghe.push({ x: gx, y: gy, w: rongGhe, h: 18, mau: TANG_3.mauGhe }));
        }
        return ghe;
    }

    private dungPhongCho(): IDongPhongCho[] {
        return PHONG_CHO.map((p, i) => {
            const dauNhom = i === 0 || PHONG_CHO[i - 1].phong !== p.phong;

            // các khoa dùng chung một phòng thì gộp ô, chỉ dòng đầu in tên phòng
            let gop = 0;
            for (let j = i; dauNhom && j < PHONG_CHO.length && PHONG_CHO[j].phong === p.phong; j++) {
                gop++;
            }

            return { ...p, hienPhong: dauNhom, gopDong: gop };
        });
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
                if (len < 90) {
                    return;
                }
                const n = len > 320 ? 2 : 1;
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
