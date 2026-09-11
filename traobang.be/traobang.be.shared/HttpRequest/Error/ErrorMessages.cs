namespace traobang.be.shared.HttpRequest.Error
{
    public static class ErrorMessages
    {
        private static readonly Dictionary<int, string> _messages = new()
        {
            { ErrorCodes.System, "Lỗi hệ thống" },
            { ErrorCodes.InternalServerError, "Lỗi server" },
            { ErrorCodes.BadRequest, "Request không hợp lệ" },
            { ErrorCodes.NotFound, "Không tìm thấy trong hệ thống" },
            { ErrorCodes.Unauthorized, "Không được phân quyền" },
            { ErrorCodes.AuthErrorUserNotFound, "User không tồn tại" },
            { ErrorCodes.AuthErrorRoleNotFound, "Role không tồn tại" },
            { ErrorCodes.AuthInvalidPassword, "Mật khẩu không đúng" },
            { ErrorCodes.AuthErrorCreateUser, "Lỗi tạo user" },
            { ErrorCodes.AuthErrorCreateRole, "Lỗi tạo role" },
            { ErrorCodes.ImportExcelSheetErrorNotFound, "Không tìm thấy sheet '{0}' trong file Excel" },
            { ErrorCodes.Found, "Đã tồn tại trong hệ thống" },
            { ErrorCodes.GoogleSheetUrlErrorInvalid, "URL Google Sheet không hợp lệ hoặc không thể truy cập được" },
            { ErrorCodes.ErrorNoPermissionAccessGoogleSheet,"Không có quyền truy cập vào Google Sheet" },
            { ErrorCodes.ErrorServiceAccountNotFoundInAppSetting,"Không tìm thấy config đường dẫn của service-account trong appsetting.json" },
            { ErrorCodes.ErrorServiceAccountNotFound,"Không tim thấy file service-account.json" },
            { ErrorCodes.TraoBangErrorPlanNotFound, "Kế hoạch không tồn tại" },
            { ErrorCodes.TraoBangErrorSubPlanNotFound, "Kế hoạch con không tồn tại" },
            { ErrorCodes.TraoBangErrorSubPlanOrderInvalid,"Thứ tự kế hoạch con không hợp lệ"},
            { ErrorCodes.TraoBangErrorSinhVienDaTonTai,"Sinh viên trong danh sách nhận bằng đã tồn tại" },
            { ErrorCodes.TraoBangErrorSinhVienNotFound,"Sinh viên không tồn tại trong danh sách nhận bằng" },
            { ErrorCodes.TraoBangErrorSinhVienOrderInvalid, "Số thự tự của sinh viên không hợp lệ" },
            { ErrorCodes.TraoBangErrorSinhVienDaTonTaiTrongHangDoi,"Sinh viên nhận bằng đã tồn tại trong hàng đợi" },
            { ErrorCodes.TraoBangErrorSinhVienTraoBangNotFound,"Sinh viên đang trao bằng không tồn tại" },
            { ErrorCodes.TraoBangErrorSinhVienTraoBangKhongThuocKhoaDangTrao,"Sinh viên đang quét không thuộc khoa đang trao bằng" },
            { ErrorCodes.ImportHeaderErrorInvalid, "Header không đúng định dạng tại dòng {0}" },
            { ErrorCodes.TraoBangErrorLoaiSlideBinhThuongPhaiCoNoiDung, "Không được bỏ trống nội dung" },
            { ErrorCodes.TraoBangErrorLoaiSlideSinhVienPhaiCoSinhVien, "Phải nhập thông tin sinh viên" },
            { ErrorCodes.TraoBangErrorSlideNotFound, "Không tìm thấy slide" },
            { ErrorCodes.TraoBangErrorGiaoDienNotFound, "Không tìm thấy giao diện" },
            { ErrorCodes.TraoBangErrorCannotDeleteActivePlan, "Không xóa được chương trình đang hoạt động" },
            { ErrorCodes.TraoBangErrorSubPlanNotActive, "Không phải khoa đang trao bằng" },
            { ErrorCodes.TraoBangErrorTienDoNotFound, "Không tìm thấy slide trong hàng đợi" },
            { ErrorCodes.AuthErrorRoleInUsed, "Role đang được sử dụng. Không thể xóa." },
            { ErrorCodes.AuthErrorCannotDeleteSuperAdmin, "Không thể xóa Super Admin." },
            { ErrorCodes.AuthErrorCannotDeleteCurrentUser, "Không thể xóa người dùng hiện tại đang đăng nhập." },
            { ErrorCodes.AuthErrorChangePassword, "Đổi mật khẩu không thành công" },
        };
        public static string GetMessage(int code)
        {
            return _messages.TryGetValue(code, out var message) ? message : "Unknown error.";
        }
    }

}
