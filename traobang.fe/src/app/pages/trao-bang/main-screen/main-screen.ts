
import { IViewGiaoDien } from '@/models/traobang/giao-dien.models';
import { IViewSvDangTraoBang } from '@/models/traobang/sv-nhan-bang.models';
import { GiaoDienService } from '@/service/giao-dien.service';
import { TraoBangSvService } from '@/service/sv-nhan-bang.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { TraoBangHubConst } from '@/shared/constants/sv-nhan-bang.constants';
import { Component, ElementRef, inject, ViewChild } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-main-screen',
  imports: [],
  templateUrl: './main-screen.html',
  styleUrl: './main-screen.scss'
})
export class MainScreen extends BaseComponent {

  static readonly STYLE_ID = 'giao-dien-style';

  _svTraoBangService = inject(TraoBangSvService);
  _giaoDienService = inject(GiaoDienService);
  svDangTrao: IViewSvDangTraoBang = {};
  giaoDien!: IViewGiaoDien;
  hubConnection: signalR.HubConnection | undefined;

  private container?: ElementRef<HTMLElement>;
  private daRenderTemplate = false;
  private daChayScript = false;

  @ViewChild('giaoDienContainer')
  set giaoDienContainer(el: ElementRef<HTMLElement> | undefined) {
    this.container = el;
    if (el) {
      this.renderTemplate();
    }
  }

  override ngOnInit(): void {
    this.getGiaoDien();
    this.getSvDangTrao();
    this.connectHub();
  }

  getGiaoDien() {
    this._giaoDienService.getGiaoDienActive().subscribe({
      next: res => {
        if (this.isResponseSucceed(res, false)) {
          this.giaoDien = res.data;
        }
      }
    })
  }

  getSvDangTrao() {
    this._svTraoBangService.getSvDangTraoBang().subscribe({
      next: res => {
        if (this.isResponseSucceed(res)) {
          this.svDangTrao = res.data
          this.applyData();
        }
      }
    })
  }

  /**
   * Nạp template từ DB vào DOM. Chỉ chạy một lần: các lần sinh viên đổi
   * sau đó chỉ cập nhật giá trị qua applyData() nên màn chiếu không bị nháy.
   */
  renderTemplate() {
    if (this.daRenderTemplate || !this.container || !this.giaoDien?.html) return;

    this.renderCss();
    this.container.nativeElement.innerHTML = this.compileHtml(this.giaoDien.html);
    this.daRenderTemplate = true;

    this.applyData();
    this.runScript();
  }

  /**
   * Đổi {{ svDangTrao.hoVaTen }} thành <span data-bind="svDangTrao.hoVaTen"></span>
   * để về sau chỉ phải gán textContent.
   */
  compileHtml(html: string): string {
    return html.replace(/\{\{([^}]+)\}\}/g, (_match, bieuThuc: string) => {
      const path = bieuThuc.replace(/<[^>]*>/g, '').trim();
      return `<span data-bind="${path}"></span>`;
    });
  }

  renderCss() {
    if (!this.giaoDien?.css) return;

    let styleElement = document.getElementById(MainScreen.STYLE_ID) as HTMLStyleElement;
    if (!styleElement) {
      styleElement = document.createElement('style');
      styleElement.id = MainScreen.STYLE_ID;
      document.head.appendChild(styleElement);
    }
    styleElement.innerHTML = this.giaoDien.css;
  }

  runScript() {
    if (this.daChayScript || !this.giaoDien?.js) return;

    this.daChayScript = true;
    try {
      const scriptFunction = new Function(this.giaoDien.js);
      scriptFunction();
    } catch (error) {
      console.error('Error executing giao dien script:', error);
    }
  }

  /**
   * Gán dữ liệu sinh viên vào template đã nạp: xử lý data-if / data-if-not
   * rồi đổ giá trị vào các node data-bind.
   */
  applyData() {
    if (!this.daRenderTemplate || !this.container) return;

    const root = this.container.nativeElement;

    root.querySelectorAll<HTMLElement>('[data-if]').forEach(node => {
      this.toggleNode(node, !!this.resolve(node.dataset['if']));
    });

    root.querySelectorAll<HTMLElement>('[data-if-not]').forEach(node => {
      this.toggleNode(node, !this.resolve(node.dataset['ifNot']));
    });

    root.querySelectorAll<HTMLElement>('[data-bind]').forEach(node => {
      const value = this.resolve(node.dataset['bind']);
      node.textContent = value == null ? '' : String(value);
    });
  }

  toggleNode(node: HTMLElement, hienThi: boolean) {
    if (hienThi) {
      node.style.removeProperty('display');
    } else {
      node.style.display = 'none';
    }
  }

  /**
   * Lấy giá trị theo đường dẫn dạng 'svDangTrao.capBang'.
   */
  resolve(path: string | undefined): any {
    if (!path) return null;

    const context: any = { svDangTrao: this.svDangTrao };
    return path
      .trim()
      .split('.')
      .reduce((acc: any, key: string) => (acc == null ? acc : acc[key]), context);
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

    const styleElement = document.getElementById(MainScreen.STYLE_ID);
    if (styleElement) {
      styleElement.remove();
    }
  }
}
