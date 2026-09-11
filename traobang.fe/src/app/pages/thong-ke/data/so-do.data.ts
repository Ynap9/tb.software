/**
 * Dữ liệu sơ đồ lễ trao bằng — HUCE.
 * Vẽ lại theo hai bản in của Nhà trường: sơ đồ vị trí chỗ ngồi (tầng 2, tầng 3 nhà G3)
 * và hướng dẫn các bước lên nhận bằng. Dữ liệu tĩnh, chưa gọi API.
 */

/** Một khu vực ghế trong hội trường tầng 2 */
export interface IKhuVucNgoi {
    id: string;
    ten: string;
    /** chỉ số hàng ghế đầu và cuối trong HANG_GHE */
    tu: number;
    den: number;
    /** màu nền của băng khu vực */
    mau: string;
    /** màu ghế trong khu vực */
    mauGhe: string;
    /** ghế đại biểu vẽ liền thành dải, không tách từng ghế như ghế sinh viên */
    laDaiBieu?: boolean;
}

/** Nhãn các hàng ghế, đọc từ trên sân khấu xuống cuối hội trường */
export const HANG_GHE = ['A', "A'", 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W'];

/** Số ghế của ba dãy trong một hàng: dãy trái, dãy giữa, dãy phải */
export const DAY_GHE_TANG_2 = [5, 13, 5];

/** Các khu vực chỗ ngồi ở tầng 2 nhà G3 */
export const KHU_VUC_NGOI: IKhuVucNgoi[] = [
    { id: 'DAI_BIEU', ten: 'GHẾ ĐẠI BIỂU', tu: 0, den: 1, mau: '#F3D2CE', mauGhe: '#C62828', laDaiBieu: true },
    { id: 'SAU_DH', ten: 'TÂN TIẾN SĨ, THẠC SĨ', tu: 2, den: 3, mau: '#F5A38B', mauGhe: '#4CAF50' },
    { id: 'KINH_TE', ten: 'KHOA KINH TẾ VÀ QUẢN LÝ XÂY DỰNG', tu: 4, den: 11, mau: '#F7E489', mauGhe: '#4CAF50' },
    { id: 'CNTT', ten: 'KHOA CÔNG NGHỆ THÔNG TIN', tu: 12, den: 15, mau: '#C9A6DE', mauGhe: '#4CAF50' },
    { id: 'KIEN_TRUC', ten: 'KHOA KIẾN TRÚC VÀ QUY HOẠCH', tu: 16, den: 23, mau: '#F0A088', mauGhe: '#4CAF50' }
];

/** Số hàng ghế và số ghế mỗi dãy của khu vực phụ huynh ở tầng 3 */
export const TANG_3 = {
    ten: 'PHỤ HUYNH, NGƯỜI THÂN',
    mau: '#4FC3F7',
    mauGhe: '#4CAF50',
    soHang: 7,
    day: [6, 13, 6]
};

/** Phòng chờ của từng Khoa trước khi vào hội trường */
export interface IPhongCho {
    khoa: string;
    phong: string;
}

export const PHONG_CHO: IPhongCho[] = [
    { khoa: 'Khoa Xây dựng Dân dụng và Công nghiệp', phong: 'P.25, 26-H2' },
    { khoa: 'Khoa Vật liệu Xây dựng', phong: 'P.14-H2' },
    { khoa: 'Ban Đào tạo kỹ sư chất lượng cao', phong: 'P.14-H2' },
    { khoa: 'Khoa Xây dựng Công trình thuỷ', phong: 'P.14-H2' },
    { khoa: 'Khoa Cơ khí', phong: 'P.14-H2' },
    { khoa: 'Khoa Cầu đường', phong: 'P.26-H2' },
    { khoa: 'Khoa Kỹ thuật Môi trường (CQ + VLVH)', phong: 'P.24-H2' }
];

/** Một bước trong hướng dẫn lên nhận bằng */
export interface IBuocNhanBang {
    so: number;
    /** các dòng chữ trong khung chú thích, xuống dòng theo đúng bản in */
    dong: string[];
    /** các chặng đường đi tương ứng với bước này */
    seg: string[];
    /** khung chú thích */
    box: { x: number; y: number; w: number; h: number };
    /** căn chữ trong khung */
    canGiua?: boolean;
}

/**
 * Bốn bước lên nhận bằng. Toạ độ vẽ trong hệ 1400 x 900 của sơ đồ hội trường:
 * sân khấu ở trên, hàng ghế ở dưới, sinh viên đi từ phải sang trái.
 */
export const BUOC_NHAN_BANG: IBuocNhanBang[] = [
    {
        so: 1,
        dong: ['Sinh viên xếp hàng', 'quét mã QR'],
        seg: ['M 1080 838 L 1080 646'],
        box: { x: 1092, y: 664, w: 292, h: 84 }
    },
    {
        so: 2,
        dong: ['Sinh viên nghe gọi tên,', 'bước lên sân khấu,', 'Lãnh đạo Khoa chúc mừng'],
        seg: ['M 1080 540 L 950 540 L 950 262'],
        box: { x: 1092, y: 404, w: 292, h: 116 }
    },
    {
        so: 3,
        dong: ['Sinh viên nhận bằng', 'từ thầy Hiệu trưởng,', 'chụp ảnh'],
        seg: ['M 950 262 L 328 262'],
        box: { x: 548, y: 74, w: 330, h: 122 },
        canGiua: true
    },
    {
        so: 4,
        dong: ['Đi xuống hội trường', '(kết thúc)'],
        seg: ['M 328 262 L 328 838'],
        box: { x: 72, y: 352, w: 292, h: 104 },
        canGiua: true
    }
];
