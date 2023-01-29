using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mirai.CSharp;
using Mirai.CSharp.Framework.Handlers;
using Mirai.CSharp.HttpApi.Handlers;
using Mirai.CSharp.HttpApi.Models.EventArgs;
using Mirai.CSharp.HttpApi.Parsers;
using Mirai.CSharp.HttpApi.Parsers.Attributes;
using Mirai.CSharp.HttpApi.Session;
using Mirai.CSharp.Models;

namespace tech.msgp.groupmanager.Code.EventHandlers
{
    [RegisterMiraiHttpParser(typeof(DefaultMappableMiraiHttpMessageParser<INewFriendApplyEventArgs, NewFriendApplyEventArgs>))]
    public partial class EventHandler : IMiraiHttpMessageHandler<INewFriendApplyEventArgs>
    {
        public async Task HandleMessageAsync(IMiraiHttpSession session, INewFriendApplyEventArgs e)
        {
            await Task.Delay(10000);
            await session.HandleNewFriendApplyAsync(e, FriendApplyAction.Deny, "请通过群临时聊天发送私信");
            e.BlockRemainingHandlers = false;
        }
    }
}
