/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, forwardRef, Input, OnDestroy, OnInit, Renderer2, ViewChild } from '@angular/core';
import { ControlValueAccessor,  NG_VALUE_ACCESSOR } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

import { Types } from '@app/framework/internal';

export const SQX_IFRAME_EDITOR_CONTROL_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => IFrameEditorComponent), multi: true
};

@Component({
    selector: 'sqx-iframe-editor',
    styleUrls: ['./iframe-editor.component.scss'],
    templateUrl: './iframe-editor.component.html',
    providers: [SQX_IFRAME_EDITOR_CONTROL_VALUE_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class IFrameEditorComponent implements ControlValueAccessor, AfterViewInit,  OnInit, OnDestroy {
    private windowMessageListener: Function;
    private callChange = (v: any) => { /* NOOP */ };
    private callTouched = () => { /* NOOP */ };
    private value: any;
    private isDisabled = false;
    private isInitialized = false;
    private plugin: HTMLIFrameElement;

    @ViewChild('iframe')
    public iframe: ElementRef<HTMLIFrameElement>;

    @Input()
    public set url(value: string) {
        this.sanitizedUrl = this.sanitizer.bypassSecurityTrustResourceUrl(value);
    }

    public sanitizedUrl: SafeResourceUrl;

    constructor(
        private readonly sanitizer: DomSanitizer,
        private readonly renderer: Renderer2
    ) {
    }

    public ngOnDestroy() {
        this.windowMessageListener();
    }

    public ngAfterViewInit() {
        this.plugin = this.iframe.nativeElement;
    }

    public ngOnInit(): void {
        this.windowMessageListener =
            this.renderer.listen('window', 'message', (event: MessageEvent) => {
                if (event.source === this.plugin.contentWindow) {
                    const { type } = event.data;

                    if (type === 'started') {
                        this.isInitialized = true;

                        if (this.plugin.contentWindow && Types.isFunction(this.plugin.contentWindow.postMessage)) {
                            this.plugin.contentWindow.postMessage({ type: 'disabled', isDisabled: this.isDisabled }, '*');
                            this.plugin.contentWindow.postMessage({ type: 'valueChanged', value: this.value }, '*');
                        }
                    } else if (type === 'resize') {
                        const { height } = event.data;

                        this.plugin.height = height + 'px';
                    } else if (type === 'valueChanged') {
                        const { value } = event.data;

                        if (!Types.jsJsonEquals(this.value, value)) {
                            this.value = value;

                            this.callChange(value);
                        }
                    } else if (type === 'touched') {
                        this.callTouched();
                    }
                }
            });
    }

    public writeValue(obj: any) {
        this.value = obj;

        if (this.isInitialized && this.plugin.contentWindow && Types.isFunction(this.plugin.contentWindow.postMessage)) {
            this.plugin.contentWindow.postMessage({ type: 'valueChanged', value: this.value }, '*');
        }
    }

    public setDisabledState(isDisabled: boolean): void {
        this.isDisabled = isDisabled;

        if (this.isInitialized && this.plugin.contentWindow && Types.isFunction(this.plugin.contentWindow.postMessage)) {
            this.plugin.contentWindow.postMessage({ type: 'disabled', isDisabled: this.isDisabled }, '*');
        }
    }

    public registerOnChange(fn: any) {
        this.callChange = fn;
    }

    public registerOnTouched(fn: any) {
        this.callTouched = fn;
    }
}