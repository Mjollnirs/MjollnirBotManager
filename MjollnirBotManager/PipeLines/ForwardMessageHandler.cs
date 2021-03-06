﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.MicroKernel;
using MjollnirBotManager.Common.Configuration;
using MjollnirBotManager.Common.PipeLines;
using MjollnirBotManager.Common.Security;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace MjollnirBotManager.PipeLines
{
    internal sealed class ForwardMessageHandler : MessageHandlerBase
    {
        private readonly IAdminChatManager _adminChatManager;
        private readonly IBotConfiguration _config;

        private ChatId preChatId;

        public ForwardMessageHandler(
            IKernel kernel,
            ILogger logger,
            IAdminChatValidator IAdminChatManager,
            ISessionManager sessionManager,
            IBotConfiguration config,
            IAdminChatManager adminChatManager)
            : base(kernel, logger, IAdminChatManager, sessionManager)
        {
            _adminChatManager = adminChatManager;
            _config = config;
        }

        protected async override Task ProcessHandler(Chat chat, MessageType messageType, bool isAdmin, Message message, ISession session, CancellationToken token)
        {
            if (isAdmin && chat.Type == ChatType.Private)
            {
                ChatId id = preChatId;

                if (id == null)
                    id = new ChatId(_config.DefaultChat);

                switch (messageType)
                {
                    case MessageType.Text:
                        await BotManager.Telegram.SendTextMessageAsync(id,
                            message.Text,
                            cancellationToken: token);
                        break;
                    case MessageType.Sticker:
                        await BotManager.Telegram.SendStickerAsync(id,
                            new InputOnlineFile(message.Sticker.FileId),
                            cancellationToken: token);
                        break;
                    case MessageType.Photo:
                        await BotManager.Telegram.SendPhotoAsync(id,
                            new InputOnlineFile(message.Photo.OrderByDescending(x => x.FileSize).First().FileId),
                            cancellationToken: token);
                        break;
                    case MessageType.Audio:
                        await BotManager.Telegram.SendAudioAsync(id,
                            new InputOnlineFile(message.Audio.FileId),
                            cancellationToken: token);
                        break;
                    case MessageType.Video:
                        await BotManager.Telegram.SendVideoAsync(id,
                            new InputOnlineFile(message.Video.FileId),
                            cancellationToken: token);
                        break;
                    case MessageType.Voice:
                        await BotManager.Telegram.SendVoiceAsync(id,
                            new InputOnlineFile(message.Voice.FileId),
                            cancellationToken: token);
                        break;
                    default:
                        break;
                }
            }
            else if (!isAdmin)
            {
                preChatId = chat;
                await ForwardToAdmin(message, token);
            }
        }

        private async Task ForwardToAdmin(Message message, CancellationToken token)
        {
            foreach (var item in await _adminChatManager.GetAllAdminChatsAsync())
            {
                await BotManager.Telegram.ForwardMessageAsync(item,
                    message.Chat,
                    message.MessageId,
                    cancellationToken: token);
            }
        }
    }
}
