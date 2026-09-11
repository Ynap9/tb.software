import { GenQrCode } from './../pages/trao-bang/cau-hinh/slide/gen-qr-code/gen-qr-code';
import { inject, Injectable } from '@angular/core';
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { ICreateSlide, IFindPagingSlide, IUpdateSlide, IViewRowSlide } from '@/models/traobang/slide.models';
@Injectable({
    providedIn: 'root'
})
export class SlideDragDropService {
    api = '/api/core/trao-bang/slide';
    http = inject(HttpClient);

    onChangeOrderSlideStudent(data: any) {
        return this.http.put<IBaseResponse>(`${this.api}/tien-do/order`, data);
    }

    createFastSlide(data: any) {
        return this.http.post<IBaseResponse>(`${this.api}/text/fast`, data);
    }

    onModeTest(id: any, isCheckinFull: boolean) {
        const body = {
            idPlan: id,
            isCheckinFull: isCheckinFull
        }
        return this.http.post<IBaseResponse>(`/api/core/trao-bang/prepare/demo`, body);
    }

    changeStatus(id: any) {
        return this.http.put<any>(`${this.api}/tien-do/revert/${id}`,{});
    }
}
