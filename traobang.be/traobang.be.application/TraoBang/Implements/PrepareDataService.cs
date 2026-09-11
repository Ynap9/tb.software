using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using traobang.be.application.Base;
using traobang.be.application.TraoBang.Dtos.PrepareData;
using traobang.be.application.TraoBang.Interfaces;
using traobang.be.domain.TraoBang;
using traobang.be.infrastructure.data;
using traobang.be.shared.Constants.TraoBang;
using traobang.be.shared.HttpRequest.AppException;
using traobang.be.shared.HttpRequest.Error;

namespace traobang.be.application.TraoBang.Implements
{
    public class PrepareDataService : BaseService, IPrepareDataService
    {

        public PrepareDataService(
            TbDbContext tbDbContext,
            ILogger<PrepareDataService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        : base(tbDbContext, logger, httpContextAccessor, mapper)
        {

        }

        public void PrepareForDemo(PrepareDataForDemoDto dto)
        {
            _logger.LogInformation($"{nameof(PrepareForDemo)} dto={JsonSerializer.Serialize(dto)}");

            var plan = _tbDbContext.Plans.AsNoTracking().FirstOrDefault(x => x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var listSubplan = _tbDbContext.SubPlans.AsNoTracking()
                                .Where(x => x.IdPlan == plan.Id && !x.Deleted && x.IsShow)
                                .OrderBy(x => x.Order)
                                .ToList();

            foreach (var subplan in listSubplan)
            {
                var slides = _tbDbContext.Slides
                                    .Where(x => !x.Deleted && x.IsShow && x.IdSubPlan == subplan.Id)
                                    .OrderBy(x => x.Id)
                                    .ToList();

                var lastSlideText = slides.Where(x => x.LoaiSlide == LoaiSlides.TEXT).LastOrDefault();

                int stt = 1;
                int countSvDemo = 1;
                const int countSvDemoMax = 2;
                bool isLastSlideText = false;

                foreach (var slide in slides)
                {
                    // checkin full thì lấy hết, không dừng ở slide text cuối
                    if (isLastSlideText && !dto.IsCheckinFull)
                    {
                        continue;
                    }

                    bool isExistInTienDo = _tbDbContext.TienDoTraoBangs.Any(x => x.IdSlide == slide.Id && !x.Deleted);

                    if (slide.LoaiSlide == LoaiSlides.TEXT)
                    {
                        if (!isExistInTienDo)
                        {
                            var tienDoTraoBang = new TienDoTraoBang
                            {
                                IdSubPlan = subplan.Id,
                                IdSinhVienNhanBang = slide.IdSinhVienNhanBang ?? -1,
                                HoVaTen = slide.NoiDung ?? string.Empty,
                                //MaSoSinhVien = sinhVien.MaSoSinhVien,
                                TrangThai = TraoBangConstants.ChuanBi,
                                Order = stt,
                                IsShow = true,
                                CreatedDate = DateTime.Now,
                                IdSlide = slide.Id,
                                LoaiSlide = slide.LoaiSlide,
                                Note = slide.Note,
                                IdPlan = plan.Id,
                                Deleted = false
                            };
                            _tbDbContext.TienDoTraoBangs.Add(tienDoTraoBang);
                        }

                        stt += 1;
                        countSvDemo = 0;

                        if (slide.Id == lastSlideText?.Id)
                        {
                            isLastSlideText = true;
                        }
                    }
                    else if (slide.LoaiSlide == LoaiSlides.SINH_VIEN && (dto.IsCheckinFull || countSvDemo <= countSvDemoMax))
                    {
                        var sv = _tbDbContext.DanhSachSinhVienNhanBangs.FirstOrDefault(x => x.Id == slide.IdSinhVienNhanBang && !x.Deleted);

                        if (sv != null)
                        {
                            if (!isExistInTienDo)
                            {
                                var tienDoTraoBang = new TienDoTraoBang
                                {
                                    IdSubPlan = subplan.Id,
                                    IdSinhVienNhanBang = slide.IdSinhVienNhanBang ?? -1,
                                    HoVaTen = sv.HoVaTen,
                                    MaSoSinhVien = sv.MaSoSinhVien,
                                    TrangThai = TraoBangConstants.ChuanBi,
                                    Order = stt,
                                    IsShow = true,
                                    CreatedDate = DateTime.Now,
                                    IdSlide = slide.Id,
                                    LoaiSlide = slide.LoaiSlide,
                                    Note = sv.Note,
                                    IdPlan = plan.Id,
                                    Deleted = false
                                };
                                _tbDbContext.TienDoTraoBangs.Add(tienDoTraoBang);
                            }

                            stt += 1;
                            countSvDemo += 1;
                        }

                    }
                }

            }

            _tbDbContext.SaveChanges();
        }

    }
}
