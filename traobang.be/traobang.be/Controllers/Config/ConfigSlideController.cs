using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.TraoBang.Dtos.Slide;
using traobang.be.application.TraoBang.Interfaces;
using traobang.be.Attributes;
using traobang.be.Controllers.Base;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest;

namespace traobang.be.Controllers.Config
{
    [Route("api/config/slide")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ConfigSlideController : BaseController
    {
        private readonly ISlideService _slideService;
        public ConfigSlideController(ILogger<ConfigSlideController> logger, ISlideService slideService) : base(logger)
        {
            _slideService = slideService;
        }

        /// <summary>
        /// Tạo slide
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Permission(PermissionKeys.SlideAdd)]
        [HttpPost("")]
        public ApiResponse Create(CreateSlideDto dto)
        {
            try
            {
                _slideService.Create(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Cập nhật slide
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("")]
        [Permission(PermissionKeys.SlideUpdate)]
        public ApiResponse Update(UpdateSlideDto dto)
        {
            try
            {
                _slideService.Update(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Xóa slide
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Permission(PermissionKeys.SlideDelete)]
        public ApiResponse Delete([FromRoute] int id)
        {
            try
            {
                _slideService.Delete(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("")]
        [Permission(PermissionKeys.SlideView)]
        public ApiResponse FindPaging([FromQuery] FindPagingSlideDto dto)
        {
            try
            {
                var result = _slideService.FindPaging(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}")]
        [Permission(PermissionKeys.SlideView)]
        public ApiResponse FindById([FromRoute] int id)
        {
            try
            {
                var result = _slideService.FindById(id);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Tải file mẫu để import slide
        /// </summary>
        /// <returns></returns>
        [HttpGet("export/template-import-slide")]
        [Permission(PermissionKeys.SlideAdd)]
        public IActionResult ExportTemplateImportSlide()
        {
            try
            {
                var excelTemplate = _slideService.DownloadTemplateImport();

                return File(
                   excelTemplate,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "ImportSlide.xlsx"
                 );

            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(ex.Message));
            }
        }

        /// <summary>
        /// Xuất excel danh sách slide sinh viên
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet("export/slide-sinh-vien")]
        [Permission(PermissionKeys.SlideView)]
        public IActionResult ExportSlideSinhVien([FromQuery] ExportSlideSinhVienDto dto)
        {
            try
            {
                var excel = _slideService.ExportSlideSinhVien(dto);

                return File(
                   excel,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "SlideSinhVien.xlsx"
                 );

            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(ex.Message));
            }
        }

        /// <summary>
        /// Import excel slide
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("import/slide")]
        [Permission(PermissionKeys.SlideAdd)]
        public ApiResponse ImportSlide([FromForm] ImportExcelSlideDto dto)
        {
            try
            {
                _slideService.ImportSlide(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Sinh mã QR cho danh sách SV nhận bằng
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("qr")]
        [Permission(PermissionKeys.SlideUpdate)]
        public async Task<ApiResponse> GenerateQr([FromBody] GenerateSinhVienQrDto dto)
        {
            try
            {
                await _slideService.GenerateQr(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Sinh mã QR cho 1 SV nhận bằng
        /// </summary>
        /// <param name="idSlide"></param>
        /// <returns></returns>
        [HttpPost("qr/slide/{idSlide}")]
        [Permission(PermissionKeys.SlideUpdate)]
        public async Task<ApiResponse> GenerateQrOneSV([FromRoute] int idSlide)
        {
            try
            {
                await _slideService.GenerateQrOneSv(idSlide);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
