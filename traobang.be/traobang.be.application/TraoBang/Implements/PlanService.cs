using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using traobang.be.application.Base;
using traobang.be.application.TraoBang.Dtos;
using traobang.be.application.TraoBang.Interface;
using traobang.be.infrastructure.data;
using traobang.be.shared.Constants.TraoBang;
using traobang.be.shared.HttpRequest.AppException;
using traobang.be.shared.HttpRequest.BaseRequest;
using traobang.be.shared.HttpRequest.Error;


namespace traobang.be.application.TraoBang.Implements
{
    public class PlanService : BaseService, IPlanService
    {
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        public PlanService(
            TbDbContext tbDbContext,
            ILogger<PlanService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper
        )
            : base(tbDbContext, logger, httpContextAccessor, mapper)
        {
        }
        public void Create(CreatePlanDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto = {JsonSerializer.Serialize(dto)}");

            var vietnameNow = GetVietnamTime();

            var giaoDien = _tbDbContext.GiaoDiens.FirstOrDefault(x => !x.Deleted && x.Id == dto.IdGiaoDien);

            var plan = new domain.TraoBang.Plan
            {
                Ten = dto.Ten,
                MoTa = dto.MoTa,
                ThoiGianBatDau = dto.ThoiGianBatDau,
                ThoiGianKetThuc = dto.ThoiGianKetThuc,
                CreatedDate = vietnameNow,
                TrangThai = TrangThaiPlan.KhoiTao,
                Deleted = false,
                IdGiaoDien = giaoDien?.Id,
            };

            _tbDbContext.Plans.Add(plan);
            _tbDbContext.SaveChanges();
        }

        public void Update(int id, UpdatePlanDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, dto = {JsonSerializer.Serialize(dto)}");

            var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == id && !x.Deleted);
            if (plan == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            }
            var vietnameNow = GetVietnamTime();

            using (var tran = _tbDbContext.Database.BeginTransaction())
            {
                if (plan.TrangThai != dto.TrangThai && dto.TrangThai == TrangThaiPlan.DangHoatDong)
                {
                    var planDangHoatDong = _tbDbContext.Plans.Where(x => x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted);
                    foreach (var pl in planDangHoatDong)
                    {
                        pl.TrangThai = TrangThaiPlan.KhoiTao;
                    }
                }

                var giaoDien = _tbDbContext.GiaoDiens.FirstOrDefault(x => !x.Deleted && x.Id == dto.IdGiaoDien);

                plan.Ten = dto.Ten;
                plan.MoTa = dto.MoTa;
                plan.ThoiGianBatDau = dto.ThoiGianBatDau;
                plan.ThoiGianKetThuc = dto.ThoiGianKetThuc;
                plan.TrangThai = dto.TrangThai;
                plan.IdGiaoDien = giaoDien?.Id;

                _tbDbContext.Plans.Update(plan);
                _tbDbContext.SaveChanges();

                tran.Commit();
            }
        }

        public BaseResponsePagingDto<ViewPlanDto> FindPaging(FindPagingPlanDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto = {JsonSerializer.Serialize(dto)}");
            var query = from p in _tbDbContext.Plans
                        where !p.Deleted
                        orderby p.CreatedDate descending
                        select p;
            var data = query.Paging(dto).ToList();
            var items = _mapper.Map<List<ViewPlanDto>>(data);
            return new BaseResponsePagingDto<ViewPlanDto>
            {
                TotalItems = query.Count(),
                Items = items
            };
        }
        public async Task<List<GetListPlanResponseDto>> GetListPlan()
        {
            _logger.LogInformation($"{nameof(GetListPlan)}");

            var plans = await _tbDbContext.Plans
                .AsNoTracking()
                .Where(x => !x.Deleted)
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new GetListPlanResponseDto
                {
                    Id = x.Id,
                    Ten = x.Ten,
                    TrangThai = x.TrangThai,
                })
                .ToListAsync();

            return plans;
        }

        public void Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id = {id}");
            var vietnameNow = GetVietnamTime();
            var plan = _tbDbContext.Plans.FirstOrDefault(x => x.Id == id && !x.Deleted);

            if (plan == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            }

            if (plan.TrangThai == TrangThaiPlan.DangHoatDong)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorCannotDeleteActivePlan);
            }

            plan.DeletedDate = vietnameNow;
            plan.Deleted = true;
            _tbDbContext.Plans.Update(plan);
            _tbDbContext.SaveChanges();
        }

        public void DeleteConfig(int id)
        {
            _logger.LogInformation($"{nameof(DeleteConfig)}, id = {id}");

            var username = getCurrentName();

            // xóa các slide + sv trước trong chương trình (plan)
            var listOldSlide = (
                            from s in _tbDbContext.Slides.Where(x => !x.Deleted)
                            join sp in _tbDbContext.SubPlans.Where(x => !x.Deleted) on s.IdSubPlan equals sp.Id
                            where sp.IdPlan == id
                            select s
                            ).ToList();
            var listIdOldSlide = listOldSlide.Select(x => x.Id);
            var listOldSv = _tbDbContext.DanhSachSinhVienNhanBangs.Where(x => listIdOldSlide.Contains(x.Id) && !x.Deleted);

            using (var tran = _tbDbContext.Database.BeginTransaction())
            {
                foreach (var oldslide in listOldSlide)
                {
                    oldslide.Deleted = true;
                    oldslide.DeletedBy = username;
                    oldslide.DeletedDate = DateTime.Now;
                }

                foreach (var oldSv in listOldSv)
                {
                    oldSv.Deleted = true;
                    oldSv.DeletedBy = username;
                    oldSv.DeletedDate = DateTime.Now;
                }

                // xoa old subplan
                var listOldSubplan = _tbDbContext.SubPlans.Where(x => x.IdPlan == id && !x.Deleted);
                foreach (var subplan in listOldSubplan)
                {
                    subplan.Deleted = true;
                    subplan.DeletedBy = username;
                    subplan.DeletedDate = DateTime.Now;
                }

                _tbDbContext.SaveChanges();
                tran.Commit();
            }
        }

        private static DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }
    }
}
