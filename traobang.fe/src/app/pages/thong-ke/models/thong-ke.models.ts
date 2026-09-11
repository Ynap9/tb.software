/**
 * Hằng số trạng thái dùng chung cho nhóm màn thống kê trao bằng.
 * Giá trị khớp với TrangThaiSubPlan / TraoBangConstants bên backend.
 */

/** Trạng thái của một khoa trong buổi lễ, khớp với TrangThaiSubPlan bên backend */
export const TrangThaiKhoa = {
    ChuanBi: 2,
    DangTraoBang: 3,
    DaTraoBang: 4
} as const;

/** Trạng thái của một sinh viên, khớp với TraoBangConstants bên backend */
export const TrangThaiSinhVien = {
    XepHang: 1,
    ChuanBi: 2,
    DangTraoBang: 3,
    DaTraoBang: 4,
    /** có tên trong danh sách nhận bằng */
    ThamGiaTraoBang: 5,
    VangMat: 6
} as const;
