import { Component } from '@angular/core';
import { Message } from './Models/Message';
import { MessageType } from './Models/MessageType';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  
  roomTitle: string = 'Sala 1';
  activeRoom: string = 'room1';

  room1Chat: Message[] = [];
  room2Chat: Message[] = [];
  adminChat: Message[] = [];

  room1Notifications: number = 0;
  room2Notifications: number = 0;
  adminNotifications: number = 0;

  currentChat: Message[] = this.room1Chat;
  currentMessage: string = '';
  
  groups = {
    room1: 'room1',
    room2: 'room2',
    admin: 'admin'
  }
  
  hubConnection: signalR.HubConnection;

  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5217/chat')
    .build();

    this.hubConnection.start()
      .then(() => {
          console.log('CONNECTED!');
          this.hubConnection.invoke("JoinRoom", 'room1');
          this.hubConnection.invoke("JoinRoom", 'room2');
          this.hubConnection.invoke("JoinRoom", 'admin');
      })
      .catch(err => console.log(err));

    this.hubConnection.on("ReceiveMessage", (room, message) => {
      if (this.groups.room1 === room) {
        this.room1Chat.push({ type: MessageType.OtherMessage, value: message });

        if (this.activeRoom != this.groups.room1)
          this.room1Notifications++;
      }

      if (this.groups.room2 === room) {
        this.room2Chat.push({ type: MessageType.OtherMessage, value: message });

        if (this.activeRoom != this.groups.room2)
          this.room2Notifications++;
      }

      if (this.groups.admin === room) {
        this.room1Chat.push({ type: MessageType.OtherMessage, value: message });
        this.room2Chat.push({ type: MessageType.OtherMessage, value: message });
        this.adminChat.push({ type: MessageType.OtherMessage, value: message });

        if (this.activeRoom != this.groups.admin)
          this.adminNotifications++;
      }
    });
  }

  sendMessage() {
    if (this.activeRoom == this.groups.room1) {
      this.room1Chat.push({ type: MessageType.MyMessage, value: this.currentMessage });
    }
    
    if (this.activeRoom == this.groups.room2) {
      this.room2Chat.push({ type: MessageType.MyMessage, value: this.currentMessage });
    }
    
    if (this.activeRoom == this.groups.admin) {
      this.adminChat.push({ type: MessageType.MyMessage, value: this.currentMessage });
    }

    this.hubConnection.invoke("SendMessage", this.activeRoom, this.currentMessage);
    this.currentMessage = '';
  }

  changeRoom(room: string) {
    this.activeRoom = room;

    if (room == this.groups.room1) {
      this.roomTitle = 'Sala 1';
      this.currentChat = this.room1Chat;
      this.room1Notifications = 0;
      return;
    }

    if (room == this.groups.room2) {
      this.roomTitle = 'Sala 2';
      this.currentChat = this.room2Chat;
      this.room2Notifications = 0;
      return;
    }

    this.roomTitle = 'Admin';
    this.currentChat = this.adminChat;
    this.adminNotifications = 0;
  }
}
