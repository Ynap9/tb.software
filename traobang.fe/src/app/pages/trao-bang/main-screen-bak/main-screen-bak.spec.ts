import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MainScreenBak } from './main-screen-bak';

describe('MainScreenBak', () => {
  let component: MainScreenBak;
  let fixture: ComponentFixture<MainScreenBak>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MainScreenBak]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MainScreenBak);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
