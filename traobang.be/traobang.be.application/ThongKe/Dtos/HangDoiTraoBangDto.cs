namespace traobang.be.application.ThongKe.Dtos
{
    /// <summary>
    /// Hàng chờ trao bằng của plan đang active, xếp theo đúng thứ tự lên nhận bằng
    /// </summary>
    public class HangDoiTraoBangDto
    {
        /// <summary>
        /// Tổng số sinh viên đang có mặt trong hàng chờ
        /// </summary>
        public int TongSinhVien { get; set; }
        public int SoLuongDaTrao { get; set; }
        public int SoLuongDangTrao { get; set; }
        public int SoLuongChuanBi { get; set; }
        public List<SinhVienHangDoiDto> Items { get; set; } = new List<SinhVienHangDoiDto>();
    }

    /// <summary>
    /// Một sinh viên trong hàng chờ trao bằng
    /// </summary>
    public class SinhVienHangDoiDto
    {
        /// <summary>
        /// Thứ tự lên nhận bằng tính trên toàn buổi lễ, đánh lại liên tục từ 1
        /// </summary>
        public int Stt { get; set; }

        /// <summary>
        /// Id bản ghi tiến độ trao bằng
        /// </summary>
        public int Id { get; set; }
        public int IdSinhVienNhanBang { get; set; }
        public int IdKhoa { get; set; }
        public string TenKhoa { get; set; } = String.Empty;

        /// <summary>
        /// Thứ tự của khoa trong buổi lễ
        /// </summary>
        public int OrderKhoa { get; set; }

        /// <summary>
        /// Thứ tự trong hàng chờ của riêng khoa đó
        /// </summary>
        public int Order { get; set; }
        public string HoVaTen { get; set; } = String.Empty;
        public string MaSoSinhVien { get; set; } = String.Empty;
        public string Lop { get; set; } = String.Empty;
        public string TenNganhDaoTao { get; set; } = String.Empty;
        public string CapBang { get; set; } = String.Empty;
        public string XepHang { get; set; } = String.Empty;
        public string? Note { get; set; }

        /// <summary>
        /// <see cref="shared.Constants.TraoBang.TraoBangConstants"/>:
        /// 2 chuẩn bị, 3 đang trao bằng, 4 đã trao bằng
        /// </summary>
        public int TrangThai { get; set; }
    }
}
