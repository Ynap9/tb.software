import { IThongKeKhoa } from '@/models/traobang/thong-ke.models';
import { ThongKeService } from '@/service/thong-ke.service';
import { TraoBangHubConst } from '@/shared/constants/sv-nhan-bang.constants';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { TrangThaiKhoa } from '../models/thong-ke.models';

/** Khoá sắp xếp của bảng thống kê */
type SortKey = 'order' | 'ten' | 'pct' | 'daTrao' | 'conLai' | 'tong';

/** Một dòng thống kê của một khoa */
interface IDongThongKe {
    khoa: IThongKeKhoa;
    order: number;
    ten: string;
    tong: number;
    daTrao: number;
    conLai: number;
    vangMat: number;
    pct: number;
    /** g = đã trao xong, c = đang trao bằng, w = chờ tới lượt */
    cls: string;
    nhanTrangThai: string;
}

/** Ngưỡng tỷ lệ đã trao bằng coi là đạt (%) */
const GOAL = 85;

@Component({
    selector: 'app-thong-ke-trao-bang',
    imports: [],
    templateUrl: './thong-ke-trao-bang.html',
    styleUrl: './thong-ke-trao-bang.scss'
})
export class ThongKeTraoBang implements OnInit, OnDestroy {
    readonly GOAL = GOAL;

    _thongKeService = inject(ThongKeService);

    hubConnection: signalR.HubConnection | undefined;

    /** danh sách khoa của plan đang active, BE trả sẵn theo thứ tự lên nhận bằng */
    dsKhoa = signal<IThongKeKhoa[]>([]);

    dangTai = signal(true);

    /** thông báo lỗi khi gọi API thất bại, null nghĩa là không có lỗi */
    loi = signal<string | null>(null);

    query = signal('');
    sortKey = signal<SortKey>('order');
    sortDir = signal(1);

    cotBang: { k: SortKey; label: string; r: boolean; hideSm: boolean }[] = [
        { k: 'ten', label: 'Khoa', r: false, hideSm: false },
        { k: 'pct', label: 'Tỷ lệ đã trao', r: false, hideSm: false },
        { k: 'daTrao', label: 'Đã trao bằng', r: true, hideSm: false },
        { k: 'conLai', label: 'Còn lại', r: true, hideSm: true },
        { k: 'tong', label: 'Tổng số SV', r: true, hideSm: false }
    ];

    // ---------- số liệu tổng quan ----------

    hero = computed(() => {
        const ds = this.dsKhoa();
        const tong = ds.reduce((a, k) => a + (k.soLuongThamGia ?? 0), 0);
        const daTrao = ds.reduce((a, k) => a + (k.soLuongDaTrao ?? 0), 0);
        const vangMat = ds.reduce((a, k) => a + (k.soLuongVangMat ?? 0), 0);
        const pctT = tong ? (daTrao / tong) * 100 : 0;

        const R = 60;
        const C = 2 * Math.PI * R;

        return {
            tong,
            daTrao,
            conLai: tong - daTrao,
            vangMat,
            pctT,
            soKhoa: ds.length,
            soKhoaXong: ds.filter((k) => k.trangThai === TrangThaiKhoa.DaTraoBang).length,
            R,
            C,
            off: C * (1 - Math.min(100, pctT) / 100),
            gAng: (GOAL / 100) * 360
        };
    });

    /** khoa đang trao bằng, dùng để hiện dòng tiến trình ngay dưới vòng tỷ lệ */
    khoaDangTrao = computed(() => this.dsKhoa().find((k) => k.trangThai === TrangThaiKhoa.DangTraoBang) ?? null);

    bang = computed<IDongThongKe[]>(() => {
        const rows = this.dsKhoa()
            .filter((k) => this.hit(`${k.ten ?? ''} ${k.truongKhoa ?? ''}`))
            .map((khoa) => {
                const tong = khoa.soLuongThamGia ?? 0;
                const daTrao = khoa.soLuongDaTrao ?? 0;
                return {
                    khoa,
                    order: khoa.order ?? 0,
                    ten: khoa.ten ?? '',
                    tong,
                    daTrao,
                    conLai: khoa.soLuongConLai ?? tong - daTrao,
                    vangMat: khoa.soLuongVangMat ?? 0,
                    pct: tong ? (daTrao / tong) * 100 : 0,
                    cls: this.clsTrangThai(khoa.trangThai),
                    nhanTrangThai: this.nhanTrangThai(khoa.trangThai)
                };
            });

        return this.sapXep(rows);
    });

    ngOnInit(): void {
        this.getThongKe();
        this.connectHub();
    }

    ngOnDestroy(): void {
        this.hubConnection?.stop().then();
    }

    getThongKe() {
        this.loi.set(null);

        // chỉ cần số liệu theo khoa, không kèm danh sách sinh viên
        this._thongKeService.getAllKhoa().subscribe({
            next: (res) => {
                this.dangTai.set(false);
                if (res.status === 1) {
                    this.dsKhoa.set(res.data ?? []);
                } else {
                    this.loi.set(res.message || 'Không lấy được số liệu thống kê.');
                }
            },
            error: () => {
                this.dangTai.set(false);
                this.loi.set('Không kết nối được máy chủ. Vui lòng thử lại.');
            }
        });
    }

    /**
     * Hub chỉ bắn tín hiệu rỗng báo có thay đổi, không mang dữ liệu.
     * Nhận được thì gọi lại API để lấy số liệu mới nhất.
     */
    connectHub() {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(TraoBangHubConst.HUB, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on(TraoBangHubConst.ReceiveSinhVienDangTrao, () => this.getThongKe());
        this.hubConnection.on(TraoBangHubConst.ReceiveCheckIn, () => this.getThongKe());
        this.hubConnection.on(TraoBangHubConst.ReceiveChonKhoa, () => this.getThongKe());
        this.hubConnection.onreconnected(() => this.getThongKe());

        this.hubConnection.start().then();
    }

    // ---------- định dạng ----------

    nf = (n: number) => n.toLocaleString('vi-VN');
    p1 = (x: number) => x.toFixed(1).replace('.', ',');

    /** đạt ngưỡng thì xanh lá, quá nửa thì cyan, còn lại thì cam */
    clsTyLe = (p: number) => (p >= GOAL ? 'g' : p >= 50 ? 'c' : 'w');

    // ---------- tương tác ----------

    doiSort(k: SortKey) {
        if (this.sortKey() === k) {
            this.sortDir.update((d) => -d);
        } else {
            this.sortKey.set(k);
            // tên khoa và thứ tự thì xuôi, các cột số thì ngược
            this.sortDir.set(k === 'ten' || k === 'order' ? 1 : -1);
        }
    }

    /** mũi tên chỉ chiều sắp xếp trên tiêu đề cột */
    muiTen(k: SortKey): string {
        if (this.sortKey() !== k) {
            return '↕';
        }
        return this.sortDir() < 0 ? '↓' : '↑';
    }

    timKiem(v: string) {
        this.query.set(v);
    }

    private sapXep(arr: IDongThongKe[]): IDongThongKe[] {
        const k = this.sortKey();
        const dir = this.sortDir();
        return arr.sort((a, b) => {
            if (k === 'ten') {
                return dir * a.ten.localeCompare(b.ten, 'vi');
            }
            return dir * (a[k] - b[k]);
        });
    }

    private nhanTrangThai(tt?: number): string {
        if (tt === TrangThaiKhoa.DaTraoBang) {
            return 'Đã trao xong';
        }
        if (tt === TrangThaiKhoa.DangTraoBang) {
            return 'Đang trao bằng';
        }
        return 'Chờ tới lượt';
    }

    private clsTrangThai(tt?: number): string {
        if (tt === TrangThaiKhoa.DaTraoBang) {
            return 'g';
        }
        if (tt === TrangThaiKhoa.DangTraoBang) {
            return 'c';
        }
        return 'w';
    }

    /** bỏ dấu tiếng Việt để tìm không phân biệt dấu */
    private hit(t: string): boolean {
        const strip = (s: string) => s.normalize('NFD').replace(/\p{M}/gu, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase();
        return !this.query() || strip(t).includes(strip(this.query()));
    }
}
