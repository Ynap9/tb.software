namespace traobang.be.application.ThongKe.Dtos
{
    /// <summary>
    /// Một khoa của plan đang active, kèm danh sách sinh viên nhận bằng của khoa đó
    /// </summary>
    public class KhoaDtos
    {
        public int Id { get; set; }
        public string Ten { get; set; } = String.Empty;
        public string TruongKhoa { get; set; } = String.Empty;
        public int Order { get; set; }

        /// <summary>
        /// <see cref="shared.Constants.TraoBang.TrangThaiSubPlan"/>
        /// </summary>
        public int TrangThai { get; set; }
        public bool IsShow { get; set; }

        /// <summary>
        /// Tổng số sinh viên trong danh sách của khoa, gồm cả sinh viên vắng mặt
        /// </summary>
        public int SoLuongSinhVien { get; set; }
        public int SoLuongThamGia { get; set; }
        public int SoLuongVangMat { get; set; }
        public int SoLuongDaTrao { get; set; }
        public int SoLuongConLai { get; set; }
        public List<SinhVienTraoBangDto> SinhViens { get; set; } =
            new List<SinhVienTraoBangDto>();
    }
}
