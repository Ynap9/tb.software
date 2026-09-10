import { IBoPhan } from '../models/thong-ke.models';

/**
 * Dữ liệu sơ đồ luồng di chuyển của sinh viên tại buổi lễ — HUCE.
 * Toàn bộ là dữ liệu tĩnh theo Bản phân công nhiệm vụ đón tiếp sinh viên K.71,
 * ngày 21–23/8/2026. Chưa gọi API.
 */

/** Thông tin từng bộ phận đón tiếp, tra theo mã bộ phận */
export const BO_PHAN: Record<string, Omit<IBoPhan, 'id'>> = {
    A: {
        name: 'Kiểm tra điều kiện nhập học',
        room: 'Tầng 2 — Nhà G3',
        color: '#B02525',
        desc: 'Kiểm tra điều kiện nhập học; nhận Giấy chứng nhận kết quả thi tốt nghiệp THPT 2026 hoặc giấy tờ minh chứng nếu trúng tuyển theo phương thức khác. Bao gồm cả 02 vị trí hỗ trợ nhập học trực tuyến.',
        pc: 9,
        qr: 9,
        who: '07 cán bộ + 02 vị trí hỗ trợ nhập học trực tuyến'
    },
    B: { name: 'Hồ sơ đối tượng ưu tiên', room: 'Tầng 2 — Nhà G3', color: '#9FA8DA', desc: 'Nộp hồ sơ minh chứng được hưởng chính sách ưu tiên theo đối tượng trong tuyển sinh (nếu có).', pc: 1, qr: 1, who: '01 cán bộ' },
    C: { name: 'Nộp học phí và các khoản thu', room: 'Tầng 2 — Nhà G3', color: '#3E9E52', desc: 'Thu học phí và các khoản thu đầu khóa cho sinh viên chưa nộp trực tuyến.', pc: 2, qr: 2, pos: 3, who: 'Phòng Kế hoạch — Tài chính' },
    D: {
        name: 'Tư vấn CTĐT hội nhập và quốc tế',
        room: 'Tầng 2 — Nhà G3',
        color: '#B49BD8',
        desc: 'Tư vấn và nhận đăng ký các CTĐT hội nhập, quốc tế: XE, XF, KTE, CTĐT chuẩn đầu ra tiếng Anh. KDE tại P.116 nhà A1.',
        pc: 0,
        qr: 0,
        who: 'Các Khoa có CTĐT'
    },
    E26: { name: 'Thu hồ sơ sinh viên', room: 'P.26 — Nhà H2', color: '#7CC79B', desc: 'Tiếp nhận và kiểm đếm hồ sơ sinh viên nhập học.', pc: 5, qr: 5, who: '05 cán bộ' },
    E25: { name: 'Thu hồ sơ sinh viên', room: 'P.25 — Nhà H2', color: '#7CC79B', desc: 'Tiếp nhận và kiểm đếm hồ sơ sinh viên nhập học.', pc: 5, qr: 5, who: '05 cán bộ' },
    G: { name: 'Tạo lập dữ liệu nhận diện sinh viên', room: 'P.23 — Nhà H2', color: '#F0A268', desc: 'Chụp ảnh và tạo dữ liệu nhận diện phục vụ làm thẻ sinh viên.', pc: 7, qr: 7, cam: 7, who: '07 bàn chụp ảnh' },
    H: { name: 'Sinh hoạt Đảng, Đoàn, Hội sinh viên', room: 'P.22 — Nhà H2', color: '#8B9BE2', desc: 'Chuyển sinh hoạt Đảng, Đoàn và nộp Đoàn phí; đăng ký tham gia Hội sinh viên.', pc: 3, qr: 3, who: '03 cán bộ' },
    I: { name: 'Hồ sơ nghĩa vụ quân sự', room: 'P.21 — Nhà H2', color: '#C3DE9C', desc: 'Nộp hồ sơ di chuyển nghĩa vụ quân sự (đối với nam sinh viên).', pc: 3, qr: 3, who: '03 cán bộ' },
    M: {
        name: 'Nhận áo đồng phục và dịch vụ hỗ trợ',
        room: 'P.24 — Nhà H2',
        color: '#FBE28F',
        desc: 'Phát áo đồng phục (quà tặng của Nhà trường) và các dịch vụ hỗ trợ sinh viên.',
        pc: 0,
        qr: 8,
        who: 'Cán bộ Nhà trường và Công ty Phương Thảo',
        flag: '8 máy quét nhưng chưa bố trí máy tính — cần xác nhận thiết bị đầu cuối'
    },
    N: { name: 'Đăng ký ở nội trú Ký túc xá', room: 'P.24 — Nhà H2', color: '#FBE28F', desc: 'Đăng ký ở nội trú, hỗ trợ tìm thuê nhà trọ cho sinh viên có nguyện vọng.', pc: 0, qr: 0, who: 'Ban Quản lý KTX, P.CTCT&QLSV' }
};

/** Thứ tự hiển thị các bộ phận, đúng theo chiều di chuyển của sinh viên */
export const ORDER: string[] = ['A', 'B', 'C', 'D', 'E26', 'E25', 'G', 'H', 'I', 'M', 'N'];

/** Danh sách bộ phận kèm mã, dùng cho các vòng lặp trong template */
export const DS_BO_PHAN: IBoPhan[] = ORDER.map((id) => ({ id, ...BO_PHAN[id] }));
