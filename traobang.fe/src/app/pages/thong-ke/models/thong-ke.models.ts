/**
 * Model dùng chung cho nhóm màn thống kê trao bằng.
 * Toàn bộ dữ liệu hiện đang là dữ liệu tĩnh trong thư mục `data`, chưa gọi API.
 */

/** Một bộ phận đón tiếp trong sơ đồ */
export interface IBoPhan {
    id: string;
    name: string;
    room: string;
    color: string;
    desc: string;
    /** số máy tính */
    pc: number;
    /** số máy quét */
    qr: number;
    /** số máy POS */
    pos?: number;
    /** số camera */
    cam?: number;
    who: string;
    /** cảnh báo cần xác nhận thêm về thiết bị */
    flag?: string;
}

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
