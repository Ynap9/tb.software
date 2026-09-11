import {
    IFindPagingSvNhanBang,
    IViewRowSvNhanBang,
    ICreateSvNhanBang,
    IUpdateSvNhanBang,
    IViewScanQrTienDoSv,
    IGetTienDoHangDoi,
    IViewScanQrCurrentSubPlan,
    IViewScanQrSubPlan,
    IViewSvDangTraoBang,
    IViewTienDoTraoBang,
    IViewSubPlanSideScreen,
    IViewSvBatDauLuiResponse,
    IGetTienDoHangDoiSinhVienBatDauLui
} from '@/models/traobang/sv-nhan-bang.models';
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class TraoBangSvService {
    api = '/api/core/trao-bang/sub-plan';
    http = inject(HttpClient);

    findPaging(query: IFindPagingSvNhanBang) {
        return this.http.get<IBaseResponsePaging<IViewRowSvNhanBang>>(`${this.api}/paging-danh-sach-sinh-vien-nhan-bang`, {
            params: { ...query }
        });
    }

    getById(id: string) {
        return this.http.get<IBaseResponseWithData<IViewRowSvNhanBang>>(`${this.api}/${id}`);
    }

    getList() {
        return this.http.get<IBaseResponseWithData<IViewRowSvNhanBang[]>>(`${this.api}/list`);
    }

    create(body: ICreateSvNhanBang) {
        return this.http.post<IBaseResponse>(`${this.api}/sinh-vien-nhan-bang`, body);
    }

    update(body: IUpdateSvNhanBang) {
        return this.http.put<IBaseResponse>(`${this.api}/sinh-vien-nhan-bang`, body);
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }

    checkHangDoi(mssv: string) {
        return this.http.get<IBaseResponseWithData<number | null>>(`${this.api}/sinh-vien-nhan-bang/hang-doi/check?mssv=${mssv}`);
    }

    pushHangDoi(mssv: string) {
        return this.http.post<IBaseResponseWithData<IViewScanQrTienDoSv>>(`${this.api}/sinh-vien-nhan-bang/hang-doi?mssv=${mssv}`, null);
    }

    pushHangDoiTruongHopDacBiet(mssv: string) {
        return this.http.post<IBaseResponseWithData<IViewScanQrTienDoSv>>(`${this.api}/sinh-vien-nhan-bang/hang-doi/truong-hop-dac-biet?mssv=${mssv}`, null);
    }

    getHangDoi(query: IGetTienDoHangDoi) {
        if (!query.SoLuong) {
            query.SoLuong = 5;
        }
        return this.http.get<IBaseResponseWithData<IViewScanQrTienDoSv[]>>(`${this.api}/sinh-vien-nhan-bang/tien-do`, {
            params: { ...query }
        });
    }

    getCurrentSubPlanById(id: number) {
        return this.http.get<IBaseResponseWithData<IViewScanQrCurrentSubPlan>>(`${this.api}/${id}/thong-tin-subplan`);
    }

    getQrListSubPlan(idPlan: number) {
        return this.http.get<IBaseResponseWithData<IViewScanQrSubPlan[]>>(`${this.api}/plan/active/list-sub-plan-infor`);
    }

    backToDaTraoBangSubPlan(idSubPlan: number) {
        return this.http.put<IBaseResponse>(`${this.api}/${idSubPlan}/trang-thai-sub-plan`, null);
    }

    nextSubPlan() {
        return this.http.post<IBaseResponse>(`${this.api}/next-sub-plan`, null);
    }

    getSvDangTraoBang() {
        return this.http.get<IBaseResponseWithData<IViewSvDangTraoBang>>(`${this.api}/infor-sinh-vien-dang-trao`);
    }

    getTienDoTraoBang() {
        return this.http.get<IBaseResponseWithData<IViewTienDoTraoBang>>(`${this.api}/tien-do-trao-bang`);
    }

    nextSvNhanBang(idSubPlan: number) {
        return this.http.post<IBaseResponse>(`${this.api}/${idSubPlan}/sinh-vien-nhan-bang/next-trao-bang`, null);
    }

    prevSvNhanBang(idSubPlan: number) {
        return this.http.post<IBaseResponse>(`${this.api}/${idSubPlan}/sinh-vien-nhan-bang/prev-trao-bang`, null);
    }

    getSvChuanBi(idSubPlan: number) {
        return this.http.get<IBaseResponseWithData<IViewSvDangTraoBang>>(`${this.api}/${idSubPlan}/sinh-vien-nhan-bang/chuan-bi`);
    }

    getSvNhanBangKhoa() {
        return this.http.get<IBaseResponseWithData<IViewSubPlanSideScreen>>(`${this.api}/danh-sach-sinh-vien-nhan-bang-khoa?soLuong=50`);
    }

    getSvBatDauLui(idSubPlan: number) {
        return this.http.post<IBaseResponseWithData<IViewSvBatDauLuiResponse>>(`${this.api}/${idSubPlan}/infor-sinh-vien-prev`, null);
    }

    getHangDoiSinhVienBatDauLui(query: IGetTienDoHangDoiSinhVienBatDauLui) {
        return this.http.get<IBaseResponseWithData<IViewScanQrTienDoSv[]>>(`${this.api}/sinh-vien-prev/tien-do`, {
            params: { ...query }
        });
    }

    cutSlideThuong(id: number) {
        return this.http.post<IBaseResponseWithData<any>>(`${this.api}/sinh-vien-nhan-bang/hang-doi/slide-thuong`, { idSubPlan: id });
    }
}
