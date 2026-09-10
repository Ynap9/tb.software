import { IThongKeChiTietKhoa, IThongKeHangDoi, IThongKeKhoa } from '@/models/traobang/thong-ke.models';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ThongKeService {
    api = '/api/core/thong-ke';
    http = inject(HttpClient);

    /**
     * Lấy toàn bộ khoa của plan đang active, mỗi khoa kèm danh sách sinh viên nhận bằng.
     * soLuongSinhVien giới hạn số sinh viên trả về cho mỗi khoa, 0 là lấy hết.
     */
    getAllKhoa(soLuongSinhVien = 0) {
        return this.http.get<IBaseResponseWithData<IThongKeKhoa[]>>(`${this.api}/khoa`, {
            params: { soLuongSinhVien }
        });
    }

    /** Lấy hàng chờ trao bằng của plan đang active, xếp theo thứ tự lên nhận bằng */
    getHangDoiSinhVien() {
        return this.http.get<IBaseResponseWithData<IThongKeHangDoi>>(`${this.api}/sinh-vien`);
    }

    /** Lấy chi tiết một khoa kèm toàn bộ sinh viên và trạng thái đã lên nhận bằng hay chưa */
    getChiTietKhoa(idKhoa: number) {
        return this.http.get<IBaseResponseWithData<IThongKeChiTietKhoa>>(`${this.api}/khoa/${idKhoa}`);
    }
}
