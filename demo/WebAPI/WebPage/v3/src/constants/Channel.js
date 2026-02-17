import { Identity } from "./Identity.js";

export const Channel = {
    SYSTEM: `${Identity.APP_ID}.channel.system`,
    DATA: `${Identity.APP_ID}.channel.data`,
    FILE: `${Identity.APP_ID}.channel.file`,
    STATE: `${Identity.APP_ID}.channel.state`,
    STATIC: `${Identity.APP_ID}.channel.static`
};