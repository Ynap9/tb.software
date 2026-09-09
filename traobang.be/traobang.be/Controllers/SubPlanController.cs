using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using traobang.be.application.TraoBang.Dtos;
using traobang.be.application.TraoBang.Dtos.SubPlan;
using traobang.be.application.TraoBang.Interface;
using traobang.be.Attributes;
using traobang.be.Controllers.Base;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.HttpRequest;

namespace traobang.be.Controllers
{
    [Route("api/core/trao-bang/sub-plan")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SubPlanController : BaseController
    {
        private readonly ISubPlanService _subPlanService;

        public SubPlanController(ILogger<SubPlanController> logger, ISubPlanService subPlanService) : base(logger)
        {
            _subPlanService = subPlanService;
        }

        [Permission(PermissionKeys.SubPlanAdd)]
        [HttpPost("{idPlan}")]
        public ApiResponse Create([FromRoute] int idPlan, [FromBody] CreateSubPlanDto dto)
        {
            try
            {
                _subPlanService.Create(idPlan, dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanUpdate)]
        [HttpPut("")]
        public async Task<ApiResponse> Update([FromBody] UpdateSubPlanDto dto)
        {
            try
            {
                await _subPlanService.Update(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanView)]
        [HttpGet("")]
        public ApiResponse FindPaging([FromQuery] FindPagingSubPlanDto dto)
        {
            try
            {
                var result = _subPlanService.FindPaging(dto);
                return new()
                {
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanUpdate)]
        [HttpPut("is-show")]
        public ApiResponse UpdateIsShow([FromBody] UpdateSubPlanIsShowDto dto)
        {
            try
            {
                _subPlanService.UpdateIsShow(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanDelete)]
        [HttpDelete("{idSubPlan}/plan/{idPlan}")]
        public ApiResponse Delete([FromRoute] int idPlan, [FromRoute] int idSubPlan)
        {
            try
            {
                _subPlanService.Delete(idPlan, idSubPlan);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanUpdate)]
        [HttpPut("sort")]
        public ApiResponse Sort([FromBody] MoveOrderSubPlanDto dto)
        {
            try
            {
                var data = _subPlanService.MoveOrder(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanView)]
        [HttpGet("plan/{idPlan}/list")]
        public async Task<ApiResponse> ListSubPlan([FromRoute] int idPlan)
        {
            try
            {
                var result = await _subPlanService.GetListSubPlan(idPlan);
                return new(result);

            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanAdd)]
        [HttpPost("import-ggsheet")]
        public async Task<ApiResponse> ImportSubPlanFromGgSheet([FromBody] ImportDanhSachSinhVienNhanBangDto dto)
        {
            try
            {
                var data = await _subPlanService.ImportDanhSachNhanBang(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        //[Permission(PermissionKeys.SubPlanView)]
        //[HttpGet("paging-danh-sach-sinh-vien-nhan-bang")]
        //public ApiResponse PagingSinhVienNhanBang([FromQuery] FindPagingSinhVienNhanBangDto dto)
        //{
        //    try
        //    {
        //        var data = _subPlanService.PagingSinhVienNhanBang(dto);
        //        return new(data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return OkException(ex);
        //    }
        //}

        //[Permission(PermissionKeys.SubPlanAdd)]
        //[HttpPost("sinh-vien-nhan-bang")]
        //public ApiResponse CreateSinhVienNhanBang([FromBody] CreateSinhVienNhanBangDto dto)
        //{
        //    try
        //    {
        //        _subPlanService.CreateSinhVienNhanBang(dto);
        //        return new();
        //    }
        //    catch (Exception ex)
        //    {
        //        return OkException(ex);
        //    }
        //}

        //[Permission(PermissionKeys.SubPlanUpdate)]
        //[HttpPut("sinh-vien-nhan-bang")]
        //public async Task<ApiResponse> UpdateSinhVienNhanBang([FromBody] UpdateSinhVienNhanBangDto dto)
        //{
        //    try
        //    {
        //        await _subPlanService.UpdateSinhVienNhanBang(dto);
        //        return new();
        //    }
        //    catch (Exception ex)
        //    {
        //        return OkException(ex);
        //    }
        //}

        //[Permission(PermissionKeys.SubPlanDelete)]
        //[HttpDelete("{idSubPlan}/sinh-vien-nhan-bang/{id}")]
        //public ApiResponse DeleteSinhVienNhanBang([FromRoute] int idSubPlan, [FromRoute] int id)
        //{
        //    try
        //    {
        //        _subPlanService.DeleteSinhVienNhanBang(idSubPlan, id);
        //        return new();
        //    }
        //    catch (Exception ex)
        //    {
        //        return OkException(ex);
        //    }
        //}

        [AllowAnonymous]
        [HttpGet("sinh-vien-nhan-bang/{mssv}")]
        public async Task<ApiResponse> GetByMssv([FromRoute] string mssv)
        {
            try
            {
                var data = await _subPlanService.ShowSinhVienNhanBangInfor(mssv);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [AllowAnonymous]
        [HttpGet("sinh-vien-nhan-bang/{mssv}/next")]
        public async Task<ApiResponse> GetNextByMssv([FromRoute] string mssv)
        {
            try
            {
                var data = await _subPlanService.NextSinhVienNhanBang(mssv);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [AllowAnonymous]
        [HttpGet("sinh-vien-nhan-bang/{mssv}/prev")]
        public async Task<ApiResponse> GetPrevMssv([FromRoute] string mssv)
        {
            try
            {
                var data = await _subPlanService.PreviousSinhVienNhanBang(mssv);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Check sinh viên đã có trong hàng đợi chưa. Trả về trạng thái của sinh viên trong hàng đợi, chưa có thì trả về null
        /// </summary>
        /// <param name="mssv"></param>
        /// <returns></returns>
        [Permission(PermissionKeys.PushSinhVienVaoHangDoi)]
        [HttpGet("sinh-vien-nhan-bang/hang-doi/check")]
        public ApiResponse CheckSinhVienTrongHangDoi([FromQuery] string mssv)
        {
            try
            {
                var data = _subPlanService.CheckSinhVienTrongHangDoi(mssv);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.PushSinhVienVaoHangDoi)]
        [HttpPost("sinh-vien-nhan-bang/hang-doi")]
        public async Task<ApiResponse> DiemDanhNhanBang([FromQuery] string mssv)
        {
            try
            {
                var data = await _subPlanService.DiemDanhNhanBang(mssv);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Get tien do sinh vien nhan bang
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Permission(PermissionKeys.GetTienDo, PermissionKeys.DieuKhienView, PermissionKeys.CheckinView)]
        [HttpGet("sinh-vien-nhan-bang/tien-do")]
        public async Task<ApiResponse> GetTienDoNhanBang([FromQuery] ViewTienDoNhanBangRequestDto dto)
        {
            try
            {
                var data = await _subPlanService.GetTienDoNhanBang(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.DieuKhienView, PermissionKeys.CheckinView, PermissionKeys.SubPlanView)]
        [HttpGet("{idSubPlan}/thong-tin-subplan")]
        public async Task<ApiResponse> GetThongTinSubPlan([FromRoute] int idSubPlan)
        {
            try
            {
                var data = await _subPlanService.GetInforSubPlan(idSubPlan);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.SubPlanUpdate, PermissionKeys.CheckinView)]
        [HttpPut("{id}/trang-thai-sub-plan")]
        public ApiResponse UpdateTrangThaiSubPlan([FromRoute] int id)
        {
            try
            {
                _subPlanService.UpdateTrangThaiSubPlan(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }

        }

        [Permission(PermissionKeys.NextSubPlan)]
        [HttpPost("next-sub-plan")]
        public async Task<ApiResponse> NextSubPlan()
        {
            try
            {
                var data = await _subPlanService.NextSubPlan();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Lấy danh sách subplan của plan active
        /// </summary>
        /// <returns></returns>
        [Permission(PermissionKeys.DieuKhienView, PermissionKeys.CheckinView)]
        [HttpGet("plan/active/list-sub-plan-infor")]
        public async Task<ApiResponse> GetListSubPlanInfor()
        {
            try
            {
                var data = await _subPlanService.GetListSubPlanInfor();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.UpdateTrangThaiSv)]
        [HttpPut("{idSubPlan}/sinh-vien-nhan-bang/{id}/trang-thai")]
        public ApiResponse UpdateTrangThaiSinhVienNhanBang([FromRoute] int idSubPlan, [FromRoute] int id)
        {
            try
            {
                _subPlanService.UpdateTrangThaiSinhVienNhanBang(idSubPlan, id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [AllowAnonymous]
        [HttpGet("infor-sinh-vien-dang-trao")]
        public async Task<ApiResponse> GetInforSinhVienDangTrao()
        {
            try
            {
                var data = await _subPlanService.GetSinhVienDangTraoBang();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.DieuKhienView, PermissionKeys.CheckinView)]
        [HttpGet("tien-do-trao-bang")]
        public async Task<ApiResponse> GetTienDoTraoBang()
        {
            try
            {
                var data = await _subPlanService.GetTienDoTraoBang();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [AllowAnonymous]
        [HttpPost("{idSubPlan}/sinh-vien-nhan-bang/next-trao-bang")]
        public async Task<ApiResponse> NextSinhVienTraoBang([FromRoute] int idSubPlan)
        {
            try
            {
                var data = await _subPlanService.NextSinhVienTraoBang(idSubPlan);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [AllowAnonymous]
        [HttpPost("{idSubPlan}/sinh-vien-nhan-bang/prev-trao-bang")]
        public async Task<ApiResponse> PrevSinhVienTraoBang([FromRoute] int idSubPlan)
        {
            try
            {
                var data = await _subPlanService.PrevSinhVienTraoBang(idSubPlan);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.DieuKhienView)]
        [HttpGet("{idSubPlan}/sinh-vien-nhan-bang/chuan-bi")]
        public async Task<ApiResponse> GetInforSinhVienChuanBiDuocTraoBang([FromRoute] int idSubPlan)
        {
            try
            {
                var data = await _subPlanService.GetInforSinhVienChuanBiDuocTraoBang(idSubPlan);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [AllowAnonymous]
        [HttpGet("danh-sach-sinh-vien-nhan-bang-khoa")]
        public async Task<ApiResponse> GetInforSubPlanDangTrao([FromQuery] int soLuong)
        {
            try
            {
                var data = await _subPlanService.GetInforSubPlanDangTrao(soLuong);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.PushSinhVienVaoHangDoi)]
        [HttpPost("sinh-vien-nhan-bang/hang-doi/truong-hop-dac-biet")]
        public async Task<ApiResponse> DiemDanhNhanBangTruongHopDacBiet([FromQuery] string mssv)
        {
            try
            {
                var data = await _subPlanService.DiemDanhNhanBangTruongHopDacBiet(mssv);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Đẩy slide thường vào hàng đợi cho mc đọc
        /// </summary>
        /// <param name="idSubPlan"></param>
        /// <returns></returns>
        [Permission(PermissionKeys.PushSlideVaoHangDoi)]
        [HttpPost("sinh-vien-nhan-bang/hang-doi/slide-thuong")]
        public async Task<ApiResponse> ChuanBiSlideNormal([FromBody] CutSlideDto dto)
        {
            try
            {
                var data = await _subPlanService.CutSlideNormal(dto.IdSubPlan);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Xóa tiến độ trao bằng, log của plan đang active
        /// </summary>
        /// <returns></returns>
        [Permission(PermissionKeys.RestartActivePlan)]
        [HttpPost("restart")]
        public async Task<ApiResponse> Restart()
        {
            try
            {
                await _subPlanService.Restart();
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{idSubPlan}/infor-sinh-vien-prev")]
        public async Task<ApiResponse> GetInforSinhVienPrev([FromRoute] int idSubPlan)
        {
            try
            {
                var data = await _subPlanService.GetInforSinhVienBatDauDuocPrev(idSubPlan);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("sinh-vien-prev/tien-do")]
        [Permission(PermissionKeys.GetTienDo)]
        public async Task<ApiResponse> GetTienDoNhanBangSinhVienBatDauPrev([FromQuery] ViewTienDoNhanBangSinhVienBatDauLuiRequestDto dto)
        {
            try
            {
                var data = await _subPlanService.GetTienDoNhanBangSinhVienBatDauPrev(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Download template excel import khoa
        /// </summary>
        /// <returns></returns>
        [HttpGet("export/template-import-subplan")]
        [Permission(PermissionKeys.SubPlanAdd)]
        public IActionResult ExportTemplateImportSlide()
        {
            try
            {
                var excelTemplate = _subPlanService.DownloadTemplateImport();

                return File(
                   excelTemplate,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "ImportKhoa.xlsx"
                 );

            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(ex.Message));
            }
        }

        /// <summary>
        /// Import excel khoa
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("import/sub-plan")]
        [Permission(PermissionKeys.SubPlanAdd)]
        public ApiResponse ImportSubPlan([FromForm] ImportExcelSubPlanDto dto)
        {
            try
            {
                _subPlanService.ImportExcelSubplan(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }


    }
}
