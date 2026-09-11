import { IThongKeSinhVienHangDoi } from '@/models/traobang/thong-ke.models';
import { ThongKeService } from '@/service/thong-ke.service';
import { TraoBangHubConst } from '@/shared/constants/sv-nhan-bang.constants';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { TrangThaiSinhVien } from '../models/thong-ke.models';

/** Một dòng trong hàng chờ kèm nhãn trạng thái đã tính sẵn */
interface IDongHangDoi {
    sv: IThongKeSinhVienHangDoi;
    nhan: string;
    /** g = đã trao, c = đang trao, rỗng = còn chờ */
    cls: string;
}

@Component({
    selector: 'app-danh-sach-sinh-vien',
    imports: [],
    templateUrl: './danh-sach-sinh-vien.html',
    styleUrl: './danh-sach-sinh-vien.scss'
})
export class DanhSachSinhVien implements OnInit, OnDestroy {
    _thongKeService = inject(ThongKeService);

    hubConnection: signalR.HubConnection | undefined;

    /** toàn bộ hàng chờ lấy từ API, đã xếp theo thứ tự lên nhận bằng */
    hangDoi = signal<IThongKeSinhVienHangDoi[]>([]);

    tongSinhVien = signal(0);
    soLuongDaTrao = signal(0);
    soLuongDangTrao = signal(0);
    soLuongChuanBi = signal(0);

    dangTai = signal(true);

    /** thông báo lỗi khi gọi API thất bại, null nghĩa là không có lỗi */
    loi = signal<string | null>(null);

    /** từ khoá tìm theo mã sinh viên, họ tên, lớp, ngành */
    query = signal('');

    /** lọc theo khoa, rỗng nghĩa là tất cả */
    khoaLoc = signal('');

    /** lọc theo trạng thái, 0 nghĩa là tất cả */
    trangThaiLoc = signal(0);

    boLocTrangThai = [
        { v: 0, l: 'Tất cả trạng thái' },
        { v: TrangThaiSinhVien.DaTraoBang, l: 'Đã nhận bằng' },
        { v: TrangThaiSinhVien.DangTraoBang, l: 'Đang nhận bằng' },
        { v: TrangThaiSinhVien.ChuanBi, l: 'Đang chờ' }
    ];

    /** các khoa đang có mặt trong hàng chờ, dựng từ chính dữ liệu trả về */
    dsKhoa = computed(() => {
        const map = new Map<string, string>();
        this.hangDoi().forEach((sv) => {
            if (sv.tenKhoa) {
                map.set(sv.tenKhoa, sv.tenKhoa);
            }
        });
        return [...map.values()];
    });

    rows = computed<IDongHangDoi[]>(() =>
        this.hangDoi()
            .filter((sv) => {
                if (this.khoaLoc() && sv.tenKhoa !== this.khoaLoc()) {
                    return false;
                }
                if (this.trangThaiLoc() && sv.trangThai !== this.trangThaiLoc()) {
                    return false;
                }
                return this.hit(`${sv.maSoSinhVien ?? ''} ${sv.hoVaTen ?? ''} ${sv.lop ?? ''} ${sv.tenKhoa ?? ''} ${sv.tenNganhDaoTao ?? ''}`);
            })
            .map((sv) => ({ sv, nhan: this.nhanTrangThai(sv.trangThai), cls: this.clsTrangThai(sv.trangThai) }))
    );

    cards = computed(() => [
        { v: this.tongSinhVien(), l: 'Trong hàng chờ' },
        { v: this.soLuongDaTrao(), l: 'Đã nhận bằng' },
        { v: this.soLuongDangTrao(), l: 'Đang nhận bằng' },
        { v: this.soLuongChuanBi(), l: 'Đang chờ' }
    ]);

    ngOnInit(): void {
        this.getHangDoi();
        this.connectHub();
    }

    ngOnDestroy(): void {
        this.hubConnection?.stop().then();
    }

    getHangDoi() {
        this.loi.set(null);

        this._thongKeService.getHangDoiSinhVien().subscribe({
            next: (res) => {
                this.dangTai.set(false);
                if (res.status === 1) {
                    const data = res.data ?? {};
                    this.hangDoi.set(data.items ?? []);
                    this.tongSinhVien.set(data.tongSinhVien ?? 0);
                    this.soLuongDaTrao.set(data.soLuongDaTrao ?? 0);
                    this.soLuongDangTrao.set(data.soLuongDangTrao ?? 0);
                    this.soLuongChuanBi.set(data.soLuongChuanBi ?? 0);
                } else {
                    this.loi.set(res.message || 'Không lấy được hàng chờ trao bằng.');
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
     * Nhận được thì gọi lại API để lấy hàng chờ mới nhất.
     */
    connectHub() {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(TraoBangHubConst.HUB, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on(TraoBangHubConst.ReceiveSinhVienDangTrao, () => this.getHangDoi());
        this.hubConnection.on(TraoBangHubConst.ReceiveCheckIn, () => this.getHangDoi());
        this.hubConnection.on(TraoBangHubConst.ReceiveChonKhoa, () => this.getHangDoi());
        this.hubConnection.onreconnected(() => this.getHangDoi());

        this.hubConnection.start().then();
    }

    nf = (n: number) => n.toLocaleString('vi-VN');

    timKiem(v: string) {
        this.query.set(v);
    }

    chonKhoa(v: string) {
        this.khoaLoc.set(v);
    }

    chonTrangThai(v: string) {
        this.trangThaiLoc.set(Number(v));
    }

    private nhanTrangThai(tt?: number): string {
        switch (tt) {
            case TrangThaiSinhVien.DaTraoBang:
                return 'Đã nhận bằng';
            case TrangThaiSinhVien.DangTraoBang:
                return 'Đang nhận bằng';
            default:
                return 'Đang chờ';
        }
    }

    private clsTrangThai(tt?: number): string {
        switch (tt) {
            case TrangThaiSinhVien.DaTraoBang:
                return 'g';
            case TrangThaiSinhVien.DangTraoBang:
                return 'c';
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
