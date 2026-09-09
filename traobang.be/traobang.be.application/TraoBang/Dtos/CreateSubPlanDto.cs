using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace traobang.be.application.TraoBang.Dtos
{
    public class CreateSubPlanDto
    {
        public string Ten { get; set; } = String.Empty;
        public string TruongKhoa { get; set; } = String.Empty;
        public string? QrTenKhoa { get; set; } = String.Empty;
        public string? MoTa { get; set; } = String.Empty;
        public string? Note { get; set; } = String.Empty;
        public string? MoBai { get; set; } = String.Empty;
        public string? KetBai { get; set; } = String.Empty;
        public bool IsShowMoBai { get; set; }
        public bool IsShowKetBai { get; set; }
        //public int Order { get; set; }

    }
}
