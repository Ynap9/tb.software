using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using traobang.be.application.Base;
using traobang.be.application.ThongKe.Dtos;
using traobang.be.application.ThongKe.Implements;
using traobang.be.infrastructure.data;
using traobang.be.shared.Constants.TraoBang;
using traobang.be.shared.HttpRequest.AppException;
using traobang.be.shared.HttpRequest.Error;

namespace traobang.be.application.ThongKe.Interfaces
{
    public class ThongKeService : BaseService, IThongKeService
    {
        public ThongKeService(
            TbDbContext tbDbContext,
            ILogger<ThongKeService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper
        )
            : base(tbDbContext, logger, httpContextAccessor, mapper) { }

        public List<KhoaDtos> FindAllKhoa(int soLuongSinhVien = 0)
        {
            _logger.LogInformation($"{nameof(FindAllKhoa)}, soLuongSinhVien = {soLuongSinhVien}");
            var planActive = _tbDbContext.Plans.FirstOrDefault(x =>
                x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted
            );
            if (planActive == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            }

            var listKhoa = _tbDbContext
                .SubPlans.AsNoTracking()
                .Where(x => x.IdPlan == planActive.Id && !x.Deleted)
                .OrderBy(x => x.Order)
                .ToList();

            var idKhoas = listKhoa.Select(x => x.Id).ToList();

            // một khoa có nhiều slide, mỗi slide loại sinh viên trỏ tới đúng một sinh viên
            var listSinhVien = (
                from slide in _tbDbContext.Slides.AsNoTracking()
                join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking()
                    on slide.IdSinhVienNhanBang equals sv.Id
                where
                    !slide.Deleted
                    && !sv.Deleted
                    && slide.LoaiSlide == LoaiSlides.SINH_VIEN
                    && idKhoas.Contains(slide.IdSubPlan)
                orderby slide.Order
                select new
                {
                    slide.IdSubPlan,
                    SinhVien = new SinhVienTraoBangDto
                    {
                        Id = sv.Id,
                        HoVaTen = sv.HoVaTen,
                        Email = sv.Email,
                        Lop = sv.Lop,
                        TrangThai = slide.TrangThai,
                    },
                }
            ).ToList();

            // số đã trao lấy từ hàng đợi tiến độ, không suy ra được từ slide
            var daTraoTheoKhoa = _tbDbContext
                .TienDoTraoBangs.AsNoTracking()
                .Where(x =>
                    !x.Deleted
                    && x.LoaiSlide == LoaiSlides.SINH_VIEN
                    && x.TrangThai == TraoBangConstants.DaTraoBang
                    && idKhoas.Contains(x.IdSubPlan)
                )
                .GroupBy(x => x.IdSubPlan)
                .Select(g => new { IdSubPlan = g.Key, SoLuong = g.Count() })
                .ToList();

            var result = new List<KhoaDtos>();

            foreach (var khoa in listKhoa)
            {
                var sinhViens = listSinhVien
                    .Where(x => x.IdSubPlan == khoa.Id)
                    .Select(x => x.SinhVien)
                    .ToList();

                var soLuongThamGia = sinhViens.Count(x =>
                    x.TrangThai == TraoBangConstants.ThamGiaTraoBang
                );
                var soLuongVangMat = sinhViens.Count(x =>
                    x.TrangThai == TraoBangConstants.VangMat
                );
                var soLuongDaTrao =
                    daTraoTheoKhoa.FirstOrDefault(x => x.IdSubPlan == khoa.Id)?.SoLuong ?? 0;

                // các số đếm vẫn tính trên toàn bộ khoa, chỉ cắt bớt danh sách trả về
                if (soLuongSinhVien == 0)
                {
                    sinhViens = new List<SinhVienTraoBangDto>();
                }
                else if (soLuongSinhVien > 0)
                {
                    sinhViens = sinhViens.Take(soLuongSinhVien).ToList();
                }

                result.Add(
                    new KhoaDtos
                    {
                        Id = khoa.Id,
                        Ten = khoa.Ten,
                        TruongKhoa = khoa.TruongKhoa,
                        Order = khoa.Order,
                        TrangThai = khoa.TrangThai,
                        IsShow = khoa.IsShow,
                        SoLuongSinhVien = sinhViens.Count,
                        SoLuongThamGia = soLuongThamGia,
                        SoLuongVangMat = soLuongVangMat,
                        SoLuongDaTrao = soLuongDaTrao,
                        SoLuongConLai = soLuongThamGia - soLuongDaTrao,
                        SinhViens = sinhViens,
                    }
                );
            }

            return result;
        }

        public HangDoiTraoBangDto FindAllSinhVien()
        {
            _logger.LogInformation($"{nameof(FindAllSinhVien)}");

            var planActive = _tbDbContext.Plans.FirstOrDefault(x =>
                x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted
            );
            if (planActive == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            }

            // hàng chờ nằm ở TienDoTraoBang, chỉ sinh viên đã được đẩy vào mới có mặt ở đây
            var hangDoi = (
                from td in _tbDbContext.TienDoTraoBangs.AsNoTracking()
                join sp in _tbDbContext.SubPlans.AsNoTracking() on td.IdSubPlan equals sp.Id
                join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking()
                    on td.IdSinhVienNhanBang equals sv.Id
                where
                    !td.Deleted
                    && !sp.Deleted
                    && !sv.Deleted
                    && sp.IdPlan == planActive.Id
                    && td.LoaiSlide == LoaiSlides.SINH_VIEN
                // thứ tự khoa trước, trong mỗi khoa theo thứ tự hàng chờ
                orderby sp.Order, td.Order
                select new SinhVienHangDoiDto
                {
                    Id = td.Id,
                    IdSinhVienNhanBang = sv.Id,
                    IdKhoa = sp.Id,
                    TenKhoa = sp.Ten,
                    OrderKhoa = sp.Order,
                    Order = td.Order,
                    HoVaTen = sv.HoVaTen,
                    MaSoSinhVien = sv.MaSoSinhVien,
                    Lop = sv.Lop,
                    TenNganhDaoTao = sv.TenNganhDaoTao,
                    CapBang = sv.CapBang,
                    XepHang = sv.XepHang,
                    Note = sv.Note,
                    TrangThai = td.TrangThai,
                }
            ).ToList();

            // đánh lại số thứ tự liên tục trên toàn buổi lễ
            for (int i = 0; i < hangDoi.Count; i++)
            {
                hangDoi[i].Stt = i + 1;
            }

            return new HangDoiTraoBangDto
            {
                TongSinhVien = hangDoi.Count,
                SoLuongDaTrao = hangDoi.Count(x => x.TrangThai == TraoBangConstants.DaTraoBang),
                SoLuongDangTrao = hangDoi.Count(x =>
                    x.TrangThai == TraoBangConstants.DangTraoBang
                ),
                SoLuongChuanBi = hangDoi.Count(x => x.TrangThai == TraoBangConstants.ChuanBi),
                Items = hangDoi,
            };
        }

        public ChiTietKhoaDto FindChiTietKhoa(int idKhoa)
        {
            _logger.LogInformation($"{nameof(FindChiTietKhoa)}, idKhoa = {idKhoa}");

            var planActive = _tbDbContext.Plans.FirstOrDefault(x =>
                x.TrangThai == TrangThaiPlan.DangHoatDong && !x.Deleted
            );
            if (planActive == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);
            }

            var khoa = _tbDbContext.SubPlans.AsNoTracking()
                .FirstOrDefault(x => x.Id == idKhoa && x.IdPlan == planActive.Id && !x.Deleted);
            if (khoa == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);
            }

            // danh sách đăng ký của khoa nằm ở slide, đây mới là danh sách đầy đủ
            var danhSach = (
                from slide in _tbDbContext.Slides.AsNoTracking()
                join sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking()
                    on slide.IdSinhVienNhanBang equals sv.Id
                where
                    !slide.Deleted
                    && !sv.Deleted
                    && slide.IdSubPlan == idKhoa
                    && slide.LoaiSlide == LoaiSlides.SINH_VIEN
                orderby slide.Order
                select new
                {
                    IdSlide = slide.Id,
                    OrderSlide = slide.Order,
                    TrangThaiSlide = slide.TrangThai,
                    SinhVien = sv,
                }
            ).ToList();

            // tiến độ thực tế nằm ở hàng đợi, sinh viên chưa check-in thì không có ở đây
            var tienDo = _tbDbContext
                .TienDoTraoBangs.AsNoTracking()
                .Where(x =>
                    !x.Deleted && x.IdSubPlan == idKhoa && x.LoaiSlide == LoaiSlides.SINH_VIEN
                )
                .Select(x => new
                {
                    x.IdSinhVienNhanBang,
                    x.TrangThai,
                    x.Order,
                })
                .ToList();

            var items = new List<SinhVienChiTietDto>();
            int stt = 0;

            foreach (var dong in danhSach)
            {
                var sv = dong.SinhVien;
                var td = tienDo.FirstOrDefault(x => x.IdSinhVienNhanBang == sv.Id);

                // vắng mặt thì giữ nguyên, còn lại lấy theo hàng đợi;
                // chưa có mặt trong hàng đợi nghĩa là chưa check-in
                int trangThai;
                if (dong.TrangThaiSlide == TraoBangConstants.VangMat)
                {
                    trangThai = TraoBangConstants.VangMat;
                }
                else if (td != null)
                {
                    trangThai = td.TrangThai;
                }
                else
                {
                    trangThai = TraoBangConstants.XepHang;
                }

                stt++;
                items.Add(
                    new SinhVienChiTietDto
                    {
                        Stt = stt,
                        Id = sv.Id,
                        IdSlide = dong.IdSlide,
                        MaSoSinhVien = sv.MaSoSinhVien,
                        HoVaTen = sv.HoVaTen,
                        Lop = sv.Lop,
                        TenNganhDaoTao = sv.TenNganhDaoTao,
                        CapBang = sv.CapBang,
                        XepHang = sv.XepHang,
                        ThanhTich = sv.ThanhTich,
                        Note = sv.Note,
                        Order = dong.OrderSlide,
                        OrderHangDoi = td?.Order,
                        DaLenNhanBang = trangThai == TraoBangConstants.DaTraoBang,
                        TrangThai = trangThai,
                    }
                );
            }

            return new ChiTietKhoaDto
            {
                Id = khoa.Id,
                Ten = khoa.Ten,
                TruongKhoa = khoa.TruongKhoa,
                Order = khoa.Order,
                TrangThai = khoa.TrangThai,
                SoLuongSinhVien = items.Count,
                SoLuongThamGia = items.Count(x => x.TrangThai != TraoBangConstants.VangMat),
                SoLuongVangMat = items.Count(x => x.TrangThai == TraoBangConstants.VangMat),
                SoLuongDaTrao = items.Count(x => x.TrangThai == TraoBangConstants.DaTraoBang),
                SoLuongDangTrao = items.Count(x => x.TrangThai == TraoBangConstants.DangTraoBang),
                SoLuongDaCheckIn = items.Count(x => x.TrangThai == TraoBangConstants.ChuanBi),
                SoLuongChuaCheckIn = items.Count(x => x.TrangThai == TraoBangConstants.XepHang),
                SinhViens = items,
            };
        }
    }
}
