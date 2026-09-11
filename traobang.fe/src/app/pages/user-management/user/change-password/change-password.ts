
import { IChangePassword } from '@/models/auth/user.models';
import { UserService } from '@/service/user.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { SharedImports } from '@/shared/import.shared';
import { Component, inject } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-change-password',
    imports: [SharedImports],
    templateUrl: './change-password.html',
    styleUrl: './change-password.scss'
})
export class ChangePassword extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _userService = inject(UserService);

    override form: FormGroup = new FormGroup(
        {
            currentPassword: new FormControl('', [Validators.required]),
            newPassword: new FormControl('', [Validators.required, Validators.minLength(6)]),
            confirmPassword: new FormControl('', [Validators.required])
        },
        (group: AbstractControl): ValidationErrors | null => {
            return group.get('newPassword')?.value === group.get('confirmPassword')?.value ? null : { passwordNotMatch: true };
        }
    );

    override ValidationMessages: Record<string, Record<string, string>> = {
        currentPassword: {
            required: 'Không được bỏ trống'
        },
        newPassword: {
            required: 'Không được bỏ trống',
            minlength: 'Mật khẩu tối thiểu 6 ký tự'
        },
        confirmPassword: {
            required: 'Không được bỏ trống'
        }
    };

    onSubmit() {
        if (this.isFormInvalid()) {
            return;
        }

        const body: IChangePassword = {
            currentPassword: this.form.value['currentPassword'],
            newPassword: this.form.value['newPassword']
        };

        this.loading = true;
        this._userService.changePassword(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã đổi mật khẩu')) {
                    this._ref?.close(true);
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
