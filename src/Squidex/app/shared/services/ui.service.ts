/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ApiUrlConfig } from '@app/framework';

export interface UISettingsDto {
    mapType: string;
    mapKey?: string;
}

@Injectable()
export class UIService {
    constructor(
        private readonly http: HttpClient,
        private readonly apiUrl: ApiUrlConfig
    ) {
    }

    public getSettings(appName: string): Observable<UISettingsDto & object> {
        const url = this.apiUrl.buildUrl(`api/apps/${appName}/ui/settings`);

        return this.http.get<UISettingsDto>(url).pipe(
            catchError(_ => {
                return of({ regexSuggestions: [], mapType: 'OSM', mapKey: '' });
            }));
    }

    public putSetting(appName: string, key: string, value: any): Observable<any> {
        const url = this.apiUrl.buildUrl(`api/apps/${appName}/ui/settings/${key}`);

        return this.http.put(url, { value });
    }

    public deleteSetting(appName: string, key: string): Observable<any> {
        const url = this.apiUrl.buildUrl(`api/apps/${appName}/ui/settings/${key}`);

        return this.http.delete(url);
    }
}