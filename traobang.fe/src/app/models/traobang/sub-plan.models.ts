import { IBaseRequestPaging } from "@/shared/models/request-paging.base.models";

export interface IViewRowConfigSubPlan {
    id?: number;
    idPlan?: number;
    ten?: string;
    truongKhoa?: string;
    qrTenKhoa?: string;
    moTa?: string | null;
    note?: string | null;
    moBai?: string;
    ketBai?: string;
    order?: number;
    isShow?: boolean;
    createdDate?: string;
}

export interface IFindPagingConfigSubPlan extends IBaseRequestPaging {}

export interface ICreateConfigSubPlan {
    idPlan: number;
    ten: string;
    truongKhoa: string;
    qrTenKhoa: string;
    moTa: string | null;
    note: string | null;
    moBai: string;
    ketBai: string;
    order: number;
    isShow: boolean;
    isShowMoBai: boolean;
    isShowKetBai: boolean;
}

export interface IUpdateConfigSubPlan extends ICreateConfigSubPlan {
    idSubPlan: number;
    newOrder: number;
}