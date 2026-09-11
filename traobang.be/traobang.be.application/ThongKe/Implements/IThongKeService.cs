using traobang.be.application.ThongKe.Dtos;

namespace traobang.be.application.ThongKe.Implements
{
    public interface IThongKeService
    {
        /// <summary>
        /// Lấy toàn bộ khoa của plan đang active, mỗi khoa kèm danh sách sinh viên nhận bằng
        /// </summary>
        /// <param name="soLuongSinhVien">số sinh viên trả về cho mỗi khoa: 0 là không kèm sinh viên, số âm là lấy hết, số dương là lấy tối đa bấy nhiêu. Các số đếm luôn tính trên toàn bộ khoa.</param>
        List<KhoaDtos> FindAllKhoa(int soLuongSinhVien = 0);

        /// <summary>
        /// Lấy hàng chờ trao bằng của plan đang active, xếp theo đúng thứ tự lên nhận bằng:
        /// thứ tự khoa trước, trong mỗi khoa theo thứ tự hàng chờ
        /// </summary>
        HangDoiTraoBangDto FindAllSinhVien();

        /// <summary>
        /// Lấy chi tiết một khoa của plan đang active: toàn bộ sinh viên trong danh sách
        /// nhận bằng kèm trạng thái đã lên nhận bằng hay chưa
        /// </summary>
        /// <param name="idKhoa">id của SubPlan</param>
        ChiTietKhoaDto FindChiTietKhoa(int idKhoa);
    }
}
