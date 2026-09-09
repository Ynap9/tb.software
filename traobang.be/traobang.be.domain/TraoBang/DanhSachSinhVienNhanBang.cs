using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using thongbao.be.shared.Interfaces;
using traobang.be.shared.Constants.Db;

namespace traobang.be.domain.TraoBang
{
    [Table(nameof(DanhSachSinhVienNhanBang), Schema = DbSchemas.TraoBang)]
    [Index(
     nameof(Id),
     IsUnique = false,
     Name = $"IX_{nameof(DanhSachSinhVienNhanBang)}"
 )]
    public class DanhSachSinhVienNhanBang : ISoftDelted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Obsolete("Sắp xóa")]
        public int IdSubPlan { get; set; }
        [MaxLength(500)]

        public string HoVaTen { get; set; } = String.Empty;
        [MaxLength(500)]
        public string Email { get; set; } = String.Empty;
        [MaxLength(500)]
        public string EmailSinhVien { get; set; } = String.Empty;
        [MaxLength(20)]
        public string MaSoSinhVien { get; set; } = String.Empty;
        [MaxLength(50)]
        public string Lop { get; set; } = String.Empty;
        public DateTime? NgaySinh { get; set; }
        [MaxLength(500)]
        public string CapBang { get; set; } = String.Empty;
        [MaxLength(1000)]
        public string TenNganhDaoTao { get; set; } = String.Empty;
        [MaxLength(500)]
        public string XepHang { get; set; } = String.Empty;
        [MaxLength(500)]
        public string ThanhTich { get; set; } = String.Empty;
        [MaxLength(500)]
        public string KhoaQuanLy { get; set; } = String.Empty;
        [MaxLength(500)]
        public string SoQuyetDinhTotNghiep { get; set; } = String.Empty;
        public DateTime? NgayQuyetDinh { get; set; }
        public string? Note { get; set; }
        [Obsolete("Sắp xóa")]
        public bool IsShow { get; set; }
        [Obsolete("Sắp xóa")]
        public int Order { get; set; }
        [Obsolete("Sắp xóa")]
        public int TrangThai { get; set; }
        public string LinkQR { get; set; } = String.Empty;
        public string LinkQrOnly { get; set; } = String.Empty;
        [MaxLength(500)]
        public string QrHoTen { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
        public string? DeletedBy { get; set; }
    }
}

