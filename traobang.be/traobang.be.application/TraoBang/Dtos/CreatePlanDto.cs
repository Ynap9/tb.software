namespace traobang.be.application.TraoBang.Dtos
{
    public class CreatePlanDto
    {
        public string Ten { get; set; } = String.Empty;
        public string MoTa { get; set; } = String.Empty;
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public int? IdGiaoDien { get; set; }
    }
}
