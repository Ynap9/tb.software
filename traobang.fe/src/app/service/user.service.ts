
import { IFindPagingUser, IViewRowUser, ICreateUser, IViewUser, IUpdateUser, IChangePassword } from '@/models/auth/user.models';
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class UserService {
    api = '/api/app/users';
    http = inject(HttpClient);

    findPaging(query: IFindPagingUser) {
        return this.http.get<IBaseResponsePaging<IViewRowUser>>(this.api, {
            params: { ...query }
        });
    }

    getById(id: string) {
        return this.http.get<IBaseResponseWithData<IViewRowUser>>(`${this.api}/${id}`);
    }

    create(body: ICreateUser) {
        return this.http.post<IBaseResponseWithData<IViewUser>>(`${this.api}`, body);
    }

    update(body: IUpdateUser) {
        return this.http.put<IBaseResponse>(`${this.api}`, body);
    }

    getMe() {
        return this.http.get<IBaseResponseWithData<IViewUser>>(`${this.api}/me`);
    }

    changePassword(body: IChangePassword) {
        return this.http.put<IBaseResponse>(`${this.api}/change-password`, body);
    }

    delete(id: string) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }

    toggleLock(id: string) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}/toggle-lock`);
    }
}
