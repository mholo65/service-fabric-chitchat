﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Owin.WebSocket;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    public class ChatController : WebSocketConnection
    {
        private readonly IChatService _chatService;
        private ChatClient _chatClient = null;

        public ChatController(IChatService chatService)
        {
            if(chatService == null)
            {
                throw new ArgumentNullException(nameof(chatService));
            }

            _chatService = chatService;
        }

        public override Task OnOpenAsync()
        {
            return SendWelcomeMessage();
        }

        public async override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            // Close if not Text
            if(type != WebSocketMessageType.Text)
            {
                var status = type == WebSocketMessageType.Binary 
                    ? WebSocketCloseStatus.InvalidMessageType
                    : WebSocketCloseStatus.NormalClosure;

                await Close(status, string.Empty);
                return;
            }
            
            // Check if Registered
            if(_chatClient == null)
            {
                var name = Encoding.UTF8.GetString(message.Array, message.Offset, message.Count);

                var chatClient = new ChatClient(name, async (msg) =>
                {
                    await SendMessage($"{{ \"sender\"=\"{msg.Sender}\", \"message\"=\"{msg.Message}\" }}");
                });

                if (await _chatService.RegisterClientAsync(chatClient))
                {
                    _chatClient = chatClient;
                    await SendMessage($"{{ \"message\"=\"Welcome {name}!\" }}");
                }
                else
                {
                    await SendMessage($"{{ \"message\"=\"The name, {name} is already taken.\" }}");
                    await SendWelcomeMessage();
                }

                return;
            }

            //Handle the message from the client

            //Example of JSON serialization with the client
            var json = Encoding.UTF8.GetString(message.Array, message.Offset, message.Count);


            //Echo the message back to the client as text
            
        }

        private Task SendWelcomeMessage()
        {
            return SendMessage("{ \"message\"=\"Welcome to ChitChat. Please tell me your name.\" }");
        }

        private Task SendMessage(string message)
        {
            return SendText(Encoding.UTF8.GetBytes(message), true);
        }
    }
}
