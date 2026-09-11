import { IThongKeKhoa } from '@/models/traobang/thong-ke.models';
import { ThongKeService } from '@/service/thong-ke.service';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TrangThaiKhoa, TrangThaiSinhVien } from '../models/thong-ke.models';

/** Một sinh viên rút gọn để hiện trong thẻ khoa */
interface ISinhVienTom {
    id?: number;
    hoVaTen: string;
    lop: string;
    vangMat: boolean;
}

/** Một thẻ khoa kèm các số liệu đã tính sẵn cho template */
interface ITheKhoa {
    khoa: IThongKeKhoa;
    /** màu nhận diện, BE không trả nên gán theo thứ tự khoa */
    color: string;
    /** tỷ lệ đã trao bằng của khoa (%) */
    pct: number;
    /** g = xong, c = đang trao, w = chưa tới lượt */
    cls: string;
    nhanTrangThai: string;
    /** vài sinh viên đầu danh sách để xem nhanh */
    sinhViens: ISinhVienTom[];
    /** số sinh viên còn lại không hiện trong thẻ */
    soSinhVienConLai: number;
}

/** Màu nhận diện của khoa trên các màn hình, lặp lại khi hết màu */
const MAU_KHOA = ['#B49BD8', '#B02525', '#3E9E52', '#9FA8DA', '#F0A268', '#8B9BE2', '#7CC79B', '#C3DE9C', '#FBE28F', '#7CBFD2', '#E4A76B', '#F2681F'];

/** Số sinh viên hiện trong mỗi thẻ khoa, phần còn lại chỉ đếm */
const SO_SV_HIEN_THI = 5;

@Component({
    selector: 'app-danh-sach-khoa',
    imports: [RouterLink],
    templateUrl: './danh-sach-khoa.html',
    styleUrl: './danh-sach-khoa.scss'
})
export class DanhSachKhoa implements OnInit {
    _thongKeService = inject(ThongKeService);

    /** danh sách khoa lấy từ API, đã sắp theo thứ tự lên nhận bằng */
    dsKhoa = signal<IThongKeKhoa[]>([]);

    dangTai = signal(true);

    /** thông báo lỗi khi gọi API thất bại, null nghĩa là không có lỗi */
    loi = signal<string | null>(null);

    /** từ khoá tìm theo tên khoa hoặc tên trưởng khoa */
    query = signal('');

    the = computed<ITheKhoa[]>(() =>
        this.dsKhoa()
            .map((khoa, i) => {
                const thamGia = khoa.soLuongThamGia ?? 0;
                const daTrao = khoa.soLuongDaTrao ?? 0;

                // BE đã cắt sẵn danh sách, cắt thêm ở đây để phòng khi gọi không truyền giới hạn
                const hienThi = (khoa.sinhViens ?? []).slice(0, SO_SV_HIEN_THI);

                return {
                    khoa,
                    color: MAU_KHOA[i % MAU_KHOA.length],
                    pct: thamGia ? (daTrao / thamGia) * 100 : 0,
                    cls: khoa.trangThai === TrangThaiKhoa.DaTraoBang ? 'g' : khoa.trangThai === TrangThaiKhoa.DangTraoBang ? 'c' : 'w',
                    nhanTrangThai: this.nhanTrangThai(khoa.trangThai),
                    sinhViens: hienThi.map((sv) => ({
                        id: sv.id,
                        hoVaTen: sv.hoVaTen ?? '',
                        lop: sv.lop ?? '',
                        vangMat: sv.trangThai === TrangThaiSinhVien.VangMat
                    })),
                    // đếm theo tổng của khoa chứ không theo mảng đã bị cắt
                    soSinhVienConLai: Math.max(0, (khoa.soLuongSinhVien ?? 0) - hienThi.length)
                };
            })
            .filter((t) => this.hit(`${t.khoa.ten ?? ''} ${t.khoa.truongKhoa ?? ''}`))
    );

    tongKhoa = computed(() => this.dsKhoa().length);
    tongSv = computed(() => this.dsKhoa().reduce((a, k) => a + (k.soLuongThamGia ?? 0), 0));
    tongDaTrao = computed(() => this.dsKhoa().reduce((a, k) => a + (k.soLuongDaTrao ?? 0), 0));
    tongVangMat = computed(() => this.dsKhoa().reduce((a, k) => a + (k.soLuongVangMat ?? 0), 0));

    /** ô số liệu tổng quan ở đầu màn */
    cards = computed(() => [
        { v: this.tongKhoa(), l: 'Khoa tham gia' },
        { v: this.tongSv(), l: 'Sinh viên nhận bằng' },
        { v: this.tongDaTrao(), l: 'Đã trao bằng' },
        { v: this.tongSv() - this.tongDaTrao(), l: 'Còn lại' },
        { v: this.tongVangMat(), l: 'Vắng mặt' }
    ]);

    ngOnInit(): void {
        this.getDanhSachKhoa();
    }

    getDanhSachKhoa() {
        this.dangTai.set(true);
        this.loi.set(null);

        // chỉ xin đúng số sinh viên hiện trên thẻ, các số đếm BE vẫn tính trên toàn bộ khoa
        this._thongKeService.getAllKhoa(SO_SV_HIEN_THI).subscribe({
            next: (res) => {
                this.dangTai.set(false);
                if (res.status === 1) {
                    // BE đã sắp theo Order, sắp lại ở đây để chắc chắn đúng thứ tự các khoa
                    this.dsKhoa.set([...(res.data ?? [])].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)));
                } else {
                    this.loi.set(res.message || 'Không lấy được danh sách khoa.');
                }
            },
            error: () => {
                this.dangTai.set(false);
                this.loi.set('Không kết nối được máy chủ. Vui lòng thử lại.');
            }
        });
    }

    timKiem(v: string) {
        this.query.set(v);
    }

    p1 = (x: number) => x.toFixed(1).replace('.', ',');
    nf = (n: number) => n.toLocaleString('vi-VN');

    private nhanTrangThai(tt?: number): string {
        if (tt === TrangThaiKhoa.DaTraoBang) {
            return 'Đã trao xong';
        }
        if (tt === TrangThaiKhoa.DangTraoBang) {
            return 'Đang trao bằng';
        }
        return 'Chờ tới lượt';
    }

    /** bỏ dấu tiếng Việt để tìm không phân biệt dấu */
    private hit(t: string): boolean {
        const strip = (s: string) => s.normalize('NFD').replace(/\p{M}/gu, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase();
        return !this.query() || strip(t).includes(strip(this.query()));
    }
}
