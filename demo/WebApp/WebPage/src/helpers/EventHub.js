import { Channel } from '../constants/Channel.js';
import { Subject, fromEvent, merge, map, share, filter } from 'rxjs';

/**
 * EventHub: The central nervous system.
 * Supports local and cross-tab communication.
 */
export class EventHub {
    constructor(channelName) {
        this._instanceId = crypto.randomUUID();
        this._local$ = new Subject();
        this._bridge = new BroadcastChannel(channelName);
        
        const external$ = fromEvent(this._bridge, 'message').pipe(
            map(ev => ev.data),
            share()
        );

        this._messages$ = merge(this._local$, external$);
    }

    /**
     * Send a message to the hub. 
     * it stays local to the current tab.
     * * @example
     * hub.post('USER_LOGIN', { id: 1 }); // Current Tab
     * * @param {string} topic - The name of the event programme.
     * * @param {any} data - The payload/content of the message.
     */
    send(topic, data) {
        const envelope = { topic, data, sender: this._instanceId};
        this._local$.next(envelope);
    }

    /**
     * Cast a message to the hub. 
     * it broadcasts to current tab as well as other tab.
     * * @example
     * hub.cast('USER_LOGIN', { id: 1 }); // Across Tab
     * * @param {string} topic - The name of the event programme.
     * * @param {any} data - The payload/content of the message.
     */
    cast(topic, data) {
        const envelope = { topic, data, sender: this._instanceId};
        this._local$.next(envelope);
        this._bridge.postMessage(envelope);
    }

    /**
     * Watch everything on the channel, excluding this tab's own messages.
     * * @param {string} [topic] - Optional. The specific topic to filter for.
     * @returns {Observable<any>} An RxJS Observable that emits the message data (or envelope if no topic is provided).
     */
    watch(topic) {
        return this._messages$.pipe(
            filter(msg => (!topic || msg.topic === topic) && (msg.sender !== this._instanceId)),
            map(msg => msg.data)
        );
    }

    /**
     * Hear everything on the channel, including this tab's own messages.
     * * @param {string} [topic] - Optional. The specific topic to filter for.
     * @returns {Observable<any>} An RxJS Observable that emits the message data (or envelope if no topic is provided).
     */
    hear(topic) {
        return this._messages$.pipe(
            filter(msg => (!topic || msg.topic === topic)),
            map(msg => msg.data)
        );
    }

    /**
     * Closes the BroadcastChannel and completes the internal subjects.
     * Call this when the hub is no longer needed to prevent memory leaks.
     */
    dispose() {
        this._bridge.close();
        this._local$.complete();
    }
}

// Named exports for specific buses
export const systemHub = new EventHub(Channel.SYSTEM);
export const dataHub = new EventHub(Channel.DATA);
export const fileHub = new EventHub(Channel.FILE);
export const stateHub = new EventHub(Channel.STATE);
export const staticHub = new EventHub(Channel.STATIC);
