﻿using System;

namespace Model
{
	public class ClientDispatcher: IMessageDispatcher
	{
		public void Dispatch(ASession session, ushort opcode, int offset, byte[] messageBytes, IMessage message)
		{
            // 如果是帧同步消息,交给ClientFrameComponent处理
            //FrameMessage frameMessage = message as FrameMessage;
            //if (frameMessage != null)
            //{
            //	Game.Scene.GetComponent<ClientFrameComponent>().Add(session, frameMessage);
            //	return;
            //}

            //// 普通消息或者是Rpc请求消息
            //if (message is AMessage || message is ARequest)
            //{
            //	MessageInfo messageInfo = new MessageInfo(opcode, message);
            //	Game.Scene.GetComponent<MessageDispatherComponent>().Handle(session, messageInfo);
            //	return;
            //}

            //throw new Exception($"message type error: {message.GetType().FullName}");

            UnityEngine.Debug.LogError("Recv Dispatch...");
		}

        public void Dispatch(Session session, Packet packet)
        {
            UnityEngine.Debug.LogError("Recv Dispatch Packet...");
        }
    }
}
