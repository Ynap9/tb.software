import { IThongKeChiTietKhoa, IThongKeSinhVienChiTiet } from '@/models/traobang/thong-ke.models';
import { ThongKeService } from '@/service/thong-ke.service';
import { TraoBangHubConst } from '@/shared/constants/sv-nhan-bang.constants';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import * as signalR from '@microsoft/signalr';
import { TrangThaiKhoa, TrangThaiSinhVien } from '../models/thong-ke.models';

/** Một dòng sinh viên kèm nhãn trạng thái đã tính sẵn */
interface IDongSinhVien {
    sv: IThongKeSinhVienChiTiet;
    nhan: string;
    /** g = đã lên, c = đang lên, i = đã check-in, w = vắng mặt, rỗng = chưa check-in */
    cls: string;
}

@Component({
    selector: 'app-chi-tiet-khoa',
    imports: [RouterLink],
    templateUrl: './chi-tiet-khoa.html',
    styleUrl: './chi-tiet-khoa.scss'
})
export class ChiTietKhoa implements OnInit, OnDestroy {
    _thongKeService = inject(ThongKeService);
    _route = inject(ActivatedRoute);

    hubConnection: signalR.HubConnection | undefined;

    idKhoa = 0;

    khoa = signal<IThongKeChiTietKhoa | null>(null);
    dangTai = signal(true);

    /** thông báo lỗi khi gọi API thất bại, null nghĩa là không có lỗi */
    loi = signal<string | null>(null);

    /** từ khoá tìm theo mã sinh viên, họ tên, lớp, ngành */
    query = signal('');

    /** lọc theo trạng thái, 0 nghĩa là tất cả */
    trangThaiLoc = signal(0);

    boLocTrangThai = [
        { v: 0, l: 'Tất cả trạng thái' },
        { v: TrangThaiSinhVien.DaTraoBang, l: 'Đã lên nhận bằng' },
        { v: TrangThaiSinhVien.DangTraoBang, l: 'Đang nhận bằng' },
        { v: TrangThaiSinhVien.ChuanBi, l: 'Đã check-in, đang chờ' },
        { v: TrangThaiSinhVien.XepHang, l: 'Chưa check-in' },
        { v: TrangThaiSinhVien.VangMat, l: 'Vắng mặt' }
    ];

    cards = computed(() => {
        const k = this.khoa();
        return [
            { v: k?.soLuongThamGia ?? 0, l: 'Sinh viên tham gia' },
            { v: k?.soLuongDaTrao ?? 0, l: 'Đã lên nhận bằng' },
            { v: k?.soLuongDaCheckIn ?? 0, l: 'Đã check-in, đang chờ' },
            { v: k?.soLuongChuaCheckIn ?? 0, l: 'Chưa check-in' },
            { v: k?.soLuongVangMat ?? 0, l: 'Vắng mặt' }
        ];
    });

    /** tỷ lệ đã lên nhận bằng của khoa (%) */
    pct = computed(() => {
        const k = this.khoa();
        const tong = k?.soLuongThamGia ?? 0;
        return tong ? ((k?.soLuongDaTrao ?? 0) / tong) * 100 : 0;
    });

    /** g = đã trao xong, c = đang trao bằng, w = chờ tới lượt */
    clsKhoa = computed(() => this.clsTrangThaiKhoa(this.khoa()?.trangThai));

    nhanTrangThaiKhoa = computed(() => {
        const tt = this.khoa()?.trangThai;
        if (tt === TrangThaiKhoa.DaTraoBang) {
            return 'Đã trao xong';
        }
        if (tt === TrangThaiKhoa.DangTraoBang) {
            return 'Đang trao bằng';
        }
        return 'Chờ tới lượt';
    });

    rows = computed<IDongSinhVien[]>(() =>
        (this.khoa()?.sinhViens ?? [])
            .filter((sv) => {
                if (this.trangThaiLoc() && sv.trangThai !== this.trangThaiLoc()) {
                    return false;
                }
                return this.hit(`${sv.maSoSinhVien ?? ''} ${sv.hoVaTen ?? ''} ${sv.lop ?? ''} ${sv.tenNganhDaoTao ?? ''}`);
            })
            .map((sv) => ({ sv, nhan: this.nhanTrangThai(sv.trangThai), cls: this.clsTrangThai(sv.trangThai) }))
    );

    ngOnInit(): void {
        this.idKhoa = Number(this._route.snapshot.paramMap.get('id'));
        this.getChiTiet();
        this.connectHub();
    }

    ngOnDestroy(): void {
        this.hubConnection?.stop().then();
    }

    getChiTiet() {
        this.loi.set(null);

        this._thongKeService.getChiTietKhoa(this.idKhoa).subscribe({
            next: (res) => {
                this.dangTai.set(false);
                if (res.status === 1) {
                    this.khoa.set(res.data ?? null);
                } else {
                    this.loi.set(res.message || 'Không lấy được danh sách sinh viên của khoa.');
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
     * Nhận được thì gọi lại API để lấy trạng thái mới nhất.
     */
    connectHub() {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(TraoBangHubConst.HUB, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on(TraoBangHubConst.ReceiveSinhVienDangTrao, () => this.getChiTiet());
        this.hubConnection.on(TraoBangHubConst.ReceiveCheckIn, () => this.getChiTiet());
        this.hubConnection.on(TraoBangHubConst.ReceiveChonKhoa, () => this.getChiTiet());
        this.hubConnection.onreconnected(() => this.getChiTiet());

        this.hubConnection.start().then();
    }

    nf = (n: number) => n.toLocaleString('vi-VN');
    p1 = (x: number) => x.toFixed(1).replace('.', ',');

    timKiem(v: string) {
        this.query.set(v);
    }

    chonTrangThai(v: string) {
        this.trangThaiLoc.set(Number(v));
    }

    private clsTrangThaiKhoa(tt?: number): string {
        if (tt === TrangThaiKhoa.DaTraoBang) {
            return 'g';
        }
        if (tt === TrangThaiKhoa.DangTraoBang) {
            return 'c';
        }
        return 'w';
    }

    private nhanTrangThai(tt?: number): string {
        switch (tt) {
            case TrangThaiSinhVien.DaTraoBang:
                return 'Đã lên nhận bằng';
            case TrangThaiSinhVien.DangTraoBang:
                return 'Đang nhận bằng';
            case TrangThaiSinhVien.ChuanBi:
                return 'Đã check-in';
            case TrangThaiSinhVien.VangMat:
                return 'Vắng mặt';
            default:
                return 'Chưa check-in';
        }
    }

    private clsTrangThai(tt?: number): string {
        switch (tt) {
            case TrangThaiSinhVien.DaTraoBang:
                return 'g';
            case TrangThaiSinhVien.DangTraoBang:
                return 'c';
            case TrangThaiSinhVien.ChuanBi:
                return 'i';
            case TrangThaiSinhVien.VangMat:
                return 'w';
            default:
                return '';
        }
    }

    /** bỏ dấu tiếng Việt để tìm không phân biệt dấu */
    private hit(t: string): boolean {
        const strip = (s: string) => s.normalize('NFD').replace(/\p{M}/gu, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase();
        return !this.query() || strip(t).includes(strip(this.query()));
    }
}
