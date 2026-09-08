import { SharedImports } from '@/shared/import.shared';
import { GiaoDienService } from '@/service/giao-dien.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Breadcrumb } from '@/shared/components/breadcrumb/breadcrumb';
import { MenuItem } from 'primeng/api';
import { IViewGiaoDien } from '@/models/traobang/giao-dien.models';

@Component({
    selector: 'app-create-giao-dien',
    imports: [SharedImports, Breadcrumb],
    templateUrl: './create-giao-dien.html',
    styleUrl: './create-giao-dien.scss'
})
export class CreateGiaoDien extends BaseComponent {
    private _giaoDienServices = inject(GiaoDienService);

    items: MenuItem[] = [{ label: 'Danh sách giao diện', routerLink: 'trao-bang/config/giao-dien' }, { label: 'Giao diện' }];
    home: MenuItem = { icon: 'pi pi-home', routerLink: '/' };

    idGiaoDien!: any;
    giaoDien!: IViewGiaoDien;
    submitted: boolean = false;
    isPlan: any = false;

    override form: FormGroup = new FormGroup({
        tenGiaoDien: new FormControl(null, [Validators.required]),
        moTa: new FormControl(''),
        html: new FormControl(''),
        css: new FormControl(''),
        js: new FormControl(''),
        noiDung: new FormControl('')
    });

    override ValidationMessages: Record<string, Record<string, string>> = {
        tenGiaoDien: {
            required: 'Không được bỏ trống'
        }
    };

    override ngOnInit() {
        if (!this.idGiaoDien) {
            this._activatedRoute.queryParamMap.subscribe((params) => {
                const id = params.get('id');
                this.isPlan = params.get('isPlan');
                if (id) {
                    this.idGiaoDien = id;
                }
            });
        }
        this.getGiaoDienById();
    }

    getGiaoDienById() {
        if (this.idGiaoDien) {
            this.loading = true;
            this._giaoDienServices
                .getById(this.idGiaoDien)
                .subscribe({
                    next: (res) => {
                        if (this.isResponseSucceed(res, false)) {
                            this.giaoDien = res.data;
                            this.form.patchValue({
                                tenGiaoDien: this.giaoDien.tenGiaoDien,
                                moTa: this.giaoDien.moTa ?? '',
                                html: this.giaoDien.html ?? '',
                                css: this.giaoDien.css ?? '',
                                js: this.giaoDien.js ?? '',
                                noiDung: this.giaoDien.noiDung ?? ''
                            });
                        }
                    }
                })
                .add(() => {
                    this.loading = false;
                });
        }
    }

    saveTemplate() {
        if (this.isFormInvalid()) {
            this.submitted = true;
            return;
        }
        this.loading = true;

        const body: any = {
            tenGiaoDien: this.form.get('tenGiaoDien')?.value,
            moTa: this.form.get('moTa')?.value ?? '',
            noiDung: this.form.get('noiDung')?.value ?? '',
            html: this.form.get('html')?.value ?? '',
            css: this.form.get('css')?.value ?? '',
            js: this.form.get('js')?.value ?? ''
        };
        if (this.idGiaoDien) {
            body.id = this.idGiaoDien;
        }

        if (this.idGiaoDien) {
            this._giaoDienServices.update(body).subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, true, 'Cập nhật giao diện thành công')) {
                        this.ngOnInit();
                    }
                },
                error: (err) => {
                    this.messageError(err?.message);
                },
                complete: () => {
                    this.loading = false;
                }
            });
        } else {
            this._giaoDienServices.create(body).subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, true, 'Thêm mới giao diện thành công')) {
                        this.idGiaoDien = res.data.id;
                        this.ngOnInit();
                    }
                },
                error: (err) => {
                    this.messageError(err?.message);
                },
                complete: () => {
                    this.loading = false;
                }
            });
        }
    }
}
