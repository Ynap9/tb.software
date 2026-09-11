using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace traobang.be.application.Auth.Dtos.User
{
    public class ChangePasswordDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Không được bỏ trống")]
        required public string CurrentPassword { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Không được bỏ trống")]
        required public string NewPassword { get; set; }
    }
}
