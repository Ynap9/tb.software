namespace traobang.be.shared.HttpRequest.Error
{
    public static class ErrorCodes
    {
        //Các mã lỗi căn bản
        public const int System = 1;
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int NotFound = 404;
        public const int Found = 409;
        public const int InternalServerError = 500;

        public const int AuthInvalidPassword = 101;
        public const int AuthErrorCreateUser = 102;
        public const int AuthErrorUserNotFound = 103;
        public const int AuthErrorCreateRole = 104;
        public const int AuthErrorRoleNotFound = 105;
        public const int AuthErrorUserEmailHuceNotFound = 106;
        public const int AuthErrorRoleInUsed = 107;
        public const int AuthErrorCannotDeleteSuperAdmin = 108;
        public const int AuthErrorCannotDeleteCurrentUser = 109;
        public const int AuthErrorChangePassword = 110;

        public const int ServiceAccountErrorNotFound = 701;
        public const int GoogleSheetUrlErrorInvalid = 702;

        public const int ImportHeaderErrorInvalid = 801;
        public const int ImportExcelSheetErrorNotFound = 809;
        public const int ErrorNoPermissionAccessGoogleSheet = 901;
        public const int ErrorServiceAccountNotFoundInAppSetting = 902;
        public const int ErrorServiceAccountNotFound = 903;

        public const int TraoBangErrorPlanNotFound = 1001;
        public const int TraoBangErrorSubPlanNotFound = 1002;
        public const int TraoBangErrorSubPlanOrderInvalid = 1003;
        public const int TraoBangErrorSinhVienDaTonTai = 1004;
        public const int TraoBangErrorSinhVienNotFound = 1005;
        public const int TraoBangErrorSinhVienOrderInvalid = 1006;
        public const int TraoBangErrorSinhVienDaTonTaiTrongHangDoi = 1007;
        public const int TraoBangErrorSinhVienTraoBangNotFound = 1008;
        public const int TraoBangErrorSinhVienTraoBangKhongThuocKhoaDangTrao = 1009;
        public const int TraoBangErrorLoaiSlideBinhThuongPhaiCoNoiDung = 1010;
        public const int TraoBangErrorLoaiSlideSinhVienPhaiCoSinhVien = 1011;
        public const int TraoBangErrorSlideNotFound = 1012;
        public const int TraoBangErrorCannotDeleteActivePlan = 1013;
        public const int TraoBangErrorSubPlanNotActive = 1014;
        public const int TraoBangErrorTienDoNotFound = 1015;

        public const int TraoBangErrorGiaoDienNotFound = 1101;
    }
}
