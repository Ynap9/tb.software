using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using traobang.be.application.Base;
using traobang.be.application.TraoBang.Dtos;
using traobang.be.application.TraoBang.Dtos.Slide;
using traobang.be.application.TraoBang.Interfaces;
using traobang.be.domain.TraoBang;
using traobang.be.infrastructure.data;
using traobang.be.infrastructure.external.Excel;
using traobang.be.infrastructure.external.File;
using traobang.be.infrastructure.external.File.Dtos;
using traobang.be.infrastructure.external.QrCode;
using traobang.be.infrastructure.external.QrCode.Dtos;
using traobang.be.shared.Constants.TraoBang;
using traobang.be.shared.HttpRequest.AppException;
using traobang.be.shared.HttpRequest.BaseRequest;
using traobang.be.shared.HttpRequest.Error;
using traobang.be.shared.Settings;
using traobang.be.shared.Utils;

namespace traobang.be.application.TraoBang.Implements
{
    public class SlideService : BaseService, ISlideService
    {
        private readonly IExcelService _excelService;
        private readonly IFileS3Services _fileS3Service;
        private readonly IQrCodeService _qrCodeService;
        private readonly FileS3Config _fileS3Config;
        private readonly TemplateSettings _templateSettings;

        public SlideService(
            TbDbContext tbDbContext,
            ILogger<SlideService> logger,
            IHttpContextAccessor httpContextAccessor,
            IExcelService excelService,
            IFileS3Services fileS3Service,
            IQrCodeService qrCodeService,
            IOptions<FileS3Config> fileS3Config,
            IOptions<TemplateSettings> templateSettings,
            IMapper mapper)
        : base(tbDbContext, logger, httpContextAccessor, mapper)
        {
            _excelService = excelService;
            _fileS3Service = fileS3Service;
            _qrCodeService = qrCodeService;
            _fileS3Config = fileS3Config.Value;
            _templateSettings = templateSettings.Value;
        }

        public void Create(CreateSlideDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto = {JsonSerializer.Serialize(dto)}");
            var username = getCurrentName();
            if (dto.LoaiSlide == LoaiSlides.TEXT && string.IsNullOrEmpty(dto.NoiDung))
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorLoaiSlideBinhThuongPhaiCoNoiDung);
            }

            if (dto.LoaiSlide == LoaiSlides.SINH_VIEN && dto.SinhVien == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorLoaiSlideSinhVienPhaiCoSinhVien);
            }

            int? idSv = null;
            if (dto.LoaiSlide == LoaiSlides.SINH_VIEN && !string.IsNullOrEmpty(dto.SinhVien?.MaSoSinhVien))
            {
                var inputSv = _mapper.Map<DanhSachSinhVienNhanBang>(dto.SinhVien);
                inputSv.CreatedBy = username;
                _tbDbContext.DanhSachSinhVienNhanBangs.Add(inputSv);

                _tbDbContext.SaveChanges();
                idSv = inputSv.Id;
            }

            var qr = _tbDbContext.Slides.AsNoTracking().Where(x => x.IdSubPlan == dto.IdSubPlan && !x.Deleted);
            int maxSlideOrder = qr.Any() ? qr.Max(x => x.Order) : 0;

            _tbDbContext.Slides.Add(new Slide()
            {
                IdSinhVienNhanBang = idSv,
                IdSubPlan = dto.IdSubPlan,
                IsShow = dto.IsShow,
                NoiDung = dto.NoiDung,
                LoaiSlide = dto.LoaiSlide,
                Note = dto.Note,
                TrangThai = dto.TrangThai,
                Order = maxSlideOrder + 1,
                CreatedBy = username
            });
            _tbDbContext.SaveChanges();
        }

        public void Update(UpdateSlideDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto = {JsonSerializer.Serialize(dto)}");

            if (dto.LoaiSlide == LoaiSlides.TEXT && string.IsNullOrEmpty(dto.NoiDung))
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorLoaiSlideBinhThuongPhaiCoNoiDung);
            }

            if (dto.LoaiSlide == LoaiSlides.SINH_VIEN && dto.SinhVien == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorLoaiSlideSinhVienPhaiCoSinhVien);
            }

            var slide = _tbDbContext.Slides.FirstOrDefault(x => x.Id == dto.Id && !x.Deleted);
            if (slide == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSlideNotFound);
            }

            slide.IdSubPlan = dto.IdSubPlan;
            slide.IsShow = dto.IsShow;
            slide.NoiDung = dto.NoiDung;
            slide.LoaiSlide = dto.LoaiSlide;
            slide.Note = dto.Note;
            slide.TrangThai = dto.TrangThai;
            slide.Order = dto.Order;

            if (slide.LoaiSlide == LoaiSlides.SINH_VIEN && dto.SinhVien != null)
            {
                var sv = _tbDbContext.DanhSachSinhVienNhanBangs.FirstOrDefault(x => x.Id == dto.SinhVien.Id && !x.Deleted);
                if (sv != null)
                {
                    sv.HoVaTen = dto.SinhVien.HoVaTen;
                    sv.MaSoSinhVien = dto.SinhVien.MaSoSinhVien;
                    sv.Lop = dto.SinhVien.Lop;
                    sv.NgaySinh = dto.SinhVien.NgaySinh;
                    sv.CapBang = dto.SinhVien.CapBang;
                    sv.TenNganhDaoTao = dto.SinhVien.TenNganhDaoTao;
                    sv.XepHang = dto.SinhVien.XepHang;
                    sv.ThanhTich = dto.SinhVien.ThanhTich;
                    sv.Email = dto.SinhVien.Email;
                    sv.EmailSinhVien = $"{dto.SinhVien.MaSoSinhVien}@st.huce.edu.vn";
                    sv.KhoaQuanLy = dto.SinhVien.KhoaQuanLy;
                    sv.SoQuyetDinhTotNghiep = dto.SinhVien.SoQuyetDinhTotNghiep;
                    sv.NgayQuyetDinh = dto.SinhVien.NgayQuyetDinh;
                    sv.Note = dto.SinhVien.Note;
                    // LinkQR do server sinh ở GenerateQr, không nhận từ client
                }
                else
                {
                    var newsv = new DanhSachSinhVienNhanBang
                    {
                        HoVaTen = dto.SinhVien.HoVaTen,
                        MaSoSinhVien = dto.SinhVien.MaSoSinhVien,
                        Lop = dto.SinhVien.Lop,
                        NgaySinh = dto.SinhVien.NgaySinh,
                        CapBang = dto.SinhVien.CapBang,
                        TenNganhDaoTao = dto.SinhVien.TenNganhDaoTao,
                        XepHang = dto.SinhVien.XepHang,
                        ThanhTich = dto.SinhVien.ThanhTich,
                        Email = dto.SinhVien.Email,
                        EmailSinhVien = $"{dto.SinhVien.MaSoSinhVien}@st.huce.edu.vn",
                        KhoaQuanLy = dto.SinhVien.KhoaQuanLy,
                        SoQuyetDinhTotNghiep = dto.SinhVien.SoQuyetDinhTotNghiep,
                        NgayQuyetDinh = dto.SinhVien.NgayQuyetDinh,
                        Note = dto.SinhVien.Note,
                        // LinkQR do server sinh ở GenerateQr, không nhận từ client
                    };
                    _tbDbContext.DanhSachSinhVienNhanBangs.Add(newsv);
                    _tbDbContext.SaveChanges();
                    slide.IdSinhVienNhanBang = newsv.Id;
                }
            }
            else if (dto.LoaiSlide == LoaiSlides.TEXT)
            {
                var oldSv = _tbDbContext.DanhSachSinhVienNhanBangs.FirstOrDefault(x => x.Id == slide.IdSinhVienNhanBang && !x.Deleted);
                if (oldSv != null)
                {
                    string username = getCurrentName();

                    oldSv.Deleted = true;
                    oldSv.DeletedDate = DateTime.Now;
                    oldSv.DeletedBy = username;
                }
                slide.IdSinhVienNhanBang = null;
            }

            _tbDbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            _logger.LogInformation($"{nameof(Create)}, id = {id}");

            var slide = _tbDbContext.Slides.FirstOrDefault(x => x.Id == id && !x.Deleted);
            if (slide == null)
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorSlideNotFound);
            }

            var username = getCurrentName();

            slide.Deleted = true;
            slide.DeletedDate = DateTime.Now;
            slide.DeletedBy = username;

            _tbDbContext.SaveChanges();
        }

        public BaseResponsePagingDto<ViewSlideDto> FindPaging(FindPagingSlideDto dto)
        {
            _logger.LogInformation($"{nameof(FindPagingSlideDto)}, dto = {JsonSerializer.Serialize(dto)}");

#pragma warning disable CS8601 // Possible null reference assignment.
            var query = from s in _tbDbContext.Slides.AsNoTracking().Where(x => !x.Deleted)
                        join sp in _tbDbContext.SubPlans.AsNoTracking().Where(x => !x.Deleted) on s.IdSubPlan equals sp.Id
                        join p in _tbDbContext.Plans.AsNoTracking().Where(x => !x.Deleted) on sp.IdPlan equals p.Id
                        join sinhVien in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => !x.Deleted) on s.IdSinhVienNhanBang equals sinhVien.Id into sinhVienGroup
                        from sv in sinhVienGroup.DefaultIfEmpty()
                            //from sv in _tbDbContext.DanhSachSinhVienNhanBangs.AsNoTracking().Where(x => x.Id == s.IdSinhVienNhanBang && !x.Deleted).DefaultIfEmpty()
                        where (string.IsNullOrEmpty(dto.Keyword) || (
                            (s.NoiDung ?? "").Trim().ToLower().Contains(dto.Keyword.Trim().ToLower()) ||
                            (sv.HoVaTen ?? "").Trim().ToLower().Contains(dto.Keyword.Trim().ToLower())
                        )) &&
                        (dto.IdPlan == null || dto.IdPlan == p.Id) &&
                        //(sp.Id == 2)
                        (dto.IdSubPlan == null || dto.IdSubPlan == sp.Id)
                        orderby s.Id
                        select new ViewSlideDto()
                        {
                            Id = s.Id,
                            IdSubPlan = s.IdSubPlan,
                            IdSinhVienNhanBang = s.IdSinhVienNhanBang,
                            LoaiSlide = s.LoaiSlide,
                            IsShow = s.IsShow,
                            CreatedBy = s.CreatedBy,
                            CreatedDate = s.CreatedDate,
                            NoiDung = s.NoiDung,
                            Note = s.Note,
                            Order = s.Order,
                            TrangThai = s.TrangThai,
                            SinhVien = sv == null ? null : new ViewSinhVienNhanBangDto()
                            {
                                Id = sv.Id,
                                Note = sv.Note,
                                SoQuyetDinhTotNghiep = sv.SoQuyetDinhTotNghiep,
                                CapBang = sv.CapBang,
                                Email = sv.Email,
                                EmailSinhVien = sv.EmailSinhVien,
                                HoVaTen = sv.HoVaTen,
                                KhoaQuanLy = sv.KhoaQuanLy,
                                LinkQR = sv.LinkQR,
                                LinkQrOnly = sv.LinkQrOnly,
                                Lop = sv.Lop,
                                MaSoSinhVien = sv.MaSoSinhVien,
                                NgayQuyetDinh = sv.NgayQuyetDinh,
                                NgaySinh = sv.NgaySinh,
                                TenNganhDaoTao = sv.TenNganhDaoTao,
                                ThanhTich = sv.ThanhTich,
                                XepHang = sv.XepHang,
                            }
                        };
#pragma warning restore CS8601 // Possible null reference assignment.
            var items = query.Paging(dto).ToList();

            // cộng domain vào link qr để FE hiển thị được ảnh, chỉ áp dụng với slide sinh viên
            foreach (var item in items)
            {
                if (item.LoaiSlide == LoaiSlides.SINH_VIEN && item.SinhVien != null)
                {
                    if (!string.IsNullOrEmpty(item.SinhVien.LinkQR))
                    {
                        item.SinhVien.LinkQR = $"{_fileS3Config.BaseUrl}/{item.SinhVien.LinkQR}";
                    }

                    if (!string.IsNullOrEmpty(item.SinhVien.LinkQrOnly))
                    {
                        item.SinhVien.LinkQrOnly = $"{_fileS3Config.BaseUrl}/{item.SinhVien.LinkQrOnly}";
                    }
                }
            }

            return new BaseResponsePagingDto<ViewSlideDto>
            {
                TotalItems = query.Count(),
                Items = items
            };
        }

        public ViewSlideDto FindById(int id)
        {
            _logger.LogInformation($"{nameof(Create)}, id = {id}");

            var slide = _tbDbContext.Slides.FirstOrDefault(x => x.Id == id && !x.Deleted);
            var data = _mapper.Map<ViewSlideDto>(slide);

            var sv = _tbDbContext.DanhSachSinhVienNhanBangs.FirstOrDefault(x => x.Id == slide.IdSinhVienNhanBang && !x.Deleted);
            data.SinhVien = _mapper.Map<ViewSinhVienNhanBangDto>(sv);
            return data;
        }

        public byte[] DownloadTemplateImport()
        {
            _logger.LogInformation($"{nameof(DownloadTemplateImport)}");

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ImportSlide.xlsx"
            );

            var bytes = File.ReadAllBytes(path);

            return bytes;
        }

        public byte[] ExportSlideSinhVien(ExportSlideSinhVienDto dto)
        {
            _logger.LogInformation($"{nameof(ExportSlideSinhVien)}, dto = {JsonSerializer.Serialize(dto)}");

            var query = (
                    from sl in _tbDbContext.Slides
                    join sv in _tbDbContext.DanhSachSinhVienNhanBangs on sl.IdSinhVienNhanBang equals sv.Id
                    join sp in _tbDbContext.SubPlans on sl.IdSubPlan equals sp.Id
                    where !sl.Deleted && !sv.Deleted && !sp.Deleted
                        && sl.LoaiSlide == LoaiSlides.SINH_VIEN
                    select new { sl, sv, sp }
                );

            if (dto.IdPlan.HasValue)
            {
                query = query.Where(x => x.sp.IdPlan == dto.IdPlan);
            }

            if (dto.IdSubPlan.HasValue)
            {
                query = query.Where(x => x.sp.Id == dto.IdSubPlan);
            }

            var listSlide = query.OrderBy(x => x.sp.Order).ThenBy(x => x.sl.Order).ToList();

            // bỏ QR tên khoa, QR họ tên, note cho MC so với file mẫu import, thêm 2 cột link QR
            var headers = new List<string>
            {
                "STT",
                "Khoa",
                "Loại Slide",
                "MSSV",
                "Họ và tên / Nội dung",
                "Lớp",
                "Ngày sinh",
                "Cấp bằng",
                "Tên ngành đào tạo",
                "Xếp hạng",
                "Thành tích",
                "Email",
                "Khoa Quản lý",
                "Trưởng Khoa",
                "Số quyết định tốt nghiệp",
                "Ngày quyết định",
                "Link QR",
                "Link QR (không có thông tin)",
            };

            var rows = new List<List<string>>();
            int stt = 1;
            foreach (var item in listSlide)
            {
                rows.Add(new List<string>
                {
                    stt.ToString(),
                    item.sp.Ten,
                    item.sl.LoaiSlide.ToString(),
                    item.sv.MaSoSinhVien,
                    item.sv.HoVaTen,
                    item.sv.Lop,
                    item.sv.NgaySinh?.ToString("dd/MM/yyyy") ?? string.Empty,
                    item.sv.CapBang,
                    item.sv.TenNganhDaoTao,
                    item.sv.XepHang,
                    item.sv.ThanhTich,
                    item.sv.Email,
                    item.sv.KhoaQuanLy,
                    item.sp.TruongKhoa,
                    item.sv.SoQuyetDinhTotNghiep,
                    item.sv.NgayQuyetDinh?.ToString("dd/MM/yyyy") ?? string.Empty,
                    string.IsNullOrEmpty(item.sv.LinkQR) ? string.Empty : $"{_fileS3Config.BaseUrl}/{item.sv.LinkQR}",
                    string.IsNullOrEmpty(item.sv.LinkQrOnly) ? string.Empty : $"{_fileS3Config.BaseUrl}/{item.sv.LinkQrOnly}",
                });

                stt++;
            }

            return _excelService.WriteExcelFile(headers, rows, "Sheet1");
        }

        public void ImportSlide(ImportExcelSlideDto dto)
        {
            _logger.LogInformation($"{nameof(ImportSlide)}, dto = {JsonSerializer.Serialize(dto)}");

            var username = getCurrentName();
            var data = _excelService.ReadExcelFile(dto.File, "Sheet1");

            int col = 0;
            int indexSTT = col++;
            int indexSubPlan = col++;
            int indexLoaiSlide = col++;
            int indexMSSV = col++;
            int indexHoTenNoiDung = col++;
            int indexLop = col++;
            int indexNgaySinh = col++;
            int indexCapBang = col++;
            int indexTenNganhDaoTao = col++;
            int indexXepHang = col++;
            int indexThanhTich = col++;
            int indexEmail = col++;
            int indexKhoaQuanLy = col++;
            int indexTruongKhoa = col++;
            int indexSoQuyetDinhTotNghiep = col++;
            int indexNgayQuyetDinh = col++;
            int indexNoteChoMC = col++;
            int indexQrTenKhoa = col++;
            int indexQrHoTen = col++;

            if (data != null && data.Count > 0)
            {
                var listAll = new List<ImportExcelMapSlideSinhVienDto>();

                int rowIndex = 1;

                using (var tran = _tbDbContext.Database.BeginTransaction())
                {
                    // xóa các slide + sv trước trong chương trình (plan)
                    var listOldSlide = (
                                    from s in _tbDbContext.Slides.Where(x => !x.Deleted)
                                    join sp in _tbDbContext.SubPlans.Where(x => !x.Deleted) on s.IdSubPlan equals sp.Id
                                    where sp.IdPlan == dto.IdPlan
                                    select s
                                    ).ToList();
                    var listIdOldSlide = listOldSlide.Select(x => x.Id);
                    var listOldSv = _tbDbContext.DanhSachSinhVienNhanBangs.Where(x => listIdOldSlide.Contains(x.Id) && !x.Deleted);

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

                    var slideSvOrderDict = new Dictionary<int, int>();
                    var slideTextOrderDict = new Dictionary<int, int>();

                    // insert vao map
                    for (int i = 0; i < data.Count; i++)
                    {
                        if (i == 0)
                        {
                            continue;
                        }
                        var row = data[i];

                        var stt = row[indexSTT];
                        var tenSubPlan = row[indexSubPlan];
                        var loaiSlide = Convert.ToInt32(row[indexLoaiSlide]);
                        var mssv = row[indexMSSV];
                        var hoTenNoiDung = row[indexHoTenNoiDung];
                        var lop = row[indexLop];
                        var ngaySinh = row[indexNgaySinh];
                        var capBang = row[indexCapBang];
                        var tenNganhDaoTao = row[indexTenNganhDaoTao];
                        var xepHang = row[indexXepHang];
                        var thanhTich = row[indexThanhTich];
                        var email = row[indexEmail];
                        var khoaQuanLy = row[indexKhoaQuanLy];
                        var truongKhoa = row[indexTruongKhoa];
                        var soQuyetDinhTotNghiep = row[indexSoQuyetDinhTotNghiep];
                        var ngayQuyetDinh = row[indexNgayQuyetDinh];
                        var noteChoMC = row[indexNoteChoMC];
                        var qrTenKhoa = row[indexQrTenKhoa];
                        var qrHoTen = row[indexQrHoTen];

                        var subplan = _tbDbContext.SubPlans.FirstOrDefault(x => x.IdPlan == dto.IdPlan && x.Ten == tenSubPlan && !x.Deleted);

                        if (subplan == null)
                        {
                            continue;
                        }

                        int tmpOrder = 0;
                        if (loaiSlide == LoaiSlides.SINH_VIEN)
                        {
                            if (slideSvOrderDict.ContainsKey(subplan.Id))
                            {
                                slideSvOrderDict[subplan.Id]++;
                            }
                            else
                            {
                                slideSvOrderDict[subplan.Id] = 1;
                            }
                            tmpOrder = slideSvOrderDict[subplan.Id];
                        }
                        else if (loaiSlide == LoaiSlides.TEXT)
                        {
                            if (slideTextOrderDict.ContainsKey(subplan.Id))
                            {
                                slideTextOrderDict[subplan.Id]++;
                            }
                            else
                            {
                                slideTextOrderDict[subplan.Id] = 1;
                            }
                            tmpOrder = slideTextOrderDict[subplan.Id];
                        }

                        var tmpMap = new ImportExcelMapSlideSinhVienDto();

                        if (loaiSlide == LoaiSlides.SINH_VIEN)
                        {
                            tmpMap.SinhVien = new DanhSachSinhVienNhanBang
                            {
                                CapBang = capBang,
                                Email = email,
                                EmailSinhVien = email,
                                HoVaTen = hoTenNoiDung,
                                MaSoSinhVien = mssv,
                                Lop = lop,
                                NgayQuyetDinh = DateTimeUtils.ParseVietnamDate(ngayQuyetDinh),
                                NgaySinh = DateTimeUtils.ParseVietnamDate(ngaySinh),
                                Note = noteChoMC,
                                KhoaQuanLy = khoaQuanLy,
                                SoQuyetDinhTotNghiep = soQuyetDinhTotNghiep,
                                TenNganhDaoTao = tenNganhDaoTao,
                                ThanhTich = thanhTich,
                                XepHang = xepHang,
                                QrHoTen = qrHoTen,
                                QrTenKhoa = qrTenKhoa,
                            };
                        }

                        tmpMap.Slide = new Slide
                        {
                            IdSubPlan = subplan.Id,
                            IsShow = true,
                            LoaiSlide = loaiSlide,
                            NoiDung = hoTenNoiDung,
                            Note = noteChoMC,
                            Order = tmpOrder,
                            TrangThai = TraoBangConstants.ThamGiaTraoBang,
                            CreatedBy = username,
                        };

                        listAll.Add(tmpMap);

                        rowIndex++;
                    }

                    // insert list sinh vien
                    var listSinhVien = listAll.Where(x => x.SinhVien != null).Select(x => x.SinhVien);
                    _tbDbContext.DanhSachSinhVienNhanBangs.AddRange(listSinhVien!);
                    _tbDbContext.SaveChanges();

                    // insert list slide
                    var listSlide = new List<Slide>();
                    foreach (var item in listAll)
                    {
                        if (item.SinhVien != null)
                        {
                            item.Slide.IdSinhVienNhanBang = item.SinhVien.Id;
                        }
                        listSlide.Add(item.Slide);
                    }
                    _tbDbContext.Slides.AddRange(listSlide);
                    _tbDbContext.SaveChanges();

                    tran.Commit();
                }
            }
        }

        public async Task GenerateQr(GenerateSinhVienQrDto dto)
        {
            _logger.LogInformation($"{nameof(GenerateQr)}, dto = {JsonSerializer.Serialize(dto)}");

            var plan = _tbDbContext.Plans.AsNoTracking().Where(x => x.Id == dto.IdPlan && !x.Deleted).FirstOrDefault()
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorPlanNotFound);

            var listSv = (
                    from sl in _tbDbContext.Slides
                    join sv in _tbDbContext.DanhSachSinhVienNhanBangs on sl.IdSinhVienNhanBang equals sv.Id
                    join sp in _tbDbContext.SubPlans on sl.IdSubPlan equals sp.Id
                    where !sl.Deleted && !sv.Deleted && !sp.Deleted
                        && sl.LoaiSlide == LoaiSlides.SINH_VIEN
                        && sp.IdPlan == plan.Id
                        && !string.IsNullOrEmpty(sv.MaSoSinhVien)
                    select sv
                ).ToList();

            foreach (var item in listSv)
            {
                await _generateQrCommon(item, plan);
            }

            _tbDbContext.SaveChanges();
        }

        public async Task GenerateQrOneSv(int idSlide)
        {
            _logger.LogInformation($"{nameof(GenerateQrOneSv)}, idSlide = {idSlide}");

            var svSlide = (
                    from sl in _tbDbContext.Slides
                    join sv in _tbDbContext.DanhSachSinhVienNhanBangs on sl.IdSinhVienNhanBang equals sv.Id
                    join sp in _tbDbContext.SubPlans on sl.IdSubPlan equals sp.Id
                    join p in _tbDbContext.Plans on sp.IdPlan equals p.Id
                    where !sl.Deleted && !sv.Deleted && !sp.Deleted && !p.Deleted
                        && sl.LoaiSlide == LoaiSlides.SINH_VIEN
                        && sl.Id == idSlide
                        && !string.IsNullOrEmpty(sv.MaSoSinhVien)
                    select new { sv, p }
                ).FirstOrDefault()
                ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienNotFound);

            await _generateQrCommon(svSlide.sv, svSlide.p);
            _tbDbContext.SaveChanges();
        }

        public void CreateSlideTextFast(CreateSlideTextFastDto dto)
        {
            _logger.LogInformation($"{nameof(CreateSlideTextFast)} dto={JsonSerializer.Serialize(dto)}");

            var username = getCurrentName();
            if (string.IsNullOrEmpty(dto.NoiDung))
            {
                throw new UserFriendlyException(ErrorCodes.TraoBangErrorLoaiSlideBinhThuongPhaiCoNoiDung);
            }

            // check subplan có đang trao bằng ko
            var subPlan = (from sp in _tbDbContext.SubPlans.AsNoTracking()
                           join p in _tbDbContext.Plans.AsNoTracking() on sp.IdPlan equals p.Id
                           where !sp.Deleted && !p.Deleted
                             && p.TrangThai == TrangThaiPlan.DangHoatDong
                             && sp.TrangThai == TrangThaiSubPlan.DangTraoBang
                             && sp.IsShow
                             && sp.Id == dto.IdSubPlan
                           select sp).FirstOrDefault()
                        ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);

            // check slide trước
            int newOrder = 2;

            if (dto.IdSlideTruoc != null)
            {
                var slideTruoc = _tbDbContext.Slides.AsNoTracking()
                                .Where(x => x.Id == dto.IdSlideTruoc && x.IdSubPlan == subPlan.Id && !x.Deleted)
                                .FirstOrDefault()
                              ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSlideNotFound);

                newOrder = slideTruoc.Order + 1;
            }

            using (var tran = _tbDbContext.Database.BeginTransaction())
            {
                var slideBefore = _tbDbContext.Slides
                                    .Where(x => x.IdSubPlan == subPlan.Id && !x.Deleted && x.Order >= newOrder)
                                    .ExecuteUpdate(setter => setter.SetProperty(x => x.Order, x => x.Order + 1));

                _tbDbContext.Slides.Add(new Slide()
                {
                    IdSinhVienNhanBang = -1,
                    IdSubPlan = subPlan.Id,
                    IsShow = true,
                    NoiDung = dto.NoiDung,
                    LoaiSlide = LoaiSlides.TEXT,
                    Note = dto.Note,
                    TrangThai = TraoBangConstants.ThamGiaTraoBang,
                    Order = newOrder,
                    CreatedBy = username
                });
                _tbDbContext.SaveChanges();
                tran.Commit();
            }
        }

        public void UpdateTienDoOrder(UpdateTienDoOrderDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateTienDoOrder)} dto={JsonSerializer.Serialize(dto)}");

            // check tiến độ có tồn tại ko
            var tienDo = _tbDbContext.TienDoTraoBangs.Where(x => x.Id == dto.IdTienDo && !x.Deleted).FirstOrDefault()
                    ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSinhVienOrderInvalid);

            // check subplan có đang trao bằng ko
            var subPlan = (from sp in _tbDbContext.SubPlans.AsNoTracking()
                           join p in _tbDbContext.Plans.AsNoTracking() on sp.IdPlan equals p.Id
                           where !sp.Deleted && !p.Deleted
                             && p.TrangThai == TrangThaiPlan.DangHoatDong
                             && sp.TrangThai == TrangThaiSubPlan.DangTraoBang
                             && sp.IsShow
                             && sp.Id == tienDo.IdSubPlan
                           select sp).FirstOrDefault()
                        ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorSubPlanNotFound);

            // lấy slide trước
            var slideTruocTrongBangTienDo = _tbDbContext.TienDoTraoBangs.AsNoTracking()
                                .Where(x => x.Id == dto.IdSlideTruoc && x.IdSubPlan == subPlan.Id && !x.Deleted)
                                .FirstOrDefault();
            int newOrder = 1;
            if (slideTruocTrongBangTienDo != null)
            {
                newOrder = slideTruocTrongBangTienDo.Order + 1;
            }

            using (var tran = _tbDbContext.Database.BeginTransaction())
            {
                tienDo.Order = newOrder;
                _tbDbContext.SaveChanges();

                _tbDbContext.TienDoTraoBangs.Where(x => x.IdSubPlan == tienDo.IdSubPlan && x.Order >= newOrder && !x.Deleted && x.Id != tienDo.Id)
                               .ExecuteUpdate(setter => setter.SetProperty(x => x.Order, x => x.Order + 1));

                var listTienDo = _tbDbContext.TienDoTraoBangs.Where(x => x.IdSubPlan == tienDo.IdSubPlan && !x.Deleted).OrderBy(x => x.Order).ToList();
                int order = 1;
                foreach (var item in listTienDo)
                {
                    item.Order = order;
                    order++;
                }
                _tbDbContext.SaveChanges();
                tran.Commit();
            }
        }

        public void RevertTienDoTraoBang(int idTienDo)
        {
            _logger.LogInformation($"{nameof(RevertTienDoTraoBang)} idTienDo={idTienDo}");

            using (var tran = _tbDbContext.Database.BeginTransaction())
            {
                var tienDo = _tbDbContext.TienDoTraoBangs.Where(x => x.Id == idTienDo && !x.Deleted).FirstOrDefault()
                    ?? throw new UserFriendlyException(ErrorCodes.TraoBangErrorTienDoNotFound);

                tienDo.Deleted = true;
                tienDo.DeletedBy = getCurrentName();
                tienDo.DeletedDate = DateTime.Now;

                // Nếu là loại slide text thì xóa cả tiến độ lẫn slide đó
                if (tienDo.LoaiSlide == LoaiSlides.TEXT)
                {
                    var slide = _tbDbContext.Slides.Where(x => x.Id == tienDo.IdSlide && !x.Deleted).FirstOrDefault();
                    if (slide != null)
                    {
                        slide.Deleted = true;
                        slide.DeletedBy = getCurrentName();
                        slide.DeletedDate = DateTime.Now;
                    }
                }

                _tbDbContext.SaveChanges();
                tran.Commit();
            }
        }

        private async Task _generateQrCommon(DanhSachSinhVienNhanBang sv, Plan plan)
        {
            string templateContent = _templateSettings.UrlSvInfo;
            string folder = "QrSinhVien";

            var content = templateContent.Replace("[mssv]", sv.MaSoSinhVien);

            // khoa và lớp nằm trên mã QR, họ tên và mssv nằm dưới, tất cả căn giữa
            string textAbove = $@"{sv.QrTenKhoa}
Lớp: {sv.Lop}";

            string textBelow = $@"{sv.QrHoTen}
MSSV: {sv.MaSoSinhVien}";

            var qrcode = _qrCodeService.GenerateQrWithText(content, textAbove, textBelow);
            string filename = $"{folder}/{sv.MaSoSinhVien}.png";

            // ảnh chỉ có mỗi mã QR, không kèm thông tin gì
            var qrcodeOnly = _qrCodeService.GenQrCodeByText(content);
            string filenameOnly = $"{folder}/{sv.MaSoSinhVien}_only.png";

            try
            {
                _logger.LogInformation($"Sinh qr cho SV mssv = {sv.MaSoSinhVien}");

                var upload = await _fileS3Service.WriteStreamFileAsync(filename, qrcode);
                sv.LinkQR = $"{_fileS3Config.BucketName}/{filename}";

                var uploadOnly = await _fileS3Service.WriteStreamFileAsync(filenameOnly, qrcodeOnly);
                sv.LinkQrOnly = $"{_fileS3Config.BucketName}/{filenameOnly}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Obsolete("Không dùng nữa, giữ lại để tham khảo mẫu ảnh QR có thêm tên đợt, ngành và STT cỡ chữ to")]
        private async Task _generateQrCommon2(DanhSachSinhVienNhanBang sv, Plan plan)
        {
            string templateContent = _templateSettings.UrlSvInfo;
            string folder = "QrSinhVien";

            var content = templateContent.Replace("[mssv]", sv.MaSoSinhVien);

            // khối text bên trái, từ trên xuống, STT để trống điền tay
            float fontSize = 64;
            var info = new List<QrTextLine>
            {
                new QrTextLine { Text = "Mã QR lên nhận bằng tốt nghiệp", FontSize = fontSize },
                new QrTextLine { Text = $"(đợt {plan.Ten})", FontSize = fontSize },
                new QrTextLine { Text = sv.QrHoTen.ToUpper(), FontSize = fontSize * 1.5f },
                new QrTextLine { Text = $"Khoa: {sv.QrTenKhoa}", FontSize = fontSize },
                new QrTextLine { Text = $"Ngành: {sv.TenNganhDaoTao}", FontSize = fontSize },
                new QrTextLine { Text = $"MSSV: {sv.MaSoSinhVien}", FontSize = fontSize },
                new QrTextLine { Text = "STT: ", FontSize = fontSize * 2, FillRemaining = true },
            };

            var qrcode = _qrCodeService.GenerateQrWithText(content, info);
            string filename = $"{folder}/{sv.MaSoSinhVien}.jpg";

            try
            {
                _logger.LogInformation($"Sinh qr cho SV mssv = {sv.MaSoSinhVien}");

                var upload = await _fileS3Service.WriteStreamFileAsync(filename, qrcode);
                sv.LinkQR = $"{_fileS3Config.BucketName}/{filename}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
