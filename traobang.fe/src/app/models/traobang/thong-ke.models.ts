/** Sinh viên nhận bằng thuộc một khoa — trả về từ api/core/thong-ke/khoa */
export interface IThongKeSinhVien {
    id?: number;
    hoVaTen?: string | null;
    email?: string | null;
    lop?: string | null;
    /** theo TraoBangConstants: 5 tham gia, 6 vắng mặt */
    trangThai?: number;
}

/** Một khoa của plan đang active, kèm danh sách sinh viên nhận bằng */
export interface IThongKeKhoa {
    id?: number;
    ten?: string;
    truongKhoa?: string;
    order?: number;
    /** theo TrangThaiSubPlan: 2 chuẩn bị, 3 đang trao bằng, 4 đã trao xong */
    trangThai?: number;
    isShow?: boolean;
    /** tổng số sinh viên trong danh sách, gồm cả vắng mặt */
    soLuongSinhVien?: number;
    soLuongThamGia?: number;
    soLuongVangMat?: number;
    soLuongDaTrao?: number;
    soLuongConLai?: number;
    sinhViens?: IThongKeSinhVien[];
}

/** Một sinh viên trong hàng chờ trao bằng */
export interface IThongKeSinhVienHangDoi {
    /** thứ tự lên nhận bằng tính trên toàn buổi lễ */
    stt?: number;
    /** id bản ghi tiến độ trao bằng */
    id?: number;
    idSinhVienNhanBang?: number;
    idKhoa?: number;
    tenKhoa?: string;
    orderKhoa?: number;
    /** thứ tự trong hàng chờ của riêng khoa */
    order?: number;
    hoVaTen?: string;
    maSoSinhVien?: string;
    lop?: string;
    tenNganhDaoTao?: string;
    capBang?: string;
    xepHang?: string;
    note?: string | null;
    /** 2 chuẩn bị, 3 đang trao bằng, 4 đã trao bằng */
    trangThai?: number;
}

/** Hàng chờ trao bằng của plan đang active */
export interface IThongKeHangDoi {
    tongSinhVien?: number;
    soLuongDaTrao?: number;
    soLuongDangTrao?: number;
    soLuongChuanBi?: number;
    items?: IThongKeSinhVienHangDoi[];
}

/** Một sinh viên trong danh sách nhận bằng của một khoa */
export interface IThongKeSinhVienChiTiet {
    /** số thứ tự trong danh sách của khoa */
    stt?: number;
    id?: number;
    idSlide?: number;
    maSoSinhVien?: string;
    hoVaTen?: string;
    lop?: string;
    tenNganhDaoTao?: string;
    capBang?: string;
    xepHang?: string;
    thanhTich?: string;
    note?: string | null;
    /** thứ tự slide trong khoa */
    order?: number;
    /** thứ tự trong hàng đợi, null nghĩa là chưa check-in */
    orderHangDoi?: number | null;
    daLenNhanBang?: boolean;
    /** 1 chưa check-in, 2 đã check-in, 3 đang nhận bằng, 4 đã nhận bằng, 6 vắng mặt */
    trangThai?: number;
}

/** Chi tiết một khoa kèm toàn bộ sinh viên trong danh sách nhận bằng */
export interface IThongKeChiTietKhoa {
    id?: number;
    ten?: string;
    truongKhoa?: string;
    order?: number;
    trangThai?: number;
    soLuongSinhVien?: number;
    soLuongThamGia?: number;
    soLuongVangMat?: number;
    soLuongDaTrao?: number;
    soLuongDangTrao?: number;
    soLuongDaCheckIn?: number;
    soLuongChuaCheckIn?: number;
    sinhViens?: IThongKeSinhVienChiTiet[];
}
