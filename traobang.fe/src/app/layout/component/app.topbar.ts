import { Component, inject } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StyleClassModule } from 'primeng/styleclass';
import { AppConfigurator } from './app.configurator';
import { LayoutService } from '../service/layout.service';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '@/service/auth.service';
import { PopoverModule } from 'primeng/popover';
import { DialogService } from 'primeng/dynamicdialog';
import { ChangePassword } from '@/pages/user-management/user/change-password/change-password';

@Component({
    selector: 'app-topbar',
    standalone: true,
    imports: [RouterModule, CommonModule, StyleClassModule, AppConfigurator, MenuModule, ButtonModule, PopoverModule],
    template: ` <div class="layout-topbar">
        <div class="layout-topbar-logo-container">
            <button class="layout-menu-button layout-topbar-action" (click)="layoutService.onMenuToggle()">
                <i class="pi pi-bars"></i>
            </button>
            <a class="layout-topbar-logo flex items-center gap-3 text-white hover:text-gray-200 transition-colors duration-200" routerLink="/">
                <!-- <img src="traobang_icon.png" class="h-8 w-8" /> -->
                <span class="font-semibold text-lg">TRAO BẰNG</span>
            </a>
        </div>

        <div class="layout-topbar-actions">
            <div class="layout-config-menu">
                <button type="button" class="layout-topbar-action" (click)="op.toggle($event)">
                    <i class="pi pi-th-large"></i>
                </button>

                <!--  -->
                <p-popover #op>
                    <div class="">
                        <div class="grid grid-cols-3 gap-4 max-h-[400px] overflow-y-auto">
                            <div *ngFor="let module of iconModules">
                                <div
                                    [ngClass]="{ 'hover:bg-gray-400': layoutService.isDarkTheme(), 'hover:bg-gray-100': !layoutService.isDarkTheme() }"
                                    class="col-span-1 flex flex-col justify-center items-center p-4 h-24 hover:cursor-pointer  border border-transparent rounded-xl"
                                    (click)="module.action(); op.hide()"
                                >
                                    <i [ngStyle]="module.color" style="font-size: 1.6rem " [ngClass]="module.icon" class="text-6xl text-primary mb-3 "></i>
                                    <p class="text-sm font-medium text-center truncate ">{{ module.label }}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </p-popover>
                <!--  -->

                <button type="button" class="layout-topbar-action" (click)="toggleDarkMode()">
                    <i [ngClass]="{ 'pi ': true, 'pi-moon': layoutService.isDarkTheme(), 'pi-sun': !layoutService.isDarkTheme() }"></i>
                </button>
                <div class="relative">
                    <button
                        class="layout-topbar-action layout-topbar-action-highlight"
                        pStyleClass="@next"
                        enterFromClass="hidden"
                        enterActiveClass="animate-scalein"
                        leaveToClass="hidden"
                        leaveActiveClass="animate-fadeout"
                        [hideOnOutsideClick]="true"
                    >
                        <i class="pi pi-palette"></i>
                    </button>
                    <app-configurator />
                </div>
            </div>

            <button class="layout-topbar-menu-button layout-topbar-action" pStyleClass="@next" enterFromClass="hidden" enterActiveClass="animate-scalein" leaveToClass="hidden" leaveActiveClass="animate-fadeout" [hideOnOutsideClick]="true">
                <i class="pi pi-ellipsis-v"></i>
            </button>

            <div class="layout-topbar-menu hidden lg:block">
                <div class="layout-topbar-menu-content">
                    <!-- <button type="button" class="layout-topbar-action">
                        <i class="pi pi-calendar"></i>
                        <span>Calendar</span>
                    </button>
                    <button type="button" class="layout-topbar-action">
                        <i class="pi pi-inbox"></i>
                        <span>Messages</span>
                    </button> -->
                    <!-- <button type="button" class="layout-topbar-action">
                        <i class="pi pi-user"></i>
                        <span>Profile</span>
                    </button> -->

                    <button type="button" (click)="menu.toggle($event)" class="layout-topbar-action">
                        <i class="pi pi-user"></i>
                    </button>
                    <p-menu #menu [popup]="true" [model]="overlayMenuItems"></p-menu>
                </div>
            </div>
        </div>
    </div>`
})
export class AppTopbar {
    _authService = inject(AuthService);
    _dialogService = inject(DialogService);

    items!: MenuItem[];
    overlayMenuItems: MenuItem[] = [
        {
            label: 'Tài khoản',
            icon: 'pi pi-user',
            command: () => {}
        },
        {
            label: 'Đổi mật khẩu',
            icon: 'pi pi-key',
            command: () => {
                this.onOpenChangePassword();
            }
        },
        {
            separator: true
        },
        {
            label: 'Đăng xuất',
            icon: 'pi pi-sign-out',
            command: () => {
                this._authService.logout();
            }
        }
    ];

    iconModules: any = [
        {
            label: 'Trao bằng',
            icon: 'pi pi-trophy',
            color: { color: 'gold' },
            url: 'https://traobang.huce.edu.vn/',
            action: () => window.open('https://traobang.huce.edu.vn/', '_blank')
        },
        {
            label: 'Sơ yếu lý lịch',
            icon: 'pi pi-id-card',
            color: {},
            url: 'https://syll.huce.edu.vn/',
            action: () => window.open('https://syll.huce.edu.vn/', '_blank')
        },
        {
            label: 'SMS',
            icon: 'pi pi-send',
            color: { color: 'black' },
            url: 'https://sms.huce.edu.vn/',
            action: () => window.open('https://sms.huce.edu.vn/', '_blank')
        },
    ];

    constructor(public layoutService: LayoutService) {}

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }

    onOpenChangePassword() {
        this._dialogService.open(ChangePassword, { header: 'Đổi mật khẩu', closable: true, modal: true, styleClass: 'w-[450px]', focusOnShow: false });
    }
}
