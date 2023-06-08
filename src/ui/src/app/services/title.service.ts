import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Injectable({
  providedIn: 'root'
})
export class TitleService {

  constructor(private readonly title: Title) { }

  public setTitle(title?: string) {
    const fullTitle = title
      ? `${title} - Chilhowee Glider Viewer`
      : 'Chilhowee Glider Viewer';
    this.title.setTitle(fullTitle);
  }
}
