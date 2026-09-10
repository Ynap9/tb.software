namespace traobang.be.application.ThongKe.Dtos
{
    /// <summary>
    /// Chi tiết một khoa: thông tin khoa kèm toàn bộ sinh viên trong danh sách nhận bằng,
    /// mỗi sinh viên có trạng thái tổng hợp cho biết đã lên nhận bằng hay chưa
    /// </summary>
    public class ChiTietKhoaDto
    {
        public int Id { get; set; }
        public string Ten { get; set; } = String.Empty;
        public string TruongKhoa { get; set; } = String.Empty;
        public int Order { get; set; }

        /// <summary>
        /// <see cref="shared.Constants.TraoBang.TrangThaiSubPlan"/>
        /// </summary>
        public int TrangThai { get; set; }

        /// <summary>
        /// Tổng số sinh viên trong danh sách, gồm cả sinh viên vắng mặt
        /// </summary>
        public int SoLuongSinhVien { get; set; }
        public int SoLuongThamGia { get; set; }
        public int SoLuongVangMat { get; set; }

        /// <summary>
        /// Đã lên nhận bằng xong
        /// </summary>
        public int SoLuongDaTrao { get; set; }

        /// <summary>
        /// Đang đứng trên sân khấu
        /// </summary>
        public int SoLuongDangTrao { get; set; }

        /// <summary>
        /// Đã check-in, đang đứng chờ trong hàng đợi
        /// </summary>
        public int SoLuongDaCheckIn { get; set; }

        /// <summary>
        /// Chưa check-in, chưa có mặt trong hàng đợi
        /// </summary>
        public int SoLuongChuaCheckIn { get; set; }

        public List<SinhVienChiTietDto> SinhViens { get; set; } = new List<SinhVienChiTietDto>();
    }

    /// <summary>
    /// Một sinh viên trong danh sách nhận bằng của khoa
    /// </summary>
    public class SinhVienChiTietDto
    {
        /// <summary>
        /// Số thứ tự trong danh sách của khoa, đánh theo thứ tự slide
        /// </summary>
        public int Stt { get; set; }
        public int Id { get; set; }
        public int IdSlide { get; set; }
        public string MaSoSinhVien { get; set; } = String.Empty;
        public string HoVaTen { get; set; } = String.Empty;
        public string Lop { get; set; } = String.Empty;
        public string TenNganhDaoTao { get; set; } = String.Empty;
        public string CapBang { get; set; } = String.Empty;
        public string XepHang { get; set; } = String.Empty;
        public string ThanhTich { get; set; } = String.Empty;
        public string? Note { get; set; }

        /// <summary>
        /// Thứ tự slide trong khoa
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Thứ tự trong hàng đợi, null nghĩa là chưa được đẩy vào hàng đợi
        /// </summary>
        public int? OrderHangDoi { get; set; }

        /// <summary>
        /// Đã lên nhận bằng xong hay chưa
        /// </summary>
        public bool DaLenNhanBang { get; set; }

        /// <summary>
        /// Trạng thái tổng hợp, dùng chung bảng giá trị với
        /// <see cref="shared.Constants.TraoBang.TraoBangConstants"/>:
        /// 1 chưa check-in, 2 đã check-in đang chờ, 3 đang nhận bằng,
        /// 4 đã nhận bằng, 6 vắng mặt
        /// </summary>
        public int TrangThai { get; set; }
    }
}
