using AutoMapper;

using EFCore.BulkExtensions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using traobang.be.application.Base;

using traobang.be.application.TraoBang.Dtos;
using traobang.be.application.TraoBang.Dtos.Slide;
using traobang.be.application.TraoBang.Dtos.SubPlan;
using traobang.be.application.TraoBang.Interface;
using traobang.be.domain.TraoBang;
using traobang.be.infrastructure.data;
using traobang.be.infrastructure.external.Excel;
using traobang.be.infrastructure.external.SignalR.Service.Interfaces;
using traobang.be.shared.Constants.TraoBang;
using traobang.be.shared.HttpRequest.AppException;
using traobang.be.shared.HttpRequest.BaseRequest;
using traobang.be.shared.HttpRequest.Error;


namespace traobang.be.application.TraoBang.Implements
{
    public class SubPlanService : BaseService, ISubPlanService
    {
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private readonly ITraoBangService _traoBangService;
        private readonly IExcelService _excelService;
        private readonly IConfiguration _configuration;
        public SubPlanService(
            TbDbContext tbDbContext,
            ILogger<SubPlanService> logger,
            IHttpContextAccessor httpContextAccessor,
            IExcelService excelService,
            IMapper mapper,
            IConfiguration configuration,
            ITraoBangService traoBangService
        )
            : base(tbDbContext, logger, httpContextAccessor, mapper)
        {
            _configuration = configuration;
            _traoBangService = traoBangService;
            _excelService = excelService;
        }
        public void Create(int idPlan, CreateSubPlanDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto = {JsonSerializer.Serialize(dto)}");

            var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == idPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var maxOrder = _tbDbContext.SubPlans
                .Where(x => x.IdPlan == idPlan && !x.Deleted)
                .Max(x => (int?)x.Order) ?? 0;

            var subplan = new domain.TraoBang.SubPlan
            {
                Ten = dto.Ten,
                MoTa = dto.MoTa,
                TruongKhoa = dto.TruongKhoa,
                QrTenKhoa = dto.QrTenKhoa ?? "",
                MoBai = dto.MoBai ?? "",
                KetBai = dto.KetBai ?? "",
                Note = dto.Note,
                IsShow = true,
                Order = maxOrder + 1,
                IdPlan = idPlan,
                IsShowMoBai = dto.IsShowMoBai,
                IsShowKetBai = dto.IsShowKetBai,
                CreatedDate = GetVietnamTime(),
                Deleted = false
            };

            _tbDbContext.SubPlans.Add(subplan);
            _tbDbContext.SaveChanges();
        }
        public async Task Update(UpdateSubPlanDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, dto = {JsonSerializer.Serialize(dto)}");

            var plan = await _tbDbContext.Plans
                .FirstOrDefaultAsync(x => x.Id == dto.IdPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var subPlans = await _tbDbContext.SubPlans
                .Where(x => x.IdPlan == dto.IdPlan && !x.Deleted)
                .OrderBy(x => x.Order)
                .ToListAsync();

            var subplan = subPlans.FirstOrDefault(x => x.Id == dto.IdSubPlan)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);

            var currentOrder = subplan.Order;
            var newOrder = dto.NewOrder;

            if (newOrder < 1 || newOrder > subPlans.Count)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanOrderInvalid);
            }

            if (currentOrder != newOrder)
            {
                if (newOrder < currentOrder)
                {
                    foreach (var sp in subPlans)
                    {
                        if (sp.Order >= newOrder && sp.Order < currentOrder)
                        {
                            sp.Order++;
                        }
                    }
                }
                else
                {
                    foreach (var sp in subPlans)
                    {
                        if (sp.Order <= newOrder && sp.Order > currentOrder)
                        {
                            sp.Order--;
                        }
                    }
                }
            }

            subplan.Ten = dto.Ten;
            subplan.MoTa = dto.MoTa;
            subplan.TruongKhoa = dto.TruongKhoa;
            subplan.QrTenKhoa = dto.QrTenKhoa ?? "";
            subplan.MoBai = dto.MoBai ?? "";
            subplan.KetBai = dto.KetBai ?? "";
            subplan.Note = dto.Note;
            subplan.Order = newOrder;
            subplan.IsShow = dto.IsShow;
            subplan.IsShowMoBai = dto.IsShowMoBai;
            subplan.IsShowKetBai = dto.IsShowKetBai;

            await _tbDbContext.SaveChangesAsync();
        }
        public BaseResponsePagingDto<ViewSubPlanDto> FindPaging(FindPagingSubPlanDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto = {JsonSerializer.Serialize(dto)}");
            var query = from sp in _tbDbContext.SubPlans.AsNoTracking().Where(x => !x.Deleted)
                        join p in _tbDbContext.Plans.AsNoTracking().Where(x => !x.Deleted) on sp.IdPlan equals p.Id
                        where
                            (string.IsNullOrEmpty(dto.Keyword) || (sp.Ten.ToLower().Contains(dto.Keyword.ToLower()))) &&
                            (dto.IdPlan == null || p.Id == dto.IdPlan)
                        orderby p descending, sp.IdPlan, sp.Order ascending
                        select sp;
            var data = query.Paging(dto).ToList();
            var items = _mapper.Map<List<ViewSubPlanDto>>(data);
            return new BaseResponsePagingDto<ViewSubPlanDto>
            {
                TotalItems = query.Count(),
                Items = items
            };
        }
        public void UpdateIsShow(UpdateSubPlanIsShowDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateIsShow)}, dto= {JsonSerializer.Serialize(dto)} ");
            var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == dto.IdPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            var subplan = _tbDbContext.SubPlans.FirstOrDefault(x => x.Id == dto.Id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);
            subplan.IsShow = dto.IsShow;
            _tbDbContext.SubPlans.Update(subplan);
            _tbDbContext.SaveChanges();
        }
        public void Delete(int idPlan, int idSubPlan)
        {
            _logger.LogInformation($"{nameof(Delete)}, idPlan= {idPlan}, idSubPlan= {idSubPlan} ");
            var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == idPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            var subplan = _tbDbContext.SubPlans.FirstOrDefault(x => x.Id == idSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);
            var vietnamNow = GetVietnamTime();
            subplan.Deleted = true;
            subplan.DeletedDate = vietnamNow;
            _tbDbContext.SubPlans.Update(subplan);
            _tbDbContext.SaveChanges();
        }
        public async Task<List<UpdateOrderSubPlanResponseDto>> MoveOrder(MoveOrderSubPlanDto dto)
        {
            _logger.LogInformation($"{nameof(MoveOrder)}, dto= {JsonSerializer.Serialize(dto)} ");
            var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == dto.IdPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            var subplan = _tbDbContext.SubPlans.FirstOrDefault(x => x.Id == dto.IdSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);
            var subPlans = _tbDbContext.SubPlans
                .Where(x => x.IdPlan == dto.IdPlan && !x.Deleted)
                .OrderBy(x => x.Order)
                .ToList();
            var movingSubPlan = subPlans.FirstOrDefault(x => x.Id == dto.IdSubPlan);
            if (movingSubPlan == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);
            }
            var currentOrder = movingSubPlan.Order;
            var newOrder = dto.NewOrder;
            if (currentOrder == newOrder)
            {
                return subPlans.Select(x => new UpdateOrderSubPlanResponseDto
                {
                    Id = x.Id,
                    Ten = x.Ten,
                    Order = x.Order
                })
                .OrderBy(x => x.Order)
                .ToList();
            }

            if (newOrder < currentOrder)
            {
                foreach (var sp in subPlans)
                {
                    if (sp.Order >= newOrder && sp.Order < currentOrder)
                    {
                        sp.Order++;
                    }

                }
            }
            else
            {
                foreach (var sp in subPlans)
                {
                    if (sp.Order <= newOrder && sp.Order > currentOrder)
                    {
                        sp.Order--;
                    }
                }
            }
            movingSubPlan.Order = newOrder;
            _tbDbContext.SubPlans.UpdateRange(subPlans);
            await _tbDbContext.SaveChangesAsync();
            return subPlans.Select(x => new UpdateOrderSubPlanResponseDto
            {
                Id = x.Id,
                Ten = x.Ten,
                Order = x.Order
            })
            .OrderBy(x => x.Order)
            .ToList();

        }
        public BaseResponsePagingDto<ViewSinhVienNhanBangDto> PagingSinhVienNhanBang(FindPagingSinhVienNhanBangDto dto)
        {
            _logger.LogInformation($"{nameof(PagingSinhVienNhanBang)}, dto= {JsonSerializer.Serialize(dto)} ");

            var query = from sv in _tbDbContext.DanhSachSinhVienNhanBangs
                        where !sv.Deleted && sv.IdSubPlan == dto.IdSubPlan && sv.TrangThai == TraoBangConstants.ThamGiaTraoBang
                        orderby sv.Order ascending
                        select sv;

            var data = query.Paging(dto).ToList();
            var items = _mapper.Map<List<ViewSinhVienNhanBangDto>>(data);

            return new BaseResponsePagingDto<ViewSinhVienNhanBangDto>
            {
                TotalItems = query.Count(),
                Items = items
            };
        }
        public async Task<List<GetListSubPlanResponseDto>> GetListSubPlan(int idPlan)
        {
            _logger.LogInformation($"{nameof(GetListSubPlan)}, idPlan= {idPlan} ");
            var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == idPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            var subPlans = await _tbDbContext.SubPlans
                                .AsNoTracking()
                                .Where(x => x.IdPlan == idPlan && !x.Deleted)
                                .OrderBy(x => x.Id)
                                .Select(x => new GetListSubPlanResponseDto
                                {
                                    Id = x.Id,
                                    Ten = x.Ten
                                }).ToListAsync();

            return subPlans;
        }
        public void CreateSinhVienNhanBang(CreateSinhVienNhanBangDto dto)
        {
            _logger.LogInformation($"{nameof(CreateSinhVienNhanBang)}, dto= {JsonSerializer.Serialize(dto)} ");

            var subPlan = _tbDbContext.SubPlans.FirstOrDefault(x => x.Id == dto.IdSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);

            var existingSinhVien = _tbDbContext.DanhSachSinhVienNhanBangs
                .FirstOrDefault(x => !x.Deleted && x.MaSoSinhVien.ToLower() == dto.MaSoSinhVien.ToLower());

            if (existingSinhVien != null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienDaTonTai);
            }

            var maxOrder = _tbDbContext.DanhSachSinhVienNhanBangs
                .Where(x => x.IdSubPlan == dto.IdSubPlan && !x.Deleted)
                .Max(x => (int?)x.Order) ?? 0;

            var vietnamNow = GetVietnamTime();

            var sinhvien = new domain.TraoBang.DanhSachSinhVienNhanBang
            {
                IdSubPlan = dto.IdSubPlan,
                HoVaTen = dto.HoVaTen,
                MaSoSinhVien = dto.MaSoSinhVien,
                Lop = dto.Lop,
                NgaySinh = dto.NgaySinh,
                CapBang = dto.CapBang,
                TenNganhDaoTao = dto.TenNganhDaoTao,
                XepHang = dto.XepHang,
                ThanhTich = dto.ThanhTich,
                Email = dto.Email,
                EmailSinhVien = $"{dto.MaSoSinhVien}@st.huce.edu.vn",
                KhoaQuanLy = dto.KhoaQuanLy,
                SoQuyetDinhTotNghiep = dto.SoQuyetDinhTotNghiep,
                NgayQuyetDinh = dto.NgayQuyetDinh,
                Note = dto.Note,
                IsShow = true,
                Order = maxOrder + 1,
                TrangThai = dto.TrangThai,
                LinkQR = dto.LinkQR,
                CreatedDate = vietnamNow,
                Deleted = false
            };

            _tbDbContext.DanhSachSinhVienNhanBangs.Add(sinhvien);
            _tbDbContext.SaveChanges();
        }

        public async Task UpdateSinhVienNhanBang(UpdateSinhVienNhanBangDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateSinhVienNhanBang)}, dto= {JsonSerializer.Serialize(dto)} ");

            var subPlan = await _tbDbContext.SubPlans
                .FirstOrDefaultAsync(x => x.Id == dto.IdSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);

            var danhSachSinhViens = await _tbDbContext.DanhSachSinhVienNhanBangs
                .Where(x => x.IdSubPlan == dto.IdSubPlan && !x.Deleted)
                .OrderBy(x => x.Order)
                .ToListAsync();

            var sinhVien = danhSachSinhViens.FirstOrDefault(x => x.Id == dto.Id)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);

            if (!string.Equals(sinhVien.MaSoSinhVien, dto.MaSoSinhVien, StringComparison.OrdinalIgnoreCase))
            {
                var existingSinhVien = danhSachSinhViens
                    .FirstOrDefault(x => string.Equals(x.MaSoSinhVien, dto.MaSoSinhVien, StringComparison.OrdinalIgnoreCase));

                if (existingSinhVien != null)
                {
                    throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienDaTonTai);
                }
            }

            var currentOrder = sinhVien.Order;
            var newOrder = dto.Order;

            if (newOrder < 1 || newOrder > danhSachSinhViens.Count)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienOrderInvalid);
            }

            if (currentOrder != newOrder)
            {
                if (newOrder < currentOrder)
                {
                    foreach (var sv in danhSachSinhViens)
                    {
                        if (sv.Order >= newOrder && sv.Order < currentOrder)
                        {
                            sv.Order++;
                        }
                    }
                }
                else
                {
                    foreach (var sv in danhSachSinhViens)
                    {
                        if (sv.Order <= newOrder && sv.Order > currentOrder)
                        {
                            sv.Order--;
                        }
                    }
                }
            }

            sinhVien.HoVaTen = dto.HoVaTen;
            sinhVien.MaSoSinhVien = dto.MaSoSinhVien;
            sinhVien.Lop = dto.Lop;
            sinhVien.NgaySinh = dto.NgaySinh;
            sinhVien.CapBang = dto.CapBang;
            sinhVien.TenNganhDaoTao = dto.TenNganhDaoTao;
            sinhVien.XepHang = dto.XepHang;
            sinhVien.ThanhTich = dto.ThanhTich;
            sinhVien.Email = dto.Email;
            sinhVien.EmailSinhVien = $"{dto.MaSoSinhVien}@st.huce.edu.vn";
            sinhVien.KhoaQuanLy = dto.KhoaQuanLy;
            sinhVien.SoQuyetDinhTotNghiep = dto.SoQuyetDinhTotNghiep;
            sinhVien.NgayQuyetDinh = dto.NgayQuyetDinh;
            sinhVien.Note = dto.Note;
            sinhVien.TrangThai = dto.TrangThai;
            sinhVien.LinkQR = dto.LinkQR;
            sinhVien.IsShow = dto.IsShow;
            sinhVien.Order = newOrder;
            await _tbDbContext.SaveChangesAsync();
        }

        public void DeleteSinhVienNhanBang(int idSubPlan, int id)
        {
            _logger.LogInformation($"{nameof(DeleteSinhVienNhanBang)}, idSubPlan= {idSubPlan}, id= {id} ");
            var subPlan = _tbDbContext.SubPlans.FirstOrDefault(x => x.Id == idSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);
            var sinhVien = _tbDbContext.DanhSachSinhVienNhanBangs.FirstOrDefault(x => x.Id == id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);
            var vietnamNow = GetVietnamTime();
            sinhVien.Deleted = true;
            sinhVien.DeletedDate = vietnamNow;
            _tbDbContext.DanhSachSinhVienNhanBangs.Update(sinhVien);
            _tbDbContext.SaveChanges();
        }

        public async Task<ViewSinhVienNhanBangDto> ShowSinhVienNhanBangInfor(string mssv)
        {
            _logger.LogInformation($"{nameof(ShowSinhVienNhanBangInfor)}, mssv= {mssv} ");

            var activePlan = await _tbDbContext.Plans.AsNoTracking().Where(x => x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted).FirstOrDefaultAsync();
            if (activePlan == null)
            {
                return null;
            }

            var sinhVienSlide = await (
                               from slide in _tbDbContext.Slides.AsNoTracking()
                               join sp in _tbDbContext.SubPlans.AsNoTracking() on slide.IdSubPlan equals sp.Id
                               join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking() on slide.IdSinhVienNhanBang equals sv.Id
                               where !slide.Deleted && !sv.Deleted && !sp.Deleted && sp.IsShow
                                    && sp.IdPlan == activePlan.Id
                                    && slide.LoaiSlide == LoaiSlides.SINH_VIEN && slide.TrangThai == TraoBangConstants.ThamGiaTraoBang
                                    && sv.MaSoSinhVien.ToLower() == mssv.ToLower()
                               select new { sv, slide }
                            ).FirstOrDefaultAsync()
                            ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);


            var subPlan = await _tbDbContext.SubPlans
                .FirstOrDefaultAsync(x => x.Id == sinhVienSlide.slide.IdSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienTraoBangKhongThuocKhoaDangTrao);

            var maxOrder = await _tbDbContext.Slides.AsNoTracking()
                .Where(x => x.IdSubPlan == sinhVienSlide.slide.IdSubPlan && x.LoaiSlide == LoaiSlides.SINH_VIEN && !x.Deleted)
                .MaxAsync(x => (int?)x.Order) ?? 0;

            var minOrder = await _tbDbContext.Slides.AsNoTracking()
                .Where(x => x.IdSubPlan == sinhVienSlide.slide.IdSubPlan && x.LoaiSlide == LoaiSlides.SINH_VIEN && !x.Deleted)
                .MinAsync(x => (int?)x.Order) ?? 0;

            var totalSubPlans = await _tbDbContext.SubPlans
                .Where(x => x.IdPlan == subPlan.IdPlan && x.IsShow && x.IdPlan == activePlan.Id && !x.Deleted)
                .CountAsync();

            var result = _mapper.Map<ViewSinhVienNhanBangDto>(sinhVienSlide.sv);
            result.TrangThai = TraoBangConstants.ThamGiaTraoBang;
            result.OrderSubPlan = $"{subPlan.Order}/{totalSubPlans}";
            result.Order = $"{sinhVienSlide.slide.Order}/{maxOrder}";
            result.IsShowNext = sinhVienSlide.slide.Order < maxOrder;
            result.IsShowPrev = sinhVienSlide.slide.Order > minOrder;
            result.ThoiGianBatDau = activePlan.ThoiGianBatDau;


            var tienDo = _tbDbContext.TienDoTraoBangs.AsNoTracking().Where(x => !x.Deleted && x.IdSinhVienNhanBang == sinhVienSlide.sv.Id && x.LoaiSlide == LoaiSlides.SINH_VIEN)
                                .FirstOrDefault();
            if (tienDo != null)
            {
                if (tienDo.TrangThai == TraoBangConstants.DaTraoBang)
                {
                    result.TrangThai = tienDo.TrangThai;
                }
                else
                {
                    result.TrangThai = TraoBangConstants.ChuanBi;
                }
            }

            return result;
        }

        public async Task<ViewSinhVienNhanBangDto> NextSinhVienNhanBang(string mssv)
        {
            _logger.LogInformation($"{nameof(NextSinhVienNhanBang)}, mssv= {mssv} ");

            var activePlan = await _tbDbContext.Plans.AsNoTracking().Where(x => x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted).FirstOrDefaultAsync();
            if (activePlan == null)
            {
                return null;
            }

            var currentSinhVienSlide = await (
                   from slide in _tbDbContext.Slides.AsNoTracking()
                   join sp in _tbDbContext.SubPlans.AsNoTracking() on slide.IdSubPlan equals sp.Id
                   join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking() on slide.IdSinhVienNhanBang equals sv.Id
                   where !slide.Deleted && !sv.Deleted && !sp.Deleted && sp.IsShow
                        && sp.IdPlan == activePlan.Id
                        && slide.LoaiSlide == LoaiSlides.SINH_VIEN && slide.TrangThai != TraoBangConstants.VangMat
                        //&& slide.LoaiSlide == LoaiSlides.SINH_VIEN && slide.TrangThai == TraoBangConstants.ThamGiaTraoBang
                        && sv.MaSoSinhVien.ToLower() == mssv.ToLower()
                   select new { sv, slide }
                ).FirstOrDefaultAsync()
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);

            var nextSinhVienSlide = await (
                    from slide in _tbDbContext.Slides.AsNoTracking()
                    join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking() on slide.IdSinhVienNhanBang equals sv.Id
                    orderby slide.Order
                    where !slide.Deleted && !sv.Deleted
                        && slide.IdSubPlan == currentSinhVienSlide.slide.IdSubPlan
                        && slide.Order > currentSinhVienSlide.slide.Order
                        && slide.TrangThai != TraoBangConstants.VangMat
                        && slide.LoaiSlide == LoaiSlides.SINH_VIEN
                    select new { sv, slide }
                ).FirstOrDefaultAsync();

            if (nextSinhVienSlide == null)
            {
                return null;
            }

            var subPlan = await _tbDbContext.SubPlans
                .FirstOrDefaultAsync(x => x.Id == nextSinhVienSlide.slide.IdSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienTraoBangKhongThuocKhoaDangTrao);

            var maxOrder = await _tbDbContext.Slides
                .Where(x => x.IdSubPlan == nextSinhVienSlide.slide.IdSubPlan && !x.Deleted && x.LoaiSlide == LoaiSlides.SINH_VIEN && x.TrangThai != TraoBangConstants.VangMat)
                .MaxAsync(x => (int?)x.Order) ?? 0;

            var minOrder = await _tbDbContext.Slides
                .Where(x => x.IdSubPlan == nextSinhVienSlide.slide.IdSubPlan && !x.Deleted && x.LoaiSlide == LoaiSlides.SINH_VIEN && x.TrangThai != TraoBangConstants.VangMat)
                .MinAsync(x => (int?)x.Order) ?? 0;

            var totalSubPlans = await _tbDbContext.SubPlans
                .Where(x => x.IdPlan == subPlan.IdPlan && !x.Deleted)
                .CountAsync();

            var result = _mapper.Map<ViewSinhVienNhanBangDto>(nextSinhVienSlide.sv);
            result.OrderSubPlan = $"{subPlan.Order}/{totalSubPlans}";
            result.Order = $"{nextSinhVienSlide.slide.Order}/{maxOrder}";
            result.IsShowNext = nextSinhVienSlide.slide.Order < maxOrder;
            result.IsShowPrev = nextSinhVienSlide.slide.Order > minOrder;

            return result;
        }

        public async Task<ViewSinhVienNhanBangDto> PreviousSinhVienNhanBang(string mssv)
        {
            _logger.LogInformation($"{nameof(PreviousSinhVienNhanBang)}, mssv= {mssv} ");

            var activePlan = await _tbDbContext.Plans.AsNoTracking().Where(x => x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted).FirstOrDefaultAsync();
            if (activePlan == null)
            {
                return null;
            }

            var currentSinhVienSlide = await (
                   from slide in _tbDbContext.Slides.AsNoTracking()
                   join sp in _tbDbContext.SubPlans.AsNoTracking() on slide.IdSubPlan equals sp.Id
                   join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking() on slide.IdSinhVienNhanBang equals sv.Id
                   where !slide.Deleted && !sv.Deleted && !sp.Deleted && sp.IsShow
                        && sp.IdPlan == activePlan.Id
                        && slide.LoaiSlide == LoaiSlides.SINH_VIEN && slide.TrangThai != TraoBangConstants.VangMat
                        //&& slide.LoaiSlide == LoaiSlides.SINH_VIEN && slide.TrangThai == TraoBangConstants.ThamGiaTraoBang
                        && sv.MaSoSinhVien.ToLower() == mssv.ToLower()
                   select new { sv, slide }
                ).FirstOrDefaultAsync()
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);

            var prevSinhVienSlide = await (
                    from slide in _tbDbContext.Slides.AsNoTracking()
                    join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking() on slide.IdSinhVienNhanBang equals sv.Id
                    orderby slide.Order descending
                    where !slide.Deleted && !sv.Deleted
                        && slide.IdSubPlan == currentSinhVienSlide.slide.IdSubPlan
                        && slide.Order < currentSinhVienSlide.slide.Order
                        && slide.TrangThai != TraoBangConstants.VangMat
                        && slide.LoaiSlide == LoaiSlides.SINH_VIEN
                    select new { sv, slide }
                ).FirstOrDefaultAsync();

            if (prevSinhVienSlide == null)
            {
                return null;
            }

            var subPlan = await _tbDbContext.SubPlans
                .FirstOrDefaultAsync(x => x.Id == prevSinhVienSlide.slide.IdSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienTraoBangKhongThuocKhoaDangTrao);

            var maxOrder = await _tbDbContext.Slides
                .Where(x => x.IdSubPlan == prevSinhVienSlide.slide.IdSubPlan && !x.Deleted && x.LoaiSlide == LoaiSlides.SINH_VIEN && x.TrangThai != TraoBangConstants.VangMat)
                .MaxAsync(x => (int?)x.Order) ?? 0;

            var minOrder = await _tbDbContext.Slides
                .Where(x => x.IdSubPlan == prevSinhVienSlide.slide.IdSubPlan && !x.Deleted && x.LoaiSlide == LoaiSlides.SINH_VIEN && x.TrangThai != TraoBangConstants.VangMat)
                .MinAsync(x => (int?)x.Order) ?? 0;

            var totalSubPlans = await _tbDbContext.SubPlans
                .Where(x => x.IdPlan == subPlan.IdPlan && !x.Deleted)
                .CountAsync();

            var result = _mapper.Map<ViewSinhVienNhanBangDto>(prevSinhVienSlide.sv);
            result.OrderSubPlan = $"{subPlan.Order}/{totalSubPlans}";
            result.Order = $"{prevSinhVienSlide.slide.Order}/{maxOrder}";
            result.IsShowNext = prevSinhVienSlide.slide.Order < maxOrder;
            result.IsShowPrev = prevSinhVienSlide.slide.Order > minOrder;

            return result;
        }

        public async Task<DiemDanhNhanBangDto> DiemDanhNhanBang(string mssv)
        {
            _logger.LogInformation($"{nameof(DiemDanhNhanBang)}, mssv= {mssv} ");
            var activePlan = _tbDbContext.Plans.AsNoTracking().Where(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong).FirstOrDefault()
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var query = (from sl in _tbDbContext.Slides
                         join sv in _tbDbContext.DanhSachSinhVienNhanBangs on sl.IdSinhVienNhanBang equals sv.Id
                         join sp in _tbDbContext.SubPlans on sl.IdSubPlan equals sp.Id
                         where !sl.Deleted && !sv.Deleted && !sp.Deleted
                             && sp.IsShow && sv.MaSoSinhVien.ToLower() == mssv.ToLower()
                             && sl.TrangThai == TraoBangConstants.ThamGiaTraoBang
                             && sp.IdPlan == activePlan.Id
                             && sl.LoaiSlide == LoaiSlides.SINH_VIEN
                         select new { sl, sv, sp }).FirstOrDefault()
                           ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);

            var sinhVien = query.sv;

            var subPlan = _tbDbContext.SubPlans
               .FirstOrDefault(x => x.Id == query.sp.Id && x.TrangThai == TraoBangConstants.DangTraoBang && !x.Deleted)
               ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienTraoBangKhongThuocKhoaDangTrao);

            // sinh viên đã có trong hàng đợi thì xóa dòng cũ đi rồi đưa vào lại cuối hàng đợi
            var listTienDoCu = _tbDbContext.TienDoTraoBangs
                .Where(x => !x.Deleted && x.MaSoSinhVien.ToLower() == mssv.ToLower())
                .ToList();

            if (listTienDoCu.Count > 0)
            {
                var username = getCurrentName();
                var vietnamNow = GetVietnamTime();

                foreach (var tienDoCu in listTienDoCu)
                {
                    tienDoCu.Deleted = true;
                    tienDoCu.DeletedBy = username;
                    tienDoCu.DeletedDate = vietnamNow;
                }

                _tbDbContext.SaveChanges();
            }

            var maxOrder = _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == query.sp.Id && !x.Deleted)
                .Max(x => (int?)x.Order) ?? 0;

            var tienDoTraoBang = new TienDoTraoBang
            {
                IdSubPlan = query.sp.Id,
                IdSinhVienNhanBang = sinhVien.Id,
                HoVaTen = sinhVien.HoVaTen,
                MaSoSinhVien = sinhVien.MaSoSinhVien,
                TrangThai = TraoBangConstants.ChuanBi,
                Order = maxOrder + 1,
                IsShow = true,
                CreatedDate = DateTime.Now,
                IdSlide = query.sl.Id,
                LoaiSlide = query.sl.LoaiSlide,
                Note = query.sv.Note,
                IdPlan = query.sp.IdPlan,
                Deleted = false
            };

            _tbDbContext.TienDoTraoBangs.Add(tienDoTraoBang);
            _tbDbContext.SaveChanges();

            await _traoBangService.NotifyCheckIn();

            return new DiemDanhNhanBangDto
            {
                TenKhoa = subPlan?.Ten ?? String.Empty,
                Id = sinhVien.Id,
                HoVaTen = sinhVien.HoVaTen,
                MaSoSinhVien = sinhVien.MaSoSinhVien,
                TrangThai = TraoBangConstants.ChuanBi,
                Order = maxOrder + 1,
                IsShow = true
            };
        }
        public int? CheckSinhVienTrongHangDoi(string mssv)
        {
            _logger.LogInformation($"{nameof(CheckSinhVienTrongHangDoi)}, mssv= {mssv} ");

            var tienDo = _tbDbContext.TienDoTraoBangs.AsNoTracking()
                .Where(x => !x.Deleted && x.MaSoSinhVien.ToLower() == mssv.ToLower())
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            return tienDo?.TrangThai;
        }

        public async Task<DiemDanhNhanBangDto> DiemDanhNhanBangTruongHopDacBiet(string mssv)
        {
            _logger.LogInformation($"{nameof(DiemDanhNhanBangTruongHopDacBiet)}, mssv= {mssv} ");

            var qr = (from sp in _tbDbContext.SubPlans.AsNoTracking().Where(x => !x.Deleted)
                      join sl in _tbDbContext.Slides.AsNoTracking().Where(x => !x.Deleted) on sp.Id equals sl.IdSubPlan
                      join p in _tbDbContext.Plans.AsNoTracking().Where(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong) on sp.IdPlan equals p.Id
                      join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => !x.Deleted) on sl.IdSinhVienNhanBang equals sv.Id
                      where sv.MaSoSinhVien.ToLower() == mssv.ToLower()
                      select new { sv, sl, sp }).FirstOrDefault();

            var sinhVien = qr?.sv;
            var slide = qr?.sl;

            if (qr == null || slide == null || sinhVien == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);
            }

            var subPlan = _tbDbContext.SubPlans
               .FirstOrDefault(x => x.Id == qr.sl.IdSubPlan && x.TrangThai == TrangThaiSubPlan.DangTraoBang && !x.Deleted)
               ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienTraoBangKhongThuocKhoaDangTrao);

            // sinh viên đã có trong hàng đợi thì xóa dòng cũ đi rồi đưa vào lại cuối hàng đợi
            var listTienDoCu = _tbDbContext.TienDoTraoBangs
                .Where(x => !x.Deleted && x.MaSoSinhVien.ToLower() == mssv.ToLower())
                .ToList();

            if (listTienDoCu.Count > 0)
            {
                var username = getCurrentName();
                var vietnamNow = GetVietnamTime();

                foreach (var tienDoCu in listTienDoCu)
                {
                    tienDoCu.Deleted = true;
                    tienDoCu.DeletedBy = username;
                    tienDoCu.DeletedDate = vietnamNow;
                }

                _tbDbContext.SaveChanges();
            }

            var maxOrder = _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == slide.IdSubPlan && !x.Deleted)
                .Max(x => (int?)x.Order) ?? 0;

            var tienDoTraoBang = new TienDoTraoBang
            {
                IdSubPlan = slide.IdSubPlan,
                IdSinhVienNhanBang = sinhVien.Id,
                HoVaTen = sinhVien.HoVaTen,
                MaSoSinhVien = sinhVien.MaSoSinhVien,
                TrangThai = TraoBangConstants.ChuanBi,
                Order = maxOrder + 1,
                IsShow = true,
                IdPlan = subPlan.IdPlan,
                IdSlide = slide.Id,
                LoaiSlide = slide.LoaiSlide,
                Note = slide.Note,
                CreatedDate = DateTime.Now,
                Deleted = false,
            };
            _tbDbContext.TienDoTraoBangs.Add(tienDoTraoBang);
            _tbDbContext.SaveChanges();

            await _traoBangService.NotifyCheckIn();

            return new DiemDanhNhanBangDto
            {
                TenKhoa = subPlan?.Ten ?? String.Empty,
                Id = sinhVien.Id,
                HoVaTen = sinhVien.HoVaTen,
                MaSoSinhVien = sinhVien.MaSoSinhVien,
                TrangThai = TraoBangConstants.ChuanBi,
                Order = maxOrder + 1,
                IsShow = true
            };
        }

        public async Task UpdateTrangThaiSinhVienNhanBang(int idSubPlan, int id)
        {
            _logger.LogInformation($"{nameof(UpdateTrangThaiSinhVienNhanBang)} ");
            var subPlan = _tbDbContext.SubPlans.FirstOrDefault(x => x.Id == idSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);
            var sinhVien = _tbDbContext.TienDoTraoBangs.FirstOrDefault(x => x.Id == id && x.IdSubPlan == idSubPlan && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);
            sinhVien.TrangThai = TraoBangConstants.DangTraoBang;
            _tbDbContext.TienDoTraoBangs.Update(sinhVien);
            _tbDbContext.SaveChanges();
            await _traoBangService.NotifySinhVienDangTrao();
            _logger.LogInformation($"Đã bắn SignalR cho sinh viên Id: {id}, SubPlan: {idSubPlan}");
        }

        public async Task<GetSinhVienDangTraoBangInforDto> GetSinhVienDangTraoBang()
        {
            _logger.LogInformation($"{nameof(GetSinhVienDangTraoBang)} ");

            var subPlan = await (
                    from sp in _tbDbContext.SubPlans.AsNoTracking().Where(x => !x.Deleted && x.IsShow)
                    join p in _tbDbContext.Plans.AsNoTracking().Where(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong) on sp.IdPlan equals p.Id
                    where sp.TrangThai == TrangThaiSubPlan.DangTraoBang
                    select sp
                ).FirstOrDefaultAsync();

            if (subPlan == null)
            {
                return null;
            }

            //var coSinhVienDaTraoHoacDangTrao = await _tbDbContext.TienDoTraoBangs
            //    .AsNoTracking()
            //    .AnyAsync(x => x.IdSubPlan == subPlan.Id
            //                && (x.TrangThai == TraoBangConstants.DaTraoBang || x.TrangThai == TraoBangConstants.DangTraoBang)
            //                && !x.Deleted);

            //if (!coSinhVienDaTraoHoacDangTrao && subPlan.IsShowMoBai)
            //{
            //    var slideMoBai = _tbDbContext.Slides.AsNoTracking().OrderBy(x => x.Id)
            //                            .FirstOrDefault(x => !x.Deleted && x.IdSubPlan == subPlan.Id && x.IsShow && x.LoaiSlide == LoaiSlides.BINH_THUONG);

            //    string text = "";
            //    string textNote = "";

            //    if (slideMoBai != null)
            //    {
            //        text = slideMoBai.NoiDung ?? "";
            //        textNote = slideMoBai.Note ?? "";
            //    }

            //    return new GetSinhVienDangTraoBangInforDto
            //    {
            //        TenSubPlan = subPlan.Ten,
            //        Id = 0,
            //        HoVaTen = string.Empty,
            //        MaSoSinhVien = string.Empty,
            //        TenNganhDaoTao = string.Empty,
            //        XepHang = string.Empty,
            //        ThanhTich = string.Empty,
            //        CapBang = string.Empty,
            //        Note = string.Empty,
            //        Text = text,
            //        TextNote = textNote,
            //        InfoType = ViewSvTypeConstants.MO_BAI,
            //    };
            //}

            var tienDo = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdSubPlan == subPlan.Id
                                        && x.TrangThai == TraoBangConstants.DangTraoBang
                                        && !x.Deleted);


            if (tienDo != null)
            {
                if (tienDo.LoaiSlide == LoaiSlides.SINH_VIEN)
                {
                    var sinhVien = await (
                                    from sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => !x.Deleted)
                                    join sl in _tbDbContext.Slides.AsNoTracking().Where(x => !x.Deleted) on sv.Id equals sl.IdSinhVienNhanBang
                                    where sv.Id == tienDo.IdSinhVienNhanBang && sl.IdSubPlan == subPlan.Id
                                    select sv
                                ).FirstOrDefaultAsync();

                    return new GetSinhVienDangTraoBangInforDto
                    {
                        TenSubPlan = subPlan.Ten,
                        Id = sinhVien!.Id,
                        HoVaTen = sinhVien.HoVaTen,
                        MaSoSinhVien = sinhVien.MaSoSinhVien,
                        TenNganhDaoTao = sinhVien.TenNganhDaoTao,
                        XepHang = sinhVien.XepHang,
                        ThanhTich = sinhVien.ThanhTich,
                        CapBang = sinhVien.CapBang,
                        Note = sinhVien.Note ?? string.Empty,
                        Text = null,
                        TextNote = null,
                        InfoType = ViewSvTypeConstants.SV,
                    };
                }
                else
                {
                    var slide = _tbDbContext.Slides.FirstOrDefault(x => x.Id == tienDo.IdSlide && !x.Deleted && x.IsShow);

                    if (slide == null)
                    {
                        return null;
                    }

                    return new GetSinhVienDangTraoBangInforDto
                    {
                        TenSubPlan = subPlan.Ten,
                        Id = tienDo.Id,
                        HoVaTen = tienDo.HoVaTen,
                        MaSoSinhVien = tienDo.MaSoSinhVien,
                        //TenNganhDaoTao = tienDo.TenNganhDaoTao,
                        //XepHang = sinhVien.XepHang,
                        //ThanhTich = sinhVien.ThanhTich,
                        //CapBang = sinhVien.CapBang,
                        Note = tienDo.Note ?? string.Empty,
                        Text = slide.NoiDung,
                        TextNote = slide.Note,
                        InfoType = ViewSvTypeConstants.MO_BAI,
                    };
                }
            }


            //var coSinhVienChuanBi = await _tbDbContext.TienDoTraoBangs
            //    .AsNoTracking()
            //    .AnyAsync(x => x.IdSubPlan == subPlan.Id
            //                && x.TrangThai == TraoBangConstants.ChuanBi
            //                && !x.Deleted);

            //if (!coSinhVienChuanBi && subPlan.IsShowKetBai)
            //{
            //    var slideMoBai = _tbDbContext.Slides.AsNoTracking().OrderByDescending(x => x.Id)
            //                            .FirstOrDefault(x => !x.Deleted && x.IdSubPlan == subPlan.Id && x.IsShow && x.LoaiSlide == LoaiSlides.BINH_THUONG);

            //    string text = "";
            //    string textNote = "";

            //    if (slideMoBai != null)
            //    {
            //        text = slideMoBai.NoiDung ?? "";
            //        textNote = slideMoBai.Note ?? "";
            //    }

            //    return new GetSinhVienDangTraoBangInforDto
            //    {
            //        TenSubPlan = subPlan.Ten,
            //        Id = 0,
            //        HoVaTen = string.Empty,
            //        MaSoSinhVien = string.Empty,
            //        TenNganhDaoTao = string.Empty,
            //        XepHang = string.Empty,
            //        ThanhTich = string.Empty,
            //        CapBang = string.Empty,
            //        Note = string.Empty,
            //        Text = text,
            //        TextNote = textNote,
            //        InfoType = ViewSvTypeConstants.KET_BAI,
            //    };
            //}

            return null;
        }
        public async Task UpdateTrangThaiSubPlan(int id)
        {
            _logger.LogInformation($"{nameof(UpdateTrangThaiSubPlan)}");

            var subPlan = _tbDbContext.SubPlans.FirstOrDefault(x => x.Id == id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);

            var currentSubPlans = _tbDbContext.SubPlans
                .Where(x => x.TrangThai == TraoBangConstants.DangTraoBang && x.Id != id && !x.Deleted)
                .ToList();

            foreach (var current in currentSubPlans)
            {
                current.TrangThai = TraoBangConstants.ChuanBi;
                _tbDbContext.SubPlans.Update(current);
            }

            subPlan.TrangThai = TraoBangConstants.DangTraoBang;
            _tbDbContext.SubPlans.Update(subPlan);

            _tbDbContext.SaveChanges();
            await _traoBangService.NotifyChonKhoa();
        }
        public async Task<List<ViewTienDoNhanBangResponseDto>> GetTienDoNhanBang(ViewTienDoNhanBangRequestDto dto)
        {
            _logger.LogInformation($"{nameof(GetTienDoNhanBang)}, dto= {JsonSerializer.Serialize(dto)} ");

            var khoaDangTrao = (
                                from plan in _tbDbContext.Plans.AsNoTracking().Where(x => x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted)
                                join subplan in _tbDbContext.SubPlans.AsNoTracking().Where(x => !x.Deleted && x.IsShow) on plan.Id equals subplan.IdPlan
                                where subplan.TrangThai == TraoBangConstants.DangTraoBang
                                select subplan
                               ).FirstOrDefault();

            if (khoaDangTrao == null)
            {
                return null;
            }

            //var sinhVien = _tbDbContext.DanhSachSinhVienNhanBangs
            //    .AsNoTracking()
            //    .FirstOrDefault(x => !x.Deleted && x.IdSubPlan == khoaDangTrao.Id);

            var sinhVien = (from sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => !x.Deleted)
                            join sl in _tbDbContext.Slides.AsNoTracking().Where(x => !x.Deleted && x.IsShow) on sv.Id equals sl.IdSinhVienNhanBang
                            where sl.IdSubPlan == khoaDangTrao.Id
                            select sv)
                           .FirstOrDefault();

            if (sinhVien == null)
            {
                return null;
            }

            var results = new List<TienDoTraoBang>();

            var sinhVienDaTrao = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .Where(x => !x.Deleted
                            && x.IdSubPlan == khoaDangTrao.Id
                            && x.TrangThai == TraoBangConstants.DaTraoBang)
                .OrderByDescending(x => x.Order)
                .FirstOrDefaultAsync();

            var soLuongConLai = dto.SoLuong - results.Count;
            if (soLuongConLai > 0)
            {
                var sinhVienChuanBi = (from td in _tbDbContext.TienDoTraoBangs.AsNoTracking()
                                       from sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => x.Id == td.IdSinhVienNhanBang && !x.Deleted).DefaultIfEmpty()
                                       orderby td.Order
                                       where !td.Deleted
                                         && td.IdSubPlan == khoaDangTrao.Id
                                         && td.TrangThai != TraoBangConstants.DaTraoBang
                                       select new TienDoTraoBang
                                       {
                                           Id = td.Id,
                                           HoVaTen = sv != null ? sv.QrHoTen : td.HoVaTen,
                                           MaSoSinhVien = td.MaSoSinhVien,
                                           TrangThai = td.TrangThai,
                                           Order = td.Order,
                                           IsShow = td.IsShow,
                                           LoaiSlide = td.LoaiSlide,
                                           IdSlide = td.IdSlide,
                                           // gán tạm tên ngành đào tạo
                                           Note = sv != null ? sv.TenNganhDaoTao : string.Empty
                                       }).ToList();

                results.AddRange(sinhVienChuanBi);
            }

            if (!results.Any())
            {
                return null;
            }

            var slideDau = _tbDbContext.Slides.AsNoTracking()
                                    .Where(x => !x.Deleted && x.IdSubPlan == khoaDangTrao.Id && x.LoaiSlide == LoaiSlides.TEXT)
                                    .OrderBy(x => x.Order).FirstOrDefault()?.Id;

            var slideCuoi = _tbDbContext.Slides.AsNoTracking()
                                .Where(x => !x.Deleted && x.IdSubPlan == khoaDangTrao.Id && x.LoaiSlide == LoaiSlides.TEXT)
                                .OrderByDescending(x => x.Order).FirstOrDefault()?.Id;

            return results.Select(result => new ViewTienDoNhanBangResponseDto
            {
                Id = result.Id,
                HoVaTen = result.HoVaTen,
                MaSoSinhVien = result.MaSoSinhVien,
                CapBang = sinhVien.CapBang,
                TenNganhDaoTao = result.Note,
                TrangThai = result.TrangThai,
                Note = sinhVien.Note ?? "",
                Order = result.Order,
                IsShow = result.IsShow,
                LoaiSlide = result.LoaiSlide,
                IsSlideDauCuoi = result.IdSlide == slideDau || result.IdSlide == slideCuoi,
                IdSlide = result.IdSlide,
            }).ToList();
        }
        public async Task<GetNextSubPlanResponseDto?> NextSubPlan()
        {
            _logger.LogInformation($"{nameof(NextSubPlan)}");

            var activePlan = _tbDbContext.Plans.FirstOrDefault(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var currentSubPlan = _tbDbContext.SubPlans
                .FirstOrDefault(x => x.TrangThai == TrangThaiSubPlan.DangTraoBang && activePlan.Id == x.IdPlan && !x.Deleted);

            if (currentSubPlan != null)
            {
                currentSubPlan.TrangThai = TrangThaiSubPlan.DaTraoBang;
                _tbDbContext.SubPlans.Update(currentSubPlan);
            }

            var nextSubPlan = _tbDbContext.SubPlans
                .Where(x => x.TrangThai == TrangThaiSubPlan.ChuanBi && !x.Deleted && x.IdPlan == activePlan.Id)
                .OrderBy(x => x.Order)
                .FirstOrDefault();

            if (nextSubPlan == null)
            {
                _tbDbContext.SaveChanges();
                return null;
            }

            using (var tran = _tbDbContext.Database.BeginTransaction())
            {
                nextSubPlan.TrangThai = TrangThaiSubPlan.DangTraoBang;
                _tbDbContext.SubPlans.Update(nextSubPlan);

                #region lay slide dau tien
                var firstSlide = _tbDbContext.Slides.FirstOrDefault(x => !x.Deleted && x.IdSubPlan == nextSubPlan.Id && x.IsShow && x.LoaiSlide == LoaiSlides.TEXT);
                if (firstSlide != null)
                {
                    var isFirstSlideOnQueue = _tbDbContext.TienDoTraoBangs.Any(x => !x.Deleted && x.IdSlide == firstSlide.Id);
                    if (!isFirstSlideOnQueue)
                    {
                        var newTienDo = new TienDoTraoBang()
                        {
                            IdPlan = nextSubPlan.IdPlan,
                            IdSubPlan = nextSubPlan.Id,
                            IdSlide = firstSlide.Id,
                            HoVaTen = firstSlide.NoiDung!,
                            IdSinhVienNhanBang = firstSlide.IdSinhVienNhanBang ?? -1,
                            LoaiSlide = firstSlide.LoaiSlide,
                            TrackingDangTrao = true,
                            Order = 1,
                            TrangThai = TraoBangConstants.ChuanBi,
                        };
                        _tbDbContext.TienDoTraoBangs.Add(newTienDo);
                    }
                }
                #endregion

                _tbDbContext.SaveChanges();

                #region cho dong dau hang doi dang trao neu chua co dong nao dang trao
                var dangTraoBang = _tbDbContext.TienDoTraoBangs
                    .Any(x => !x.Deleted && x.IdSubPlan == nextSubPlan.Id && x.TrangThai == TraoBangConstants.DangTraoBang);

                if (!dangTraoBang)
                {
                    var tienDoDauTien = _tbDbContext.TienDoTraoBangs
                        .Where(x => !x.Deleted && x.IdSubPlan == nextSubPlan.Id)
                        .OrderBy(x => x.Order)
                        .FirstOrDefault();

                    if (tienDoDauTien != null)
                    {
                        tienDoDauTien.TrangThai = TraoBangConstants.DangTraoBang;
                        _tbDbContext.TienDoTraoBangs.Update(tienDoDauTien);
                        _tbDbContext.SaveChanges();
                    }
                }
                #endregion

                tran.Commit();
            }
            await _traoBangService.NotifyChonKhoa();

            return new GetNextSubPlanResponseDto
            {
                Id = nextSubPlan.Id,
                TenSubPlan = nextSubPlan.Ten,
                TruongKhoa = nextSubPlan.TruongKhoa,
                Order = nextSubPlan.Order,
                TrangThai = nextSubPlan.TrangThai
            };
        }
        public async Task<List<GetListSubPlanDto>> GetListSubPlanInfor()
        {
            _logger.LogInformation($"{nameof(GetListSubPlanInfor)}");

            var subPlans = await (
                                    from sp in _tbDbContext.SubPlans.AsNoTracking().Where(x => !x.Deleted && x.IsShow)
                                    join p in _tbDbContext.Plans.AsNoTracking().Where(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong) on sp.IdPlan equals p.Id
                                    orderby sp.Order
                                    select sp
                                ).ToListAsync();

            var items = new List<GetListSubPlanDto>();

            foreach (var subPlan in subPlans)
            {
                var daTrao = await _tbDbContext.TienDoTraoBangs
                    .AsNoTracking()
                    .CountAsync(x => x.IdSubPlan == subPlan.Id && !x.Deleted && x.TrangThai == TraoBangConstants.DaTraoBang && x.LoaiSlide == LoaiSlides.SINH_VIEN);

                var tongSo = await _tbDbContext.Slides
                    .AsNoTracking()
                    .CountAsync(x => !x.Deleted && x.IdSubPlan == subPlan.Id && x.TrangThai == TraoBangConstants.ThamGiaTraoBang && x.LoaiSlide == LoaiSlides.SINH_VIEN);

                var tienDo = tongSo > 0 ? $"{daTrao}/{tongSo}" : "0/0";

                items.Add(new GetListSubPlanDto
                {
                    Id = subPlan.Id,
                    Ten = subPlan.Ten,
                    TrangThai = subPlan.TrangThai,
                    Order = subPlan.Order,
                    TienDo = tienDo
                });
            }

            return items;
        }
        public async Task<GetInforSubPlanDto> GetInforSubPlan(int idSubPlan)
        {
            _logger.LogInformation($"{nameof(GetInforSubPlan)}, idSubPlan= {idSubPlan} ");

            var plan = await _tbDbContext.Plans.Where(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong).FirstOrDefaultAsync();
            if (plan == null)
            {
                return null;
            }

            var subPlan = await _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == idSubPlan && x.IdPlan == plan.Id && !x.Deleted);
            if (subPlan == null)
            {
                return null;
            }

            var soLuongThamGia = (
                                    from slide in _tbDbContext.Slides.AsNoTracking()
                                    join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking() on slide.IdSinhVienNhanBang equals sv.Id
                                    where slide.IdSubPlan == subPlan.Id && !slide.Deleted && !sv.Deleted && slide.IsShow
                                        && slide.LoaiSlide == LoaiSlides.SINH_VIEN
                                        && slide.TrangThai == TraoBangConstants.ThamGiaTraoBang
                                    select sv.Id
                                    ).Count();
            var soLuongVangMat = (
                                    from slide in _tbDbContext.Slides.AsNoTracking()
                                    join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking() on slide.IdSinhVienNhanBang equals sv.Id
                                    where slide.IdSubPlan == subPlan.Id && !slide.Deleted && !sv.Deleted && slide.IsShow
                                        && slide.LoaiSlide == LoaiSlides.SINH_VIEN
                                        && slide.TrangThai == TraoBangConstants.VangMat
                                    select sv.Id
                                    ).Count();

            var soLuongDaTrao = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .CountAsync(x => x.IdSubPlan == idSubPlan && !x.Deleted && x.TrangThai == TraoBangConstants.DaTraoBang && x.LoaiSlide == LoaiSlides.SINH_VIEN);

            // Lấy slide text ko ở trong hàng đợi, ko lấy slide đầu và cuối
            var listSlideTrongHangDoi = _tbDbContext.TienDoTraoBangs.Where(x => !x.Deleted && x.LoaiSlide == LoaiSlides.TEXT).Select(x => x.IdSlide).ToList();
            var slideDau = _tbDbContext.Slides.AsNoTracking()
                                    .Where(x => !x.Deleted && x.IdSubPlan == idSubPlan && x.LoaiSlide == LoaiSlides.TEXT)
                                    .OrderBy(x => x.Order).FirstOrDefault()?.Id;

            var slideCuoi = _tbDbContext.Slides.AsNoTracking()
                                .Where(x => !x.Deleted && x.IdSubPlan == idSubPlan && x.LoaiSlide == LoaiSlides.TEXT)
                                .OrderByDescending(x => x.Order).FirstOrDefault()?.Id;
            var listSlide = _tbDbContext.Slides
                                    .Where(x => !x.Deleted && x.IsShow && x.IdSubPlan == idSubPlan
                                                && x.Id != slideDau && x.Id != slideCuoi
                                                && x.LoaiSlide == LoaiSlides.TEXT && !listSlideTrongHangDoi.Contains(x.Id))
                                    .OrderBy(x => x.Order)
                                    .ToList();

            return new GetInforSubPlanDto
            {
                Ten = subPlan.Ten,
                IdPlan = plan.Id,
                IdSubPlan = subPlan.Id,
                SoLuongThamGia = soLuongThamGia,
                SoLuongVangMat = soLuongVangMat,
                SoLuongDaTrao = soLuongDaTrao,
                SoLuongConLai = soLuongThamGia - soLuongDaTrao,
                SlideTexts = _mapper.Map<List<ViewSlideDto>>(listSlide)
            };

        }
        public async Task<GetTienDoTraoBangResponseDto> GetTienDoTraoBang()
        {
            var activePlan = _tbDbContext.Plans.AsNoTracking().Where(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong).FirstOrDefault();
            if (activePlan == null)
            {
                return null;
            }

            var sinhVienDaTrao = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .CountAsync(x => x.TrangThai == TraoBangConstants.DaTraoBang && x.LoaiSlide == LoaiSlides.SINH_VIEN && !x.Deleted);

            var tongSinhVienThamGiaTraoBang = (
                                                from plan in _tbDbContext.Plans.AsNoTracking()
                                                join sp in _tbDbContext.SubPlans.AsNoTracking() on plan.Id equals sp.IdPlan
                                                join slide in _tbDbContext.Slides.AsNoTracking() on sp.Id equals slide.IdSubPlan
                                                join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => !x.Deleted) on slide.IdSinhVienNhanBang equals sv.Id
                                                where slide.TrangThai == TraoBangConstants.ChuanBi && slide.IsShow
                                                     && !plan.Deleted && !sp.Deleted && !slide.Deleted
                                                     && sp.IsShow && plan.TrangThai == TrangThaiPlan.DangHoatDong
                                                select sv.Id
                                               ).Count();

            var tienDo = tongSinhVienThamGiaTraoBang > 0 ? (double)sinhVienDaTrao / tongSinhVienThamGiaTraoBang * 100 : 0;
            return new GetTienDoTraoBangResponseDto
            {
                TienDo = $"{sinhVienDaTrao}/{tongSinhVienThamGiaTraoBang} sinh viên ({tienDo:F2}%)"
            };
        }
        //next sinh viên đang trao
        public async Task<GetSinhVienDangTraoBangInforDto> NextSinhVienTraoBang(int idSubPlan)
        {
            _logger.LogInformation($"{nameof(NextSinhVienTraoBang)} idSubPlan={idSubPlan}");

            var sinhVienDangTrao = await _tbDbContext.TienDoTraoBangs
                .FirstOrDefaultAsync(x => x.IdSubPlan == idSubPlan
                            && x.TrangThai == TraoBangConstants.DangTraoBang
                            && !x.Deleted);

            if (sinhVienDangTrao == null)
            {
                var sinhVienDauTien = await _tbDbContext.TienDoTraoBangs
                    .Where(x => x.IdSubPlan == idSubPlan
                                && x.TrangThai == TraoBangConstants.ChuanBi
                                && !x.Deleted)
                    .OrderBy(x => x.Order)
                    .FirstOrDefaultAsync();

                if (sinhVienDauTien == null)
                {
                    var subPlanForKetBai = await _tbDbContext.SubPlans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

                    if (subPlanForKetBai != null && subPlanForKetBai.IsShowKetBai)
                    {
                        await _traoBangService.NotifySinhVienDangTrao();

                        var tienDoSlide = _tbDbContext.TienDoTraoBangs
                                                .Where(x => !x.Deleted && x.LoaiSlide == LoaiSlides.TEXT && x.IdSubPlan == subPlanForKetBai.Id)
                                                .Select(x => x.IdSlide).ToList();

                        var finalSlide = _tbDbContext.Slides
                                            .Where(x => !x.Deleted && x.LoaiSlide == LoaiSlides.TEXT && x.IsShow && x.IdSubPlan == subPlanForKetBai.Id && !tienDoSlide.Contains(x.Id))
                                            .OrderByDescending(x => x.Order)
                                            .FirstOrDefault();

                        if (finalSlide != null)
                        {
                            var maxOrder = _tbDbContext.TienDoTraoBangs
                                                .Where(x => x.IdSubPlan == subPlanForKetBai.Id && !x.Deleted)
                                                .Max(x => (int?)x.Order) ?? 0;

                            _tbDbContext.TienDoTraoBangs.Add(new TienDoTraoBang
                            {
                                IdSubPlan = finalSlide.IdSubPlan,
                                IdSlide = finalSlide.Id,
                                HoVaTen = finalSlide.NoiDung,
                                TrangThai = TraoBangConstants.DangTraoBang,
                                TrackingDangTrao = true,
                                IsShow = finalSlide.IsShow,
                                IdPlan = subPlanForKetBai.IdPlan,
                                LoaiSlide = LoaiSlides.TEXT,
                                Note = finalSlide.Note,
                                Order = maxOrder + 1,
                                IdSinhVienNhanBang = -1
                            });
                            _tbDbContext.SaveChanges();

                            return new GetSinhVienDangTraoBangInforDto
                            {
                                TenSubPlan = subPlanForKetBai.Ten ?? string.Empty,
                                Text = finalSlide.NoiDung,
                                TextNote = finalSlide.Note
                            };
                        }

                        return new GetSinhVienDangTraoBangInforDto
                        {
                            TenSubPlan = subPlanForKetBai.Ten ?? string.Empty,
                            Text = subPlanForKetBai.KetBai ?? string.Empty,
                            TextNote = subPlanForKetBai.KetBaiNote ?? string.Empty
                        };
                    }

                    return null;
                }

                sinhVienDauTien.TrangThai = TraoBangConstants.DangTraoBang;
                sinhVienDauTien.TrackingDangTrao = true;
                _tbDbContext.TienDoTraoBangs.Update(sinhVienDauTien);
                await _tbDbContext.SaveChangesAsync();

                var sinhVienInfo = await _tbDbContext.DanhSachSinhVienNhanBangs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == sinhVienDauTien.IdSinhVienNhanBang && !x.Deleted);

                var subPlan = await _tbDbContext.SubPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

                await _traoBangService.NotifySinhVienDangTrao();

                return new GetSinhVienDangTraoBangInforDto
                {
                    TenSubPlan = subPlan?.Ten ?? string.Empty,
                    Id = sinhVienDauTien.Id,
                    HoVaTen = sinhVienInfo?.HoVaTen ?? string.Empty,
                    MaSoSinhVien = sinhVienInfo?.MaSoSinhVien ?? string.Empty,
                    TenNganhDaoTao = sinhVienInfo?.TenNganhDaoTao ?? string.Empty,
                    XepHang = sinhVienInfo?.XepHang ?? string.Empty,
                    ThanhTich = sinhVienInfo?.ThanhTich ?? string.Empty,
                    CapBang = sinhVienInfo?.CapBang ?? string.Empty,
                    Note = sinhVienInfo?.Note ?? string.Empty
                };
            }

            sinhVienDangTrao.TrangThai = TraoBangConstants.DaTraoBang;
            _tbDbContext.TienDoTraoBangs.Update(sinhVienDangTrao);
            await _tbDbContext.SaveChangesAsync();

            var sinhVienTiepTheo = await _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == idSubPlan
                            && x.TrangThai == TraoBangConstants.ChuanBi
                            && !x.Deleted)
                .OrderBy(x => x.Order)
                .FirstOrDefaultAsync();

            if (sinhVienTiepTheo == null)
            {
                var subPlanForKetBai = await _tbDbContext.SubPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

                if (subPlanForKetBai != null && subPlanForKetBai.IsShowKetBai)
                {
                    await _traoBangService.NotifySinhVienDangTrao();
                    return new GetSinhVienDangTraoBangInforDto
                    {
                        TenSubPlan = subPlanForKetBai.Ten ?? string.Empty,
                        Text = subPlanForKetBai.KetBai ?? string.Empty,
                        TextNote = subPlanForKetBai.KetBaiNote ?? string.Empty
                    };
                }

                return null;
            }
            var sinhVienDangTraoInfo = await _tbDbContext.DanhSachSinhVienNhanBangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sinhVienDangTrao.IdSinhVienNhanBang && !x.Deleted);

            var subPlanDangTrao = await _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);
            var vietnamNow = GetVietnamTime();
            var existingLog = await _tbDbContext.TraoBangLogs
                .FirstOrDefaultAsync(x => x.IdSinhVien == sinhVienDangTrao.IdSinhVienNhanBang
                                          && x.IdSubPlan == idSubPlan
                                          && !x.Deleted);
            if (existingLog == null)
            {
                var traoBangLog = new TraoBangLog
                {
                    IdSubPlan = idSubPlan,
                    IdSinhVien = sinhVienDangTrao.IdSinhVienNhanBang,
                    NoiDung = $"Sinh viên {sinhVienDangTraoInfo?.MaSoSinhVien ?? string.Empty}, {sinhVienDangTraoInfo?.HoVaTen ?? string.Empty}, Khoa {subPlanDangTrao?.Ten ?? string.Empty} đã được trao bằng thành công!",
                    CreatedDate = vietnamNow,
                    Deleted = false
                };

                _tbDbContext.TraoBangLogs.Add(traoBangLog);
                await _tbDbContext.SaveChangesAsync();
            }
            sinhVienTiepTheo.TrangThai = TraoBangConstants.DangTraoBang;
            sinhVienTiepTheo.TrackingDangTrao = true;
            _tbDbContext.TienDoTraoBangs.Update(sinhVienTiepTheo);
            await _tbDbContext.SaveChangesAsync();

            var sinhVienInfoTiepTheo = await _tbDbContext.DanhSachSinhVienNhanBangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sinhVienTiepTheo.IdSinhVienNhanBang && !x.Deleted);

            var subPlanInfo = await _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

            await _traoBangService.NotifySinhVienDangTrao();

            return new GetSinhVienDangTraoBangInforDto
            {
                TenSubPlan = subPlanInfo?.Ten ?? string.Empty,
                Id = sinhVienTiepTheo.Id,
                HoVaTen = sinhVienInfoTiepTheo?.HoVaTen ?? string.Empty,
                MaSoSinhVien = sinhVienInfoTiepTheo?.MaSoSinhVien ?? string.Empty,
                TenNganhDaoTao = sinhVienInfoTiepTheo?.TenNganhDaoTao ?? string.Empty,
                XepHang = sinhVienInfoTiepTheo?.XepHang ?? string.Empty,
                ThanhTich = sinhVienInfoTiepTheo?.ThanhTich ?? string.Empty,
                CapBang = sinhVienInfoTiepTheo?.CapBang ?? string.Empty,
                Note = sinhVienInfoTiepTheo?.Note ?? string.Empty
            };
        }

        public async Task<DiemDanhNhanBangDto> CutSlideNormal(int idSubPlan)
        {
            _logger.LogInformation($"{nameof(CutSlideNormal)} idSubPlan={idSubPlan}");

            var userid = getCurrentUserId();

            var query = (from p in _tbDbContext.Plans.Where(x => !x.Deleted)
                         join sp in _tbDbContext.SubPlans.Where(x => !x.Deleted) on p.Id equals sp.IdPlan
                         where p.TrangThai == TrangThaiPlan.DangHoatDong &&
                                 idSubPlan == sp.Id
                         select new { p, sp }).FirstOrDefault();

            if (query != null)
            {
                var plan = query.p;
                var subPlan = query.sp;

                var slideDau = _tbDbContext.Slides.AsNoTracking()
                                    .Where(x => !x.Deleted && x.IdSubPlan == idSubPlan && x.LoaiSlide == LoaiSlides.TEXT)
                                    .OrderBy(x => x.Order).FirstOrDefault()?.Id;

                var slideCuoi = _tbDbContext.Slides.AsNoTracking()
                                    .Where(x => !x.Deleted && x.IdSubPlan == idSubPlan && x.LoaiSlide == LoaiSlides.TEXT)
                                    .OrderByDescending(x => x.Order).FirstOrDefault()?.Id;

                var slideDaChay = _tbDbContext.TienDoTraoBangs
                                        .Where(x => !x.Deleted && x.IdSubPlan == idSubPlan
                                                    && x.LoaiSlide == LoaiSlides.TEXT)
                                        .Select(x => x.IdSlide)
                                        .ToList();

                var maxOrder = _tbDbContext.TienDoTraoBangs
                                        .Where(x => !x.Deleted && x.IdSubPlan == idSubPlan)
                                        .Select(x => (int?)x.Order)
                                        .Max() ?? 0;

                var slideIncomming = _tbDbContext.Slides.Where(x => !x.Deleted && x.IdSubPlan == idSubPlan
                                                    && !slideDaChay.Contains(x.Id)
                                                    && x.Id != slideDau && x.Id != slideCuoi
                                                    && x.LoaiSlide == LoaiSlides.TEXT && x.IsShow)
                                                .OrderBy(x => x.Order)
                                                .FirstOrDefault();

                if (slideIncomming != null)
                {
                    var tienDo = new TienDoTraoBang
                    {
                        IdSubPlan = slideIncomming.IdSubPlan,
                        IdSinhVienNhanBang = slideIncomming.IdSinhVienNhanBang ?? -1,
                        TrangThai = TraoBangConstants.ChuanBi,
                        HoVaTen = slideIncomming.NoiDung ?? "",
                        Order = maxOrder + 1,
                        IdSlide = slideIncomming.Id,
                        IdPlan = plan.Id,
                        LoaiSlide = LoaiSlides.TEXT,
                        IsShow = true,
                        CreatedBy = userid,
                        TrackingDangTrao = true,
                        Note = slideIncomming.Note,
                    };

                    _tbDbContext.TienDoTraoBangs.Add(tienDo);
                    _tbDbContext.SaveChanges();
                }

                await _traoBangService.NotifyCheckIn();

                return new DiemDanhNhanBangDto
                {
                    TenKhoa = subPlan?.Ten ?? string.Empty,
                    Id = slideIncomming != null ? slideIncomming.Id : -1,
                    TrangThai = TraoBangConstants.ChuanBi,
                    Order = maxOrder + 1,
                    IsShow = true
                };

            }

            return null;
        }

        //Prev sinh viên trao bằng 
        public async Task<GetSinhVienDangTraoBangInforDto?> PrevSinhVienTraoBang(int idSubPlan)
        {
            _logger.LogInformation($"{nameof(PrevSinhVienTraoBang)}");

            var sinhVienDangTrao = await _tbDbContext.TienDoTraoBangs
                .FirstOrDefaultAsync(x => x.IdSubPlan == idSubPlan
                            && x.TrangThai == TraoBangConstants.DangTraoBang
                            && !x.Deleted);

            if (sinhVienDangTrao == null)
            {
                var sinhVienCuoiCung = await _tbDbContext.TienDoTraoBangs
                    .Where(x => x.IdSubPlan == idSubPlan
                                && x.TrangThai == TraoBangConstants.DaTraoBang
                                && !x.Deleted)
                    .OrderByDescending(x => x.Order)
                    .FirstOrDefaultAsync();

                if (sinhVienCuoiCung == null)
                {
                    var subPlanForMoBai = await _tbDbContext.SubPlans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

                    if (subPlanForMoBai != null && subPlanForMoBai.IsShowMoBai)
                    {
                        await _traoBangService.NotifySinhVienDangTrao();
                        return new GetSinhVienDangTraoBangInforDto
                        {
                            TenSubPlan = subPlanForMoBai.Ten ?? string.Empty,
                            Text = subPlanForMoBai.MoBai ?? string.Empty,
                            TextNote = subPlanForMoBai.MoBaiNote ?? string.Empty
                        };
                    }

                    return null;
                }

                sinhVienCuoiCung.TrangThai = TraoBangConstants.DangTraoBang;
                _tbDbContext.TienDoTraoBangs.Update(sinhVienCuoiCung);
                await _tbDbContext.SaveChangesAsync();

                var sinhVienInfo = await _tbDbContext.DanhSachSinhVienNhanBangs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == sinhVienCuoiCung.IdSinhVienNhanBang && !x.Deleted);

                var subPlan = await _tbDbContext.SubPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

                await _traoBangService.NotifySinhVienDangTrao();

                return new GetSinhVienDangTraoBangInforDto
                {
                    TenSubPlan = subPlan?.Ten ?? string.Empty,
                    Id = sinhVienCuoiCung.Id,
                    HoVaTen = sinhVienInfo?.HoVaTen ?? string.Empty,
                    MaSoSinhVien = sinhVienInfo?.MaSoSinhVien ?? string.Empty,
                    TenNganhDaoTao = sinhVienInfo?.TenNganhDaoTao ?? string.Empty,
                    XepHang = sinhVienInfo?.XepHang ?? string.Empty,
                    ThanhTich = sinhVienInfo?.ThanhTich ?? string.Empty,
                    CapBang = sinhVienInfo?.CapBang ?? string.Empty,
                    Note = sinhVienInfo?.Note ?? string.Empty
                };
            }

            sinhVienDangTrao.TrangThai = TraoBangConstants.ChuanBi;
            _tbDbContext.TienDoTraoBangs.Update(sinhVienDangTrao);
            await _tbDbContext.SaveChangesAsync();

            var existingLogDangTrao = await _tbDbContext.TraoBangLogs
                .FirstOrDefaultAsync(x => x.IdSinhVien == sinhVienDangTrao.IdSinhVienNhanBang
                                          && x.IdSubPlan == idSubPlan
                                          && !x.Deleted);

            if (existingLogDangTrao != null)
            {
                _tbDbContext.TraoBangLogs.Remove(existingLogDangTrao);
                await _tbDbContext.SaveChangesAsync();
            }

            var sinhVienTruocDo = await _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == idSubPlan
                            && x.Order < sinhVienDangTrao.Order
                            && x.TrangThai == TraoBangConstants.DaTraoBang
                            && !x.Deleted)
                .OrderByDescending(x => x.Order)
                .FirstOrDefaultAsync();

            if (sinhVienTruocDo == null)
            {
                var subPlanForMoBai = await _tbDbContext.SubPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

                if (subPlanForMoBai != null && subPlanForMoBai.IsShowMoBai)
                {
                    await _traoBangService.NotifySinhVienDangTrao();
                    return new GetSinhVienDangTraoBangInforDto
                    {
                        TenSubPlan = subPlanForMoBai.Ten ?? string.Empty,
                        Text = subPlanForMoBai.MoBai ?? string.Empty,
                        TextNote = subPlanForMoBai.MoBaiNote ?? string.Empty
                    };
                }

                return null;
            }

            var existingLogTruocDo = await _tbDbContext.TraoBangLogs
                .FirstOrDefaultAsync(x => x.IdSinhVien == sinhVienTruocDo.IdSinhVienNhanBang
                                          && x.IdSubPlan == idSubPlan
                                          && !x.Deleted);

            if (existingLogTruocDo != null)
            {
                _tbDbContext.TraoBangLogs.Remove(existingLogTruocDo);
                await _tbDbContext.SaveChangesAsync();
            }

            sinhVienTruocDo.TrangThai = TraoBangConstants.DangTraoBang;
            _tbDbContext.TienDoTraoBangs.Update(sinhVienTruocDo);
            await _tbDbContext.SaveChangesAsync();

            var sinhVienInfoTruocDo = await _tbDbContext.DanhSachSinhVienNhanBangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sinhVienTruocDo.IdSinhVienNhanBang && !x.Deleted);

            var subPlanInfo = await _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

            await _traoBangService.NotifySinhVienDangTrao();

            return new GetSinhVienDangTraoBangInforDto
            {
                TenSubPlan = subPlanInfo?.Ten ?? string.Empty,
                Id = sinhVienTruocDo.Id,
                HoVaTen = sinhVienInfoTruocDo?.HoVaTen ?? string.Empty,
                MaSoSinhVien = sinhVienInfoTruocDo?.MaSoSinhVien ?? string.Empty,
                TenNganhDaoTao = sinhVienInfoTruocDo?.TenNganhDaoTao ?? string.Empty,
                XepHang = sinhVienInfoTruocDo?.XepHang ?? string.Empty,
                ThanhTich = sinhVienInfoTruocDo?.ThanhTich ?? string.Empty,
                CapBang = sinhVienInfoTruocDo?.CapBang ?? string.Empty,
                Note = sinhVienInfoTruocDo?.Note ?? string.Empty
            };
        }
        // GetInFor sinh viên được trao bằng tiếp theo 
        public async Task<GetInforSinhVienChuanBiDuocTraoBangResponseDto?> GetInforSinhVienChuanBiDuocTraoBang(int idSubPlan)
        {
            _logger.LogInformation($"{nameof(GetInforSinhVienChuanBiDuocTraoBang)}, idSubPlan= {idSubPlan} ");
            var subPlan = await _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);
            if (subPlan == null)
            {
                return null;
            }

            var sinhVienTiepTheo = await _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == idSubPlan
                            && x.TrangThai == TraoBangConstants.ChuanBi
                            && !x.Deleted)
                .OrderBy(x => x.Order)
                .FirstOrDefaultAsync();

            if (sinhVienTiepTheo == null)
            {
                return null;
            }

            var sinhVienInfor = await _tbDbContext.DanhSachSinhVienNhanBangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sinhVienTiepTheo.IdSinhVienNhanBang && !x.Deleted);

            if (sinhVienInfor == null)
            {
                return null;
            }

            return new GetInforSinhVienChuanBiDuocTraoBangResponseDto
            {
                TenSubPlan = subPlan?.Ten ?? string.Empty,
                Id = sinhVienInfor.Id,
                HoVaTen = sinhVienInfor?.HoVaTen ?? string.Empty,
                MaSoSinhVien = sinhVienInfor?.MaSoSinhVien ?? string.Empty,
                TenNganhDaoTao = sinhVienInfor?.TenNganhDaoTao ?? string.Empty,
                XepHang = sinhVienInfor?.XepHang ?? string.Empty,
                ThanhTich = sinhVienInfor?.ThanhTich ?? string.Empty,
                CapBang = sinhVienInfor?.CapBang ?? string.Empty,
                Note = sinhVienInfor?.Note ?? string.Empty
            };
        }
        // GetInFor sinh viên bắt đầu được PREV
        public async Task<GetInforSinhVienBatDauDuocPrevResponseDto?> GetInforSinhVienBatDauDuocPrev(int idSubPlan)
        {
            _logger.LogInformation($"{nameof(GetInforSinhVienBatDauDuocPrev)}, idSubPlan= {idSubPlan} ");

            var subPlan = await _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == idSubPlan && !x.Deleted);

            if (subPlan == null)
            {
                return null;
            }

            // Sinh viên "bắt đầu được PREV" = sinh viên có TrackingDangTrao = true và TrangThai = "Chuẩn bị"
            var sinhVienBatDauDuocPrev = await _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == idSubPlan
                            && x.TrangThai == TraoBangConstants.ChuanBi
                            && x.TrackingDangTrao == true
                            && !x.Deleted)
                .OrderByDescending(x => x.Order)
                .FirstOrDefaultAsync();
            var sinhVienDuocPrevDen = await _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == idSubPlan
                            && x.TrangThai == TraoBangConstants.DangTraoBang
                            && !x.Deleted)
                .OrderByDescending(x => x.Order)
                .FirstOrDefaultAsync();
            var listSinhVienPrev = await _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == idSubPlan
                            && x.Order >= sinhVienDuocPrevDen.Order && x.Order < sinhVienBatDauDuocPrev.Order
                            && !x.Deleted)
                .OrderByDescending(x => x.Order)
                .ToListAsync();
            foreach (var sv in listSinhVienPrev)
            {
                sv.TrangThai = TraoBangConstants.DaTraoBang;
                _tbDbContext.TienDoTraoBangs.Update(sv);
            }
            if (sinhVienBatDauDuocPrev == null)
            {
                return null;
            }
            sinhVienBatDauDuocPrev.TrangThai = TraoBangConstants.DangTraoBang;
            _tbDbContext.TienDoTraoBangs.Update(sinhVienBatDauDuocPrev);
            await _tbDbContext.SaveChangesAsync();


            var sinhVienInfor = await _tbDbContext.DanhSachSinhVienNhanBangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sinhVienBatDauDuocPrev.IdSinhVienNhanBang && !x.Deleted);

            if (sinhVienInfor == null)
            {
                return null;
            }


            var sinhVienChuanBiTiepTheo = await _tbDbContext.TienDoTraoBangs
                .Where(x => x.IdSubPlan == idSubPlan
                            && x.TrangThai == TraoBangConstants.ChuanBi
                            && x.Order > sinhVienBatDauDuocPrev.Order
                            && !x.Deleted)
                .OrderBy(x => x.Order)
                .FirstOrDefaultAsync();

            GetInforSinhVienChuanBiDuocTraoBangResponseDto? svChuanBiTiepTheo = null;

            if (sinhVienChuanBiTiepTheo != null)
            {
                var sinhVienChuanBiInfor = await _tbDbContext.DanhSachSinhVienNhanBangs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == sinhVienChuanBiTiepTheo.IdSinhVienNhanBang && !x.Deleted);

                if (sinhVienChuanBiInfor != null)
                {
                    svChuanBiTiepTheo = new GetInforSinhVienChuanBiDuocTraoBangResponseDto
                    {
                        TenSubPlan = subPlan?.Ten ?? string.Empty,
                        Id = sinhVienChuanBiInfor.Id,
                        HoVaTen = sinhVienChuanBiInfor?.HoVaTen ?? string.Empty,
                        MaSoSinhVien = sinhVienChuanBiInfor?.MaSoSinhVien ?? string.Empty,
                        TenNganhDaoTao = sinhVienChuanBiInfor?.TenNganhDaoTao ?? string.Empty,
                        XepHang = sinhVienChuanBiInfor?.XepHang ?? string.Empty,
                        ThanhTich = sinhVienChuanBiInfor?.ThanhTich ?? string.Empty,
                        CapBang = sinhVienChuanBiInfor?.CapBang ?? string.Empty,
                        Note = sinhVienChuanBiInfor?.Note ?? string.Empty
                    };
                }
            }
            await _traoBangService.NotifySinhVienDangTrao();
            return new GetInforSinhVienBatDauDuocPrevResponseDto
            {
                SvBatDauLui = new GetInforSinhVienChuanBiDuocTraoBangResponseDto
                {
                    TenSubPlan = subPlan?.Ten ?? string.Empty,
                    Id = sinhVienInfor.Id,
                    HoVaTen = sinhVienInfor?.HoVaTen ?? string.Empty,
                    MaSoSinhVien = sinhVienInfor?.MaSoSinhVien ?? string.Empty,
                    TenNganhDaoTao = sinhVienInfor?.TenNganhDaoTao ?? string.Empty,
                    XepHang = sinhVienInfor?.XepHang ?? string.Empty,
                    ThanhTich = sinhVienInfor?.ThanhTich ?? string.Empty,
                    CapBang = sinhVienInfor?.CapBang ?? string.Empty,
                    Note = sinhVienInfor?.Note ?? string.Empty
                },
                SvChuanBiTiepTheo = svChuanBiTiepTheo
            };
        }
        //Get tiến độ nhận bằng sinh viên bắt đầu PREV
        public async Task<List<ViewTienDoNhanBangResponseDto>> GetTienDoNhanBangSinhVienBatDauPrev(ViewTienDoNhanBangSinhVienBatDauLuiRequestDto dto)
        {
            _logger.LogInformation($"{nameof(GetTienDoNhanBang)}, dto= {JsonSerializer.Serialize(dto)} ");
            var khoaDangTrao = _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefault(x => x.TrangThai == TraoBangConstants.DangTraoBang && !x.Deleted);
            if (khoaDangTrao == null)
            {
                return null;
            }
            var sinhVien = _tbDbContext.DanhSachSinhVienNhanBangs
                .AsNoTracking()
                .FirstOrDefault(x => !x.Deleted && x.IdSubPlan == khoaDangTrao.Id);
            if (sinhVien == null)
            {
                return null;
            }

            var results = new List<TienDoTraoBang>();

            var sinhVienBatDauLui = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .Where(x => !x.Deleted
                            && x.IdSubPlan == khoaDangTrao.Id
                            && x.MaSoSinhVien == dto.Mssv)
                .OrderByDescending(x => x.Order)
                .FirstOrDefaultAsync();

            /*if (sinhVienDaTrao != null)
            {
                results.Add(sinhVienDaTrao);
            }*/

            var soLuongConLai = dto.SoLuong - results.Count;
            if (soLuongConLai > 0)
            {
                var sinhVienChuanBi = await _tbDbContext.TienDoTraoBangs
                    .AsNoTracking()
                    .Where(x => !x.Deleted
                                && x.IdSubPlan == khoaDangTrao.Id
                                && x.Order >= sinhVienBatDauLui.Order
                                && x.TrangThai != TraoBangConstants.DaTraoBang)
                    .OrderBy(x => x.Order)
                    .Take(soLuongConLai)
                    .ToListAsync();

                results.AddRange(sinhVienChuanBi);
            }

            if (!results.Any())
            {
                return null;
            }

            return results.Select(result => new ViewTienDoNhanBangResponseDto
            {
                Id = result.Id,
                HoVaTen = result.HoVaTen,
                MaSoSinhVien = result.MaSoSinhVien,
                CapBang = sinhVien.CapBang,
                TenNganhDaoTao = sinhVien.TenNganhDaoTao,
                TrangThai = result.TrangThai,
                Note = sinhVien.Note ?? "",
                Order = result.Order,
                IsShow = result.IsShow
            }).ToList();
        }
        ///Get khoa tiến độ , list sinh viên đang trao màn cánh gà 
        public async Task<GetInforSubPlanDangTraoResponseDto> GetInforSubPlanDangTrao(int SoLuong)
        {
            //int chuanBiTrao = 5;
            _logger.LogInformation($"{nameof(GetInforSubPlanDangTrao)} ");

            var plan = await _tbDbContext.Plans.Where(x => !x.Deleted && x.TrangThai == TrangThaiPlan.DangHoatDong).FirstOrDefaultAsync()
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var subPlan = await _tbDbContext.SubPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TrangThai == TraoBangConstants.DangTraoBang && x.IdPlan == plan.Id && !x.Deleted && x.IsShow)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);

            var tienDoList = new List<TienDoTraoBang>();

            var sinhVienDaTrao = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .Where(x => x.IdSubPlan == subPlan.Id && !x.Deleted && x.TrangThai == TraoBangConstants.DaTraoBang && x.LoaiSlide == LoaiSlides.SINH_VIEN)
                .OrderByDescending(x => x.Order)
                .FirstOrDefaultAsync();

            // Lấy sv đang trao bằng
            var sinhVienDangTrao = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .Where(x => x.IdSubPlan == subPlan.Id && !x.Deleted && x.TrangThai == TraoBangConstants.DangTraoBang && x.LoaiSlide == LoaiSlides.SINH_VIEN)
                .FirstOrDefaultAsync();

            if (sinhVienDangTrao != null)
            {
                tienDoList.Add(sinhVienDangTrao);
            }

            // Lấy 5 sv đang chuẩn bị
            var sinhVienChuanBi = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .Where(x => x.IdSubPlan == subPlan.Id && !x.Deleted && x.TrangThai == TraoBangConstants.ChuanBi && x.LoaiSlide == LoaiSlides.SINH_VIEN)
                .OrderBy(x => x.Order)
                //.Take(chuanBiTrao)
                .ToListAsync();

            tienDoList.AddRange(sinhVienChuanBi);

            var idsSinhVienTrongTienDo = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .Where(x => x.IdSubPlan == subPlan.Id && !x.Deleted && x.LoaiSlide == LoaiSlides.SINH_VIEN)
                .Select(x => x.IdSinhVienNhanBang)
                .ToListAsync();


            var orderDanhSachChuanBi = new List<int>();
            if (sinhVienChuanBi.Any())
            {
                var idsChuanBi = sinhVienChuanBi.Select(x => x.IdSinhVienNhanBang).ToList();
                orderDanhSachChuanBi = (from sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking()
                                        join sl in _tbDbContext.Slides.AsNoTracking() on sv.Id equals sl.IdSinhVienNhanBang
                                        where !sv.Deleted && !sl.Deleted
                                         && idsChuanBi.Contains(sv.Id)
                                        select sl.Order).ToList();
            }


            var maxOrderDanhSachChuanBi = orderDanhSachChuanBi.Any()
                ? orderDanhSachChuanBi.Max()
                : 0;

            var sinhVienXepHang = (from sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking()
                                   join sl in _tbDbContext.Slides.AsNoTracking() on sv.Id equals sl.IdSinhVienNhanBang
                                   where !sv.Deleted && !sl.Deleted
                                    && sl.IdSubPlan == subPlan.Id && sl.TrangThai == TraoBangConstants.ThamGiaTraoBang
                                    && !idsSinhVienTrongTienDo.Contains(sv.Id)
                                   orderby sl.Order
                                   select new { sv, sl }).Take(SoLuong).ToList();

            var sinhVienXepHangHopLe = sinhVienXepHang
                .Where(x => x.sl.Order >= maxOrderDanhSachChuanBi || maxOrderDanhSachChuanBi == 0)
                .ToList();

            var sinhVienBiBoQua = sinhVienXepHang
                .Where(x => x.sl.Order < maxOrderDanhSachChuanBi && maxOrderDanhSachChuanBi > 0)
                .ToList();

            var idsSinhVien = tienDoList.Select(x => x.IdSinhVienNhanBang)
                .Concat(sinhVienXepHangHopLe.Select(x => x.sv.Id))
                .Concat(sinhVienBiBoQua.Select(x => x.sv.Id))
                .ToList();

            var danhSachSinhViens = await _tbDbContext.DanhSachSinhVienNhanBangs
                .AsNoTracking()
                .Where(x => idsSinhVien.Contains(x.Id) && !x.Deleted)
                .ToListAsync();

            var sinhVienDaTraoCount = await _tbDbContext.TienDoTraoBangs
                .AsNoTracking()
                .CountAsync(x => x.IdSubPlan == subPlan.Id && !x.Deleted && x.TrangThai == TraoBangConstants.DaTraoBang && x.LoaiSlide == LoaiSlides.SINH_VIEN);

            var tongSinhVienThamGiaTraoBang = (from slide in _tbDbContext.Slides.AsNoTracking().Where(x => x.IdSubPlan == subPlan.Id && x.IsShow && !x.Deleted && x.LoaiSlide == LoaiSlides.SINH_VIEN)
                                               join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => !x.Deleted) on slide.IdSinhVienNhanBang equals sv.Id
                                               where slide.TrangThai == TraoBangConstants.ThamGiaTraoBang
                                               select sv.Id
                                               ).Count();


            var itemsFromTienDo = (from tienDo in tienDoList
                                   join sv in danhSachSinhViens on tienDo.IdSinhVienNhanBang equals sv.Id
                                   select new ListSinhVienDto
                                   {
                                       TenSubPlan = subPlan.Ten,
                                       Id = sv.Id,
                                       HoVaTen = sv.QrHoTen,
                                       MaSoSinhVien = sv.MaSoSinhVien,
                                       TenNganhDaoTao = sv.TenNganhDaoTao,
                                       TrangThai = tienDo.TrangThai,
                                       CapBang = sv.CapBang,
                                       OrderTienDo = tienDo.Order,
                                       OrderDanhSachNhanBang = sv.Order,
                                   }).ToList();

            var listTienDoSv = _tbDbContext.TienDoTraoBangs.AsNoTracking().Where(x => !x.Deleted && x.LoaiSlide == LoaiSlides.SINH_VIEN && x.IdSubPlan == subPlan.Id)
                                            .OrderBy(x => x.Order).Select(x => x.IdSinhVienNhanBang).ToList()
                                            .Select((IdSinhVienNhanBang, index) => new { IdSinhVienNhanBang, RowNumber = index + 1 });

            foreach (var item in itemsFromTienDo)
            {
                var ord = listTienDoSv.Where(x => x.IdSinhVienNhanBang == item.Id).FirstOrDefault();
                if (ord != null)
                {
                    item.OrderTienDo = ord.RowNumber;
                }
            }

            //var itemsFromXepHangHopLe = sinhVienXepHangHopLe.Select(x => new ListSinhVienDto
            //{
            //    TenSubPlan = subPlan.Ten,
            //    Id = x.sv.Id,
            //    HoVaTen = x.sv.QrHoTen,
            //    MaSoSinhVien = x.sv.MaSoSinhVien,
            //    TenNganhDaoTao = x.sv.TenNganhDaoTao,
            //    TrangThai = TraoBangConstants.XepHang,
            //    CapBang = x.sv.CapBang,
            //    OrderDanhSachNhanBang = x.sl.Order,
            //}).ToList();


            //var itemsFromSinhVienBiBoQua = sinhVienBiBoQua.Select(x => new ListSinhVienDto
            //{
            //    TenSubPlan = subPlan.Ten,
            //    Id = x.sv.Id,
            //    HoVaTen = x.sv.QrHoTen,
            //    MaSoSinhVien = x.sv.MaSoSinhVien,
            //    TenNganhDaoTao = x.sv.TenNganhDaoTao,
            //    TrangThai = TraoBangConstants.XepHang,
            //    CapBang = x.sv.CapBang,
            //    OrderDanhSachNhanBang = x.sl.Order,
            //}).ToList();

            var items = itemsFromTienDo
                //.Concat(itemsFromXepHangHopLe)
                //.Concat(itemsFromSinhVienBiBoQua)
                .ToList();

            return new GetInforSubPlanDangTraoResponseDto
            {
                Id = subPlan.Id,
                Ten = subPlan.Ten,
                TienDo = $"{sinhVienDaTraoCount}/{tongSinhVienThamGiaTraoBang}",
                Items = items
            };
        }

        public async Task Restart()
        {
            _logger.LogInformation($"{nameof(Restart)}");

            var activePlan = _tbDbContext.Plans.FirstOrDefault(x => x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted)
                                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var listSp = _tbDbContext.SubPlans.Where(x => !x.Deleted && x.IdPlan == activePlan.Id).ToList();

            var listTienDoDelete = new List<TienDoTraoBang>();
            var listLogDelete = new List<TraoBangLog>();

            using (var tran = _tbDbContext.Database.BeginTransaction())
            {
                foreach (var sp in listSp)
                {
                    var tmpListTienDo = _tbDbContext.TienDoTraoBangs.Where(x => x.IdSubPlan == sp.Id && !x.Deleted).ToList();
                    listTienDoDelete.AddRange(tmpListTienDo);

                    sp.TrangThai = TrangThaiSubPlan.ChuanBi;

                    var tmpListLog = _tbDbContext.TraoBangLogs.Where(x => x.IdSubPlan == sp.Id && !x.Deleted).ToList();
                    listLogDelete.AddRange(tmpListLog);
                }

                //await _tbDbContext.Database.ExecuteSqlRawAsync("DELETE FROM tb.TienDoTraoBang");
                //await _tbDbContext.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('tb.TienDoTraoBang', RESEED, 0)");

                //await _tbDbContext.Database.ExecuteSqlRawAsync("UPDATE [tb].[SubPlan] SET [TrangThai] = 2");

                //await _tbDbContext.Database.ExecuteSqlRawAsync("DELETE FROM tb.TraoBangLog");
                //await _tbDbContext.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('tb.TraoBangLog', RESEED, 0)");

                _tbDbContext.TienDoTraoBangs.RemoveRange(listTienDoDelete);
                _tbDbContext.TraoBangLogs.RemoveRange(listLogDelete);

                await _tbDbContext.SaveChangesAsync();

                tran.Commit();
            }


        }
        public async Task<ImportDanhSachSinhVienNhanBangResponseDto> ImportDanhSachNhanBang(ImportDanhSachSinhVienNhanBangDto dto)
        {
            _logger.LogInformation($"{nameof(ImportDanhSachNhanBang)}, dto= {JsonSerializer.Serialize(dto)} ");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var sheetData = await _getSheetData(dto.Url, dto.SheetName);

            var headers = sheetData[0];
            var dataRows = sheetData.Skip(1).ToList();

            // Map header indices
            var colIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i].Trim();
                colIndexMap[header] = i;
            }

            // Validate required headers
            var requiredHeaders = new[] { "Thứ tự Khoa", "STT", "MSSV", "Họ và tên", "Lớp", "Ngày sinh",
                           "Cấp bằng", "Tên ngành đào tạo", "Xếp hạng", "Thành tích",
                           "email", "Khoa Quản lý", "Số quyết định tốt nghiệp",
                           "Ngày quyết định", "Status", "Link QR minio" };

            foreach (var header in requiredHeaders)
            {
                if (!colIndexMap.ContainsKey(header))
                {
                    throw new UserFriendlyException(ErrorCodes.ImportHeaderErrorInvalid);
                }
            }

            var existingMSSVs = await _tbDbContext.DanhSachSinhVienNhanBangs
                .Where(x => !x.Deleted)
                .Select(x => x.MaSoSinhVien)
                .ToListAsync();

            var existingMSSVSet = new HashSet<string>(existingMSSVs, StringComparer.OrdinalIgnoreCase);
            var processedMSSVs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var danhSachList = new List<domain.TraoBang.DanhSachSinhVienNhanBang>();
            var vietnamNow = GetVietnamTime();
            int totalDataImported = 0;

            foreach (var row in dataRows)
            {
                if (row.Count == 0 || row.All(c => string.IsNullOrWhiteSpace(c)))
                {
                    continue;
                }

                try
                {
                    var mssv = GetCellValue(row, colIndexMap, "MSSV");

                    if (string.IsNullOrWhiteSpace(mssv))
                    {
                        continue;
                    }

                    if (existingMSSVSet.Contains(mssv))
                    {
                        _logger.LogWarning($"MSSV {mssv} đã tồn tại trong DB, bỏ qua");
                        continue;
                    }

                    if (processedMSSVs.Contains(mssv))
                    {
                        _logger.LogWarning($"MSSV {mssv} bị trùng trong file import, bỏ qua");
                        continue;
                    }

                    processedMSSVs.Add(mssv);

                    var idSubPlanStr = GetCellValue(row, colIndexMap, "Thứ tự Khoa");
                    var orderStr = GetCellValue(row, colIndexMap, "STT");

                    var idSubPlan = string.IsNullOrWhiteSpace(idSubPlanStr) ? 0 : int.Parse(idSubPlanStr);
                    var order = string.IsNullOrWhiteSpace(orderStr) ? 0 : int.Parse(orderStr);

                    var hoVaTen = GetCellValue(row, colIndexMap, "Họ và tên");
                    var lop = GetCellValue(row, colIndexMap, "Lớp");
                    var ngaySinhStr = GetCellValue(row, colIndexMap, "Ngày sinh");
                    var capBang = GetCellValue(row, colIndexMap, "Cấp bằng");
                    var tenNganhDaoTao = GetCellValue(row, colIndexMap, "Tên ngành đào tạo");
                    var xepHang = GetCellValue(row, colIndexMap, "Xếp hạng");
                    var thanhTich = GetCellValue(row, colIndexMap, "Thành tích");
                    var email = GetCellValue(row, colIndexMap, "email");
                    var khoaQuanLy = GetCellValue(row, colIndexMap, "Khoa Quản lý");
                    var soQuyetDinh = GetCellValue(row, colIndexMap, "Số quyết định tốt nghiệp");
                    var ngayQuyetDinhStr = GetCellValue(row, colIndexMap, "Ngày quyết định");
                    var statusStr = GetCellValue(row, colIndexMap, "Status");
                    var noteStr = GetCellValue(row, colIndexMap, "Note cho MC");
                    var linkQR = GetCellValue(row, colIndexMap, "Link QR minio");

                    var ngaySinh = ParseDate(ngaySinhStr);
                    var ngayQuyetDinh = ParseDate(ngayQuyetDinhStr);
                    var trangThai = MapStatus(statusStr);
                    var emailSinhVien = $"{mssv}@st.huce.edu.vn";

                    var danhSach = new domain.TraoBang.DanhSachSinhVienNhanBang
                    {
                        IdSubPlan = idSubPlan,
                        Order = order,
                        MaSoSinhVien = mssv,
                        HoVaTen = hoVaTen,
                        Lop = lop,
                        NgaySinh = ngaySinh,
                        CapBang = capBang,
                        TenNganhDaoTao = tenNganhDaoTao,
                        XepHang = xepHang,
                        ThanhTich = thanhTich,
                        Email = email,
                        EmailSinhVien = emailSinhVien,
                        KhoaQuanLy = khoaQuanLy,
                        SoQuyetDinhTotNghiep = soQuyetDinh,
                        NgayQuyetDinh = ngayQuyetDinh,
                        TrangThai = trangThai,
                        Note = noteStr,
                        LinkQR = linkQR,
                        IsShow = true,
                        CreatedDate = vietnamNow,
                        Deleted = false
                    };

                    danhSachList.Add(danhSach);
                    totalDataImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Lỗi khi xử lý dòng: {string.Join(", ", row)}. Error: {ex.Message}");
                    continue;
                }
            }

            if (danhSachList.Any())
            {
                await _tbDbContext.BulkInsertAsync(danhSachList);
            }

            _logger.LogInformation($"Tổng số dòng đã insert: {totalDataImported}");

            stopwatch.Stop();

            return new ImportDanhSachSinhVienNhanBangResponseDto
            {
                TotalRowsImported = dataRows.Count,
                TotalDataImported = totalDataImported,
                ImportTimeInSeconds = (int)stopwatch.Elapsed.TotalSeconds
            };
        }

        public byte[] DownloadTemplateImport()
        {
            _logger.LogInformation($"{nameof(DownloadTemplateImport)}");

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ImportKhoa.xlsx"
            );

            var bytes = File.ReadAllBytes(path);

            return bytes;
        }

        public void ImportExcelSubplan(ImportExcelSubPlanDto dto)
        {
            _logger.LogInformation($"{nameof(ImportExcelSubplan)}");

            var username = getCurrentName();
            var data = _excelService.ReadExcelFile(dto.File, "Sheet1");

            int col = 0;
            int indexSTT = col++;
            int indexTen = col++;
            int indexTruongKhoa = col++;
            int indexQrTenKhoa = col++;

            if (data != null && data.Count > 0)
            {
                var listAll = new List<ImportExcelMapSlideSinhVienDto>();

                int rowIndex = 1;
                var listSubPlan = new List<SubPlan>();

                // insert
                for (int i = 0; i < data.Count; i++)
                {
                    if (i == 0)
                    {
                        continue;
                    }

                    var row = data[i];

                    var stt = row[indexSTT];
                    var tenSubPlan = row[indexTen];
                    var tenTruongKhoa = row[indexTruongKhoa];
                    var qrTenKhoa = row[indexQrTenKhoa];

                    if (string.IsNullOrEmpty(tenSubPlan))
                    {
                        continue;
                    }

                    var oldSubPlan = _tbDbContext.SubPlans.FirstOrDefault(x => x.IdPlan == dto.IdPlan && x.Ten == tenSubPlan && !x.Deleted);
                    if (oldSubPlan != null)
                    {
                        oldSubPlan.TruongKhoa = tenTruongKhoa;
                        oldSubPlan.QrTenKhoa = qrTenKhoa;
                        oldSubPlan.Order = rowIndex;
                    }
                    else
                    {
                        var tmpSubPlan = new SubPlan()
                        {
                            IdPlan = dto.IdPlan,
                            Ten = tenSubPlan,
                            TruongKhoa = tenTruongKhoa,
                            QrTenKhoa = qrTenKhoa,
                            Order = rowIndex,
                            CreatedBy = username,
                            IsShow = true,
                            IsShowKetBai = true,
                            IsShowMoBai = true,
                            TrangThai = TrangThaiSubPlan.ChuanBi,
                        };
                        listSubPlan.Add(tmpSubPlan);
                    }

                    rowIndex++;
                }

                _tbDbContext.AddRange(listSubPlan);
                _tbDbContext.SaveChanges();
            }
        }

        private string GetCellValue(List<string> row, Dictionary<string, int> colIndexMap, string headerName)
        {
            if (!colIndexMap.ContainsKey(headerName))
            {
                return string.Empty;
            }

            var index = colIndexMap[headerName];
            if (index >= row.Count)
            {
                return string.Empty;
            }

            return row[index]?.Trim() ?? string.Empty;
        }

        private DateTime ParseDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                return DateTime.MinValue;
            }

            if (DateTime.TryParse(dateStr, out var date))
            {
                return date;
            }

            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var dateExact))
            {
                return dateExact;
            }

            return DateTime.MinValue;
        }

        private int MapStatus(string statusStr)
        {
            if (string.IsNullOrWhiteSpace(statusStr))
            {
                return TraoBangConstants.ChuanBi;
            }

            var status = statusStr.Trim().ToLower();

            if (status.Contains("vắng") || status.Contains("vang"))
            {
                return TraoBangConstants.VangMat;
            }

            if (status.Contains("tham gia"))
            {
                return TraoBangConstants.ThamGiaTraoBang;
            }

            return TraoBangConstants.ChuanBi;
        }

        private async Task<List<List<string>>> _getSheetData(string sheetUrl, string sheetName)
        {
            var serviceAccountPath = _configuration["Google:ServiceAccountPath"];

            if (string.IsNullOrEmpty(serviceAccountPath))
            {
                throw new UserFriendlyException(ErrorCodes.ServiceAccountErrorNotFound, ErrorMessages.GetMessage(ErrorCodes.ServiceAccountErrorNotFound));
            }

            if (!Path.IsPathRooted(serviceAccountPath))
            {
                var basePath = AppContext.BaseDirectory;
                serviceAccountPath = Path.Combine(basePath, serviceAccountPath);
            }

            if (!File.Exists(serviceAccountPath))
            {
                throw new UserFriendlyException(ErrorCodes.ServiceAccountErrorNotFound, ErrorMessages.GetMessage(ErrorCodes.ServiceAccountErrorNotFound));
            }

            var credential = GoogleCredential.FromFile(serviceAccountPath)
                .CreateScoped("https://www.googleapis.com/auth/spreadsheets.readonly");

            var service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Google Sheets API"
            });

            var spreadsheetId = _extractSpreadsheetId(sheetUrl);
            var range = sheetName;

            var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            var responseData = new List<List<string>>();

            if (values != null && values.Any())
            {
                foreach (var row in values)
                {
                    var stringRow = row.Select(c => c?.ToString() ?? string.Empty).ToList();
                    responseData.Add(stringRow);
                }
            }

            return responseData;
        }

        private string _extractSpreadsheetId(string sheetUrl)
        {
            var match = System.Text.RegularExpressions.Regex.Match(sheetUrl, @"/spreadsheets/d/([a-zA-Z0-9-_]+)");
            if (!match.Success || string.IsNullOrEmpty(match.Groups[1].Value))
            {
                throw new UserFriendlyException(ErrorCodes.GoogleSheetUrlErrorInvalid, ErrorMessages.GetMessage(ErrorCodes.GoogleSheetUrlErrorInvalid));
            }
            return match.Groups[1].Value;
        }
        private static DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }
    }
}
