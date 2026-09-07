import { GenQrCode } from './../pages/trao-bang/cau-hinh/slide/gen-qr-code/gen-qr-code';
import { inject, Injectable } from '@angular/core';
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { ICreateSlide, IFindPagingSlide, IUpdateSlide, IViewRowSlide } from '@/models/traobang/slide.models';
@Injectable({
    providedIn: 'root'
})
export class SlideService {
    api = '/api/config/slide';
    http = inject(HttpClient);

    findPaging(query: IFindPagingSlide, dataFilter?: any) {
        return this.http.get<IBaseResponsePaging<IViewRowSlide>>(this.api, {
            params: { ...query, ...dataFilter }
        });
    }

    getById(id: string) {
        return this.http.get<IBaseResponseWithData<IViewRowSlide>>(`${this.api}/${id}`);
    }

    getList() {
        return this.http.get<IBaseResponseWithData<IViewRowSlide[]>>(`${this.api}/list`);
    }

    create(body: ICreateSlide) {
        return this.http.post<IBaseResponse>(`${this.api}`, body);
    }

    update(body: IUpdateSlide) {
        return this.http.put<IBaseResponse>(`${this.api}`, body);
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }

    downloadFileTemplate() {
        return this.http.get(`${this.api}/export/template-import-slide`, { responseType: 'blob' });
    }

    exportSlideSinhVien(dataFilter?: any) {
        return this.http.get(`${this.api}/export/slide-sinh-vien`, {
            params: { ...dataFilter },
            responseType: 'blob'
        });
    }

    uploadFile(body: any) {
        return this.http.post<IBaseResponse>(`${this.api}/import/slide`, body);
    }

    genQrCodeBySlide(body: any) {
        return this.http.post<IBaseResponse>(`${this.api}/qr`, body);
    }

    genQrCode(id: number) {
        return this.http.post<IBaseResponse>(`${this.api}/qr/slide/${id}`, {});
    }
}
