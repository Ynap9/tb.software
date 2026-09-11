using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.ThongKe.Implements;
using traobang.be.Controllers.Base;
using traobang.be.shared.HttpRequest;

namespace traobang.be.Controllers
{
    [Route("api/core/thong-ke")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ThongKeController : BaseController
    {
        private readonly IThongKeService _thongKeService;

        public ThongKeController(ILogger<ThongKeController> logger, IThongKeService thongKeService)
            : base(logger)
        {
            _thongKeService = thongKeService;
        }

        /// <summary>
        /// Lấy toàn bộ khoa của plan đang active, mỗi khoa kèm danh sách sinh viên nhận bằng
        /// </summary>
        /// <param name="soLuongSinhVien">số sinh viên trả về cho mỗi khoa: 0 là không kèm sinh viên, số âm là lấy hết, số dương là lấy tối đa bấy nhiêu. Các số đếm luôn tính trên toàn bộ khoa.</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("khoa")]
        public ApiResponse FindAllKhoa([FromQuery] int soLuongSinhVien = 0)
        {
            try
            {
                var data = _thongKeService.FindAllKhoa(soLuongSinhVien);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Lấy hàng chờ trao bằng của plan đang active, xếp theo đúng thứ tự lên nhận bằng
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("sinh-vien")]
        public ApiResponse FindAllSinhVien()
        {
            try
            {
                var data = _thongKeService.FindAllSinhVien();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Lấy chi tiết một khoa: toàn bộ sinh viên trong danh sách nhận bằng kèm
        /// trạng thái đã lên nhận bằng hay chưa
        /// </summary>
        /// <param name="idKhoa">id của SubPlan</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("khoa/{idKhoa}")]
        public ApiResponse FindChiTietKhoa([FromRoute] int idKhoa)
        {
            try
            {
                var data = _thongKeService.FindChiTietKhoa(idKhoa);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
