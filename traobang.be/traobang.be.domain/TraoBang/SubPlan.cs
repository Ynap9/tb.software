using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using thongbao.be.shared.Interfaces;
using traobang.be.shared.Constants.Db;

namespace traobang.be.domain.TraoBang
{
    [Table(nameof(SubPlan), Schema = DbSchemas.TraoBang)]
    [Index(
     nameof(Id),
     IsUnique = false,
     Name = $"IX_{nameof(SubPlan)}"
 )]
    public class SubPlan : ISoftDelted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int IdPlan { get; set; }
        public string Ten { get; set; } = String.Empty;
        public string TruongKhoa { get; set; } = String.Empty;
        /// <summary>
        /// Tên khoa in trên mã QR của sinh viên thuộc khoa này
        /// </summary>
        [MaxLength(500)]
        public string QrTenKhoa { get; set; } = String.Empty;
        public string? MoTa { get; set; } = String.Empty;
        public string? Note { get; set; } = String.Empty;
        public string MoBai { get; set; } = String.Empty;
        public string KetBai { get; set; } = String.Empty;
        public string MoBaiNote { get; set; } = String.Empty;
        public string KetBaiNote { get; set; } = String.Empty;
        public int TrangThai { get; set; }
        public int Order { get; set; }
        public bool IsShow { get; set; }
        public bool IsShowMoBai { get; set; }
        public bool IsShowKetBai { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
        public string? DeletedBy { get; set; }
    }
}
