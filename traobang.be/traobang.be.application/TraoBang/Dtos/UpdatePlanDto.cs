using traobang.be.shared.Constants.TraoBang;
using traobang.be.shared.Validations;

namespace traobang.be.application.TraoBang.Dtos
{
    public class UpdatePlanDto
    {
        public string Ten { get; set; } = String.Empty;
        public string MoTa { get; set; } = String.Empty;
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }

        [IntegerRange(AllowableValues = new int[] { TrangThaiPlan.KhoiTao, TrangThaiPlan.DaKetThuc, TrangThaiPlan.DangHoatDong })]
        public int TrangThai { get; set; }
        public int? IdGiaoDien { get; set; }
    }
}
