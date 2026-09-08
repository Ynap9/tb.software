
import { IViewSvDangTraoBang } from '@/models/traobang/sv-nhan-bang.models';
import { TraoBangSvService } from '@/service/sv-nhan-bang.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { TraoBangHubConst } from '@/shared/constants/sv-nhan-bang.constants';
import { Component, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-main-screen-bak',
  imports: [],
  templateUrl: './main-screen-bak.html',
  styleUrl: './main-screen-bak.scss'
})
export class MainScreenBak extends BaseComponent {

  _svTraoBangService = inject(TraoBangSvService);
  svDangTrao: IViewSvDangTraoBang = {};
  hubConnection: signalR.HubConnection | undefined;

  override ngOnInit(): void {
    this.getSvDangTrao();
    this.connectHub();
  }

  getSvDangTrao() {
    this._svTraoBangService.getSvDangTraoBang().subscribe({
      next: res => {
        if (this.isResponseSucceed(res)) {
          this.svDangTrao = res.data
        }
      }
    })
  }

  connectHub() {
    const hubUrl = TraoBangHubConst.HUB;
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on(TraoBangHubConst.ReceiveSinhVienDangTrao, (...args) => {
      //const idSubPlan = args[0];

      //if (!idSubPlan) return;

      this.getSvDangTrao();
      // this.initData();
    });

    this.hubConnection.on(TraoBangHubConst.ReceiveChonKhoa, (...args) => {
      //const idSubPlan = args[0];

      //if (!idSubPlan) return;

      this.getSvDangTrao();
      // this.initData();
    });

    this.hubConnection.onreconnected(() => {
      this.getSvDangTrao();
    });

    this.hubConnection.start().then();
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop().then();
  }
}
