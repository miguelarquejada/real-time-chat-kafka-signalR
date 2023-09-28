import { MessageType } from "./MessageType";

export interface Message {
    type: MessageType;
    value: string;
} 