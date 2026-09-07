import { IBaseRequestPaging } from "@/shared/models/request-paging.base.models";

export interface SinhVien {
    hoVaTen?: string;
    email?: string;
    emailSinhVien?: string;
    maSoSinhVien?: string;
    lop?: string;
    ngaySinh?: Date;
    capBang?: string;
    tenNganhDaoTao?: string;
    xepHang?: string;
    thanhTich?: string;
    khoaQuanLy?: string;
    soQuyetDinhTotNghiep?: string;
    ngayQuyetDinh?: Date;
    note?: string;
    linkQR?: string;
    linkQrOnly?: string;
    id?: number;
}
export interface IViewRowSlide {
    id: number;
    idSubPlan?: number;
    loaiSlide?: number;
    idSinhVienNhanBang?: number;
    noiDung?: string;
    note?: string;
    trangThai?: number;
    isShow?: boolean;
    sinhVien?: SinhVien;
    loaiSlideName?: string;
    trangThaiText?: string;
}
export interface ICreateSlide {
    idSubPlan?: number;
    loaiSlide?: number;
    idSinhVienNhanBang?: number;
    noiDung?: string;
    note?: string;
    trangThai?: number;
    isShow?: boolean;
    sinhVien?: SinhVien;
}

export interface IUpdateSlide extends ICreateSlide {
    id: string
}

export interface IFindPagingSlide extends IBaseRequestPaging { }

export enum ELoaiSlide {
    TEXT = 1,
    SINH_VIEN = 2,
}

export const listLoaiSlide = [
    {
        code: 1,
        name: 'Text'
    },
    {
        code: 2,
        name: 'Sinh viên'
    }
];
