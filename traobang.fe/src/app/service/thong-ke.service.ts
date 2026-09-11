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
     * Lấy toàn bộ khoa của plan đang active, xếp theo thứ tự lên nhận bằng.
     * soLuongSinhVien: 0 là không kèm sinh viên, số dương là lấy tối đa bấy nhiêu mỗi khoa,
     * số âm là lấy hết. Các số đếm luôn tính trên toàn bộ khoa.
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
