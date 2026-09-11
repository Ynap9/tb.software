/**
 * Dữ liệu sơ đồ lễ trao bằng — HUCE.
 * Gộp hai bản in của Nhà trường vào chung một mặt bằng tầng 2 nhà G3:
 * vị trí chỗ ngồi của từng khu vực và luồng bốn bước sinh viên lên nhận bằng.
 * Dữ liệu tĩnh, chưa gọi API.
 */

/** Một khu vực ghế trong hội trường */
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
}

/** Nhãn các hàng ghế, đọc từ sân khấu xuống cuối hội trường */
export const HANG_GHE = ['A', "A'", 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O'];

/** Số ghế của ba dãy trong một hàng: dãy trái, dãy giữa, dãy phải */
export const DAY_GHE = [5, 13, 5];

/** Các khu vực chỗ ngồi trong hội trường */
export const KHU_VUC_NGOI: IKhuVucNgoi[] = [
    { id: 'DAI_BIEU', ten: 'GHẾ ĐẠI BIỂU', tu: 0, den: 1, mau: '#F3D2CE', mauGhe: '#C62828' },
    { id: 'SAU_DH', ten: 'TÂN TIẾN SĨ, THẠC SĨ', tu: 2, den: 3, mau: '#F5A38B', mauGhe: '#4CAF50' },
    { id: 'KINH_TE', ten: 'KHOA KINH TẾ VÀ QUẢN LÝ XÂY DỰNG', tu: 4, den: 7, mau: '#F7E489', mauGhe: '#4CAF50' },
    { id: 'CNTT', ten: 'KHOA CÔNG NGHỆ THÔNG TIN', tu: 8, den: 11, mau: '#C9A6DE', mauGhe: '#4CAF50' },
    { id: 'KIEN_TRUC', ten: 'KHOA KIẾN TRÚC VÀ QUY HOẠCH', tu: 12, den: 15, mau: '#F0A088', mauGhe: '#4CAF50' }
];

/** Vị trí ngồi hoặc phòng chờ của một khoa */
export interface IViTriKhoa {
    /** từ khoá để nhận ra khoa trong tên lấy từ DB, viết thường, không dấu */
    tuKhoa: string[];
    viTri: string;
}

/**
 * Vị trí của từng khoa trong buổi lễ. Tên khoa lấy từ DB nên không so khớp nguyên văn
 * mà dò theo từ khoá không dấu — tránh lệch vì "&" và "và", "thủy" và "thuỷ".
 * Khoa nào không khớp từ khoá nào sẽ hiện là chưa có vị trí.
 */
export const VI_TRI_KHOA: IViTriKhoa[] = [
    // ngồi trong hội trường G3
    { tuKhoa: ['tien si', 'thac si', 'sau dai hoc'], viTri: 'Hội trường G3' },
    { tuKhoa: ['kinh te'], viTri: 'Hội trường G3' },
    { tuKhoa: ['cong nghe thong tin'], viTri: 'Hội trường G3' },
    { tuKhoa: ['kien truc'], viTri: 'Hội trường G3' },
    // chờ ở các phòng nhà H2
    { tuKhoa: ['dan dung'], viTri: 'P.25, P.26-H2' },
    { tuKhoa: ['vat lieu'], viTri: 'P.14-H2' },
    { tuKhoa: ['chat luong cao'], viTri: 'P.14-H2' },
    { tuKhoa: ['cong trinh thuy'], viTri: 'P.14-H2' },
    { tuKhoa: ['co khi'], viTri: 'P.14-H2' },
    { tuKhoa: ['cau duong'], viTri: 'P.26-H2' },
    { tuKhoa: ['moi truong'], viTri: 'P.24-H2' }
];

/** Một bước trong luồng lên nhận bằng */
export interface IBuocNhanBang {
    so: number;
    /** nhãn ngắn hiện cạnh huy hiệu trên sơ đồ */
    ten: string;
    /** mô tả đầy đủ theo bản hướng dẫn của Nhà trường */
    moTa: string;
    /** các chặng đường đi của bước này */
    seg: string[];
    /** vị trí huy hiệu số bước trên tuyến */
    badge: { x: number; y: number };
    /** vị trí nhãn ngắn và cách neo chữ so với huy hiệu */
    nhan: { x: number; y: number; neo: 'start' | 'middle' | 'end' };
}

/**
 * Bốn bước lên nhận bằng, toạ độ trong hệ 1120 x 1120 của mặt bằng hội trường.
 * Sinh viên lên bằng lối bên phải, lách qua bàn trao bằng rồi đi vòng phía sau
 * bục nhận bằng, cuối cùng xuống bằng lối bên trái.
 */
export const BUOC_NHAN_BANG: IBuocNhanBang[] = [
    {
        so: 1,
        ten: 'Xếp hàng quét mã QR',
        moTa: 'Sinh viên xếp hàng quét mã QR',
        // từ khe giữa hàng F và G (khu Khoa Kinh tế) đi lên, dừng ở ngang hàng ghế đại biểu
        seg: ['M 799 641 L 799 397'],
        badge: { x: 799, y: 420 },
        nhan: { x: 767, y: 425, neo: 'end' }
    },
    {
        so: 2,
        ten: 'Bước lên sân khấu',
        moTa: 'Sinh viên nghe gọi tên, bước lên sân khấu, Lãnh đạo Khoa chúc mừng',
        // vượt bậc thềm rồi lách sang trái để tránh bàn trao bằng
        seg: ['M 799 390 L 799 318 L 690 318 L 690 158'],
        badge: { x: 799, y: 363 },
        nhan: { x: 767, y: 368, neo: 'end' }
    },
    {
        so: 3,
        ten: 'Nhận bằng, chụp ảnh',
        moTa: 'Sinh viên nhận bằng từ thầy Hiệu trưởng, chụp ảnh',
        // đi vòng phía sau bục nhận bằng, không cắt ngang bục
        seg: ['M 690 158 L 321 158'],
        badge: { x: 520, y: 158 },
        nhan: { x: 520, y: 124, neo: 'middle' }
    },
    {
        so: 4,
        ten: 'Đi xuống hội trường',
        moTa: 'Đi xuống hội trường (kết thúc)',
        // xuống bằng lối bên trái, kết thúc ở khe giữa hàng F và G
        seg: ['M 321 158 L 321 641'],
        badge: { x: 321, y: 455 },
        nhan: { x: 353, y: 460, neo: 'start' }
    }
];
