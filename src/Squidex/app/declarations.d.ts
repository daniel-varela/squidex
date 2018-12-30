/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

declare module 'graphiql';
declare module 'pikaday/pikaday';
declare module 'progressbar.js';

declare module 'sortablejs' {
    export class Ref {
        public destroy(): any;
    }

    export function create(element: any, options: any): Ref;
}
