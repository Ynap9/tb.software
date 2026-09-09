using traobang.be.application.TraoBang.Dtos;
using traobang.be.application.TraoBang.Dtos.SubPlan;
using traobang.be.shared.HttpRequest.BaseRequest;

namespace traobang.be.application.TraoBang.Interface
{
    public interface ISubPlanService
    {
        public void Create(int idPlan, CreateSubPlanDto dto);
        public Task Update(UpdateSubPlanDto dto);
        public BaseResponsePagingDto<ViewSubPlanDto> FindPaging(FindPagingSubPlanDto dto);
        public void UpdateIsShow(UpdateSubPlanIsShowDto dto);
        public void Delete(int idPlan, int idSubPlan);
        public Task<List<UpdateOrderSubPlanResponseDto>> MoveOrder(MoveOrderSubPlanDto dto);
        public Task<List<GetListSubPlanResponseDto>> GetListSubPlan(int idPlan);
        public Task<ImportDanhSachSinhVienNhanBangResponseDto> ImportDanhSachNhanBang(ImportDanhSachSinhVienNhanBangDto dto);
        public BaseResponsePagingDto<ViewSinhVienNhanBangDto> PagingSinhVienNhanBang(FindPagingSinhVienNhanBangDto dto);
        public void CreateSinhVienNhanBang(CreateSinhVienNhanBangDto dto);
        public Task UpdateSinhVienNhanBang(UpdateSinhVienNhanBangDto dto);
        public void DeleteSinhVienNhanBang(int idSubPlan, int id);
        public Task<ViewSinhVienNhanBangDto> ShowSinhVienNhanBangInfor(string mssv);
        public Task<ViewSinhVienNhanBangDto> NextSinhVienNhanBang(string mssv);
        public Task<ViewSinhVienNhanBangDto> PreviousSinhVienNhanBang(string mssv);
        public Task<DiemDanhNhanBangDto> DiemDanhNhanBang(string mssv);
        /// <summary>
        /// Trạng thái của sinh viên trong hàng đợi, chưa có trong hàng đợi thì trả về null
        /// </summary>
        /// <param name="mssv"></param>
        /// <returns></returns>
        public int? CheckSinhVienTrongHangDoi(string mssv);
        public Task<List<ViewTienDoNhanBangResponseDto>> GetTienDoNhanBang(ViewTienDoNhanBangRequestDto dto);
        public Task<GetInforSubPlanDto> GetInforSubPlan(int idSubPlan);
        public Task UpdateTrangThaiSubPlan(int id);
        public Task<GetNextSubPlanResponseDto?> NextSubPlan();
        /// <summary>
        /// Lấy danh sách subplan của plan active
        /// </summary>
        /// <returns></returns>
        public Task<List<GetListSubPlanDto>> GetListSubPlanInfor();
        public Task UpdateTrangThaiSinhVienNhanBang(int idSubPlan, int id);
        public Task<GetSinhVienDangTraoBangInforDto> GetSinhVienDangTraoBang();
        public Task<GetTienDoTraoBangResponseDto> GetTienDoTraoBang();
        public Task<GetSinhVienDangTraoBangInforDto> NextSinhVienTraoBang(int idSubPlan);

        public Task<DiemDanhNhanBangDto> CutSlideNormal(int idSubPlan);

        public Task<GetSinhVienDangTraoBangInforDto?> PrevSinhVienTraoBang(int idSubPlan);
        public Task<GetInforSinhVienChuanBiDuocTraoBangResponseDto?> GetInforSinhVienChuanBiDuocTraoBang(int idSubPlan);
        public Task<GetInforSubPlanDangTraoResponseDto> GetInforSubPlanDangTrao(int SoLuong);
        public Task<DiemDanhNhanBangDto> DiemDanhNhanBangTruongHopDacBiet(string mssv);
        public Task Restart();
        public Task<GetInforSinhVienBatDauDuocPrevResponseDto?> GetInforSinhVienBatDauDuocPrev(int idSubPlan);
        public Task<List<ViewTienDoNhanBangResponseDto>> GetTienDoNhanBangSinhVienBatDauPrev(ViewTienDoNhanBangSinhVienBatDauLuiRequestDto dto);

        /// <summary>
        /// Xuất mẫu excel template import khoa
        /// </summary>
        /// <returns></returns>
        public byte[] DownloadTemplateImport();

        /// <summary>
        /// Import excel khoa
        /// </summary>
        /// <param name="dto"></param>
        public void ImportExcelSubplan(ImportExcelSubPlanDto dto);
    }
}
