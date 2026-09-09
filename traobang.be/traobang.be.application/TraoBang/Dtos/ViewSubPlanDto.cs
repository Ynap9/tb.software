using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace traobang.be.application.TraoBang.Dtos
{
    public class ViewSubPlanDto
    {
        public int Id { get; set; }
        public int IdPlan { get; set; }
        public string Ten { get; set; } = String.Empty;
        public string TruongKhoa { get; set; } = String.Empty;
        public string QrTenKhoa { get; set; } = String.Empty;
        public string? MoTa { get; set; } = String.Empty;
        public string? Note { get; set; } = String.Empty;
        public string? MoBai { get; set; } = String.Empty;
        public string? KetBai { get; set; } = String.Empty;
        public int Order { get; set; }
        public bool IsShow { get; set; }   
        public bool IsShowMoBai { get; set; }
        public bool IsShowKetBai { get; set; }
    }
}
