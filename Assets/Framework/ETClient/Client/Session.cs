using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Model
{
	public sealed class Session : ASession
	{

        private static int RpcId { get; set; }
        private AChannel channel;
        private NetworkClient                       mNetwork;

        private readonly Dictionary<int, Action<IResponse>>     requestCallback = new Dictionary<int, Action<IResponse>>();
        private readonly List<byte[]> byteses = new List<byte[]>() { new byte[1], new byte[2] };

        public NetworkClient Network
        {
            get
            {
                return mNetwork;
            }
        }

        public int Error
        {
            get
            {
                return this.channel.Error;
            }
            set
            {
                this.channel.Error = value;
            }
        }

        //            this.Id = STATIC_ID++;

        //			this.mNetwork = rNetwork;
        //			this.mChannel = rChannel;
        //            mChannel.Start();

        //            this.StartRecv();
        public Session(NetworkClient rNetwork, AChannel aChannel)
        {
            mNetwork = rNetwork;
            this.channel = aChannel;
            this.requestCallback.Clear();
            this.Id = IdGenerater.GenerateId();
            long id = this.Id;
            channel.ErrorCallback += (c, e) =>
            {
                this.Network.Remove(id);
            };
            channel.ReadCallback += this.OnRead;

            this.channel.Start();
        }
        public void Start()
        {
            this.channel.Start();
        }
        public override void Dispose()
        {
            long id = this.Id;

            base.Dispose();

            foreach (Action<IResponse> action in this.requestCallback.Values)
            {
                //TODO
                //action.Invoke(new AResponse { Error = this.Error });
            }

            int error = this.channel.Error;
            if (this.channel.Error != 0)
            {
                Log.Error($"session dispose: {this.Id} {error}");
            }

            this.channel.Dispose();
            this.Network.Remove(id);
            this.requestCallback.Clear();
        }

        public IPEndPoint RemoteAddress
        {
            get
            {
                return this.channel.RemoteAddress;
            }
        }

        public ChannelType ChannelType
        {
            get
            {
                return this.channel.ChannelType;
            }
        }

        public MemoryStream Stream
        {
            get
            {
                return this.channel.Stream;
            }
        }

        public void OnRead(MemoryStream memoryStream)
        {
            try
            {
                this.Run(memoryStream);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Run(MemoryStream memoryStream)
        {
            memoryStream.Seek(Packet.MessageIndex, SeekOrigin.Begin);
            byte flag = memoryStream.GetBuffer()[Packet.FlagIndex];
            ushort opcode = BitConverter.ToUInt16(memoryStream.GetBuffer(), Packet.OpcodeIndex);

//#if !SERVER
//            if (OpcodeHelper.IsClientHotfixMessage(opcode))
//            {
//                this.GetComponent<SessionCallbackComponent>().MessageCallback.Invoke(this, flag, opcode, memoryStream);
//                return;
//            }
//#endif

            object message;
            try
            {
                Type instance = NetworkOpcodeType.Instance.GetType(opcode);
                message = this.Network.MessagePacker.DeserializeFrom(instance, memoryStream);
                //Log.Debug($"recv: {JsonHelper.ToJson(message)}");
            }
            catch (Exception e)
            {
                // 出现任何消息解析异常都要断开Session，防止客户端伪造消息
                Log.Error($"opcode: {opcode} {this.Network.Count} {e} ");
                this.Error = ErrorCode.ERR_PacketParserError;
                this.Network.Remove(this.Id);
                return;
            }
            
            // flag第一位为1表示这是rpc返回消息,否则交由MessageDispatcher分发
            if ((flag & 0x01) == 0)
            {
                this.Network.MessageDispatcher.Dispatch(this, opcode, message);
                return;
            }

            IResponse response = message as IResponse;
            if (response == null)
            {
                throw new Exception($"flag is response, but message is not! {opcode}");
            }
            Action<IResponse> action;
            if (!this.requestCallback.TryGetValue(response.RpcId, out action))
            {
                return;
            }
            this.requestCallback.Remove(response.RpcId);

            action(response);
        }

        public Task<IResponse> Call(IRequest request)
        {
            int rpcId = ++RpcId;
            var tcs = new TaskCompletionSource<IResponse>();

            this.requestCallback[rpcId] = (response) =>
            {
                try
                {
                    if (ErrorCode.IsRpcNeedThrowException(response.Error))
                    {
                        throw new RpcException(response.Error, response.Message);
                    }

                    tcs.SetResult(response);
                }
                catch (Exception e)
                {
                    tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
                }
            };

            request.RpcId = rpcId;
            this.Send(0x00, request);
            return tcs.Task;
        }

        public Task<IResponse> Call(IRequest request, CancellationToken cancellationToken)
        {
            int rpcId = ++RpcId;
            var tcs = new TaskCompletionSource<IResponse>();

            this.requestCallback[rpcId] = (response) =>
            {
                try
                {
                    if (ErrorCode.IsRpcNeedThrowException(response.Error))
                    {
                        throw new RpcException(response.Error, response.Message);
                    }

                    tcs.SetResult(response);
                }
                catch (Exception e)
                {
                    tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
                }
            };

            cancellationToken.Register(() => this.requestCallback.Remove(rpcId));

            request.RpcId = rpcId;
            this.Send(0x00, request);
            return tcs.Task;
        }

        public void Send(IMessage message)
        {
            this.Send(0x00, message);
        }

        public void Send(byte flag, IMessage message)
        {
            ushort opcode = NetworkOpcodeType.Instance.GetOpcode(message.GetType());

            Send(flag, opcode, message);
        }

        public void Send(byte flag, ushort opcode, object message)
        {
			//if (this.IsDisposed)
			//{
			//	throw new Exception("session已经被Dispose了");
			//}
			
//			if (OpcodeHelper.IsNeedDebugLogMessage(opcode) )
//			{
//#if !SERVER
//				if (OpcodeHelper.IsClientHotfixMessage(opcode))
//				{
//				}
//				else
//#endif
//				{
//					Log.Msg(message);
//				}
//			}

			MemoryStream stream = this.Stream;
			
			stream.Seek(Packet.MessageIndex, SeekOrigin.Begin);
			stream.SetLength(Packet.MessageIndex);
			this.Network.MessagePacker.SerializeTo(message, stream);
			stream.Seek(0, SeekOrigin.Begin);

			if (stream.Length > ushort.MaxValue)
			{
				Log.Error($"message too large: {stream.Length}, opcode: {opcode}");
				return;
			}
			
			this.byteses[0][0] = flag;
			this.byteses[1].WriteTo(0, opcode);
			int index = 0;
			foreach (var bytes in this.byteses)
			{
				Array.Copy(bytes, 0, stream.GetBuffer(), index, bytes.Length);
				index += bytes.Length;
			}

#if SERVER
			// 如果是allserver，内部消息不走网络，直接转给session,方便调试时看到整体堆栈
			if (this.Network.AppType == AppType.AllServer)
			{
				Session session = this.Network.Entity.GetComponent<NetInnerComponent>().Get(this.RemoteAddress);
				session.Run(stream);
				return;
			}
#endif

			this.Send(stream);
        }

        public void Send(MemoryStream stream)
        {
            channel.Send(stream);
        }
        //        private static uint                         STATIC_ID = 1;
        //		private static uint                         RpcId            { get; set; }

        //		private NetworkClient                       mNetwork;
        //		private Dictionary<uint, Action<object>>    mRequestCallback = new Dictionary<uint, Action<object>>();
        //		private AChannel                            mChannel;
        //		private List<byte[]>                        mByteses         = new List<byte[]>() {new byte[0], new byte[0]};

        //        public IPEndPoint                           RemoteAddress    => this.mChannel?.RemoteAddress;
        //        public ChannelType                          ChannelType      => this.mChannel.ChannelType;

        //        public MemoryStream Stream
        //        {
        //            get
        //            {
        //                return this.mChannel.Stream;
        //            }
        //        }

        //        public Session(NetworkClient rNetwork, AChannel rChannel)
        //		{
        //            this.Id = STATIC_ID++;

        //			this.mNetwork = rNetwork;
        //			this.mChannel = rChannel;
        //            mChannel.Start();

        //            this.StartRecv();
        //		}

        //		private async void StartRecv()
        //		{
        //			while (true)
        //			{
        //				if (this.Id == 0)
        //				{
        //					return;
        //				}

        //				byte[] rMessageBytes;
        //				try
        //				{
        //					rMessageBytes = await mChannel.Recv();
        //					if (this.Id == 0)
        //					{
        //						return;
        //					}
        //				}
        //				catch (Exception e)
        //				{
        //					Log.Error(e.ToString());
        //					continue;
        //				}

        //				if (rMessageBytes.Length < 3)
        //				{
        //					continue;
        //				}

        //				ushort nOpcode = BitConverter.ToUInt16(rMessageBytes, 0);
        //				try
        //				{
        //					this.Run(nOpcode, rMessageBytes);
        //				}
        //				catch (Exception e)
        //				{
        //					Log.Error(e.ToString());
        //				}
        //			}
        //		}

        //		private void Run(ushort nOpcode, byte[] rMessageBytes)
        //		{
        //			int nOffset = 0;
        //			bool bIsCompressed = (nOpcode & 0x8000) > 0;    // opcode最高位表示是否压缩

        //            if (bIsCompressed) // 最高位为1,表示有压缩,需要解压缩
        //			{
        //				rMessageBytes = ZipHelper.Decompress(rMessageBytes, 2, rMessageBytes.Length - 2);
        //				nOffset = 0;
        //			}
        //			else
        //			{
        //				nOffset = 2;
        //			}
        //			nOpcode &= 0x7fff;
        //			this.RunDecompressedBytes(nOpcode, rMessageBytes, nOffset);
        //		}

        //		private void RunDecompressedBytes(ushort nOpcode, byte[] rMessageBytes, int nOffset)
        //		{
        //			Type rMessageType =  NetworkOpcodeType.Instance.GetType(nOpcode);
        //			object message = this.mNetwork.MessagePacker.DeserializeFrom(rMessageType, rMessageBytes, nOffset, rMessageBytes.Length - nOffset);

        //			//Log.Debug($"recv: {MongoHelper.ToJson(message)}");

        //			AResponse response = message as AResponse;
        //			if (response != null)
        //			{
        //				// rpcFlag>0 表示这是一个rpc响应消息
        //				// Rpc回调有找不着的可能，因为client可能取消Rpc调用
        //				Action<object> rAction;
        //				if (!this.mRequestCallback.TryGetValue(response.RpcId, out rAction))
        //				{
        //					return;
        //				}
        //				this.mRequestCallback.Remove(response.RpcId);
        //				rAction(message);
        //				return;
        //			}
        //			this.mNetwork.MessageDispatcher.Dispatch(this, nOpcode, nOffset, rMessageBytes, (AMessage)message);
        //		}

        //		/// <summary>
        //		/// Rpc调用
        //		/// </summary>
        //		public Task<Response> Call<Response>(ARequest rRequest, CancellationToken rCancellationToken) where Response : AResponse
        //		{
        //			rRequest.RpcId = ++RpcId;
        //			this.SendMessage(rRequest);

        //			var tcs = new TaskCompletionSource<Response>();
        //			this.mRequestCallback[RpcId] = (message) =>
        //			{
        //				try
        //				{
        //					Response response = (Response)message;
        //					if (response.Error > 100)
        //					{
        //						tcs.SetException(new RpcException(response.Error, response.Message));
        //						return;
        //					}
        //					//Log.Debug($"recv: {MongoHelper.ToJson(response)}");
        //					tcs.SetResult(response);
        //				}
        //				catch (Exception e)
        //				{
        //					tcs.SetException(new Exception($"Rpc Error: {typeof(Response).FullName}", e));
        //				}
        //			};
        //			rCancellationToken.Register(() => { this.mRequestCallback.Remove(RpcId); });
        //			return tcs.Task;
        //		}

        //		/// <summary>
        //		/// Rpc调用,发送一个消息,等待返回一个消息
        //		/// </summary>
        //		public Task<Response> Call<Response>(ARequest rRequest) where Response : AResponse
        //		{
        //			rRequest.RpcId = ++RpcId;

        //			var tcs = new TaskCompletionSource<Response>();
        //			this.mRequestCallback[RpcId] = (message) =>
        //			{
        //				try
        //				{
        //					Response response = (Response)message;
        //					if (response.Error > 100)
        //					{
        //						tcs.SetException(new RpcException(response.Error, response.Message));
        //						return;
        //					}
        //                    Log.Debug($"recv: {this.mNetwork.MessagePacker.SerializeToText(response)}");
        //					tcs.SetResult(response);
        //				}
        //				catch (Exception e)
        //				{
        //					tcs.SetException(new Exception($"Rpc Error: {typeof(Response).FullName}", e));
        //				}
        //			};

        //            this.SendMessage(rRequest);

        //            return tcs.Task;
        //		}

        //		public void Send(AMessage rMessage)
        //		{
        //			if (this.Id == 0)
        //			{
        //				throw new Exception("session已经被Dispose了");
        //			}
        //			this.SendMessage(rMessage);
        //		}

        //		public void Reply<Response>(Response rMessage) where Response : AResponse
        //		{
        //			if (this.Id == 0)
        //			{
        //				throw new Exception("session已经被Dispose了");
        //			}
        //			this.SendMessage(rMessage);
        //		}

        //		private void SendMessage(object rMessage)
        //		{
        //			//Log.Debug($"send: {MongoHelper.ToJson(message)}");
        //			ushort nOpcode = NetworkOpcodeType.Instance.GetOpcode(rMessage.GetType());
        //			byte[] rMessageBytes = this.mNetwork.MessagePacker.SerializeToByteArray(rMessage);
        //			if (rMessageBytes.Length > 100)
        //			{
        //				byte[] rNewMessageBytes = ZipHelper.Compress(rMessageBytes);
        //				if (rNewMessageBytes.Length < rMessageBytes.Length)
        //				{
        //					rMessageBytes = rNewMessageBytes;                    
        //                    //nOpcode |= 0x8000;
        //                }
        //			}

        //			byte[] rOpcodeBytes = BitConverter.GetBytes(nOpcode);

        //			this.mByteses[0] = rOpcodeBytes;
        //			this.mByteses[1] = rMessageBytes;

        //			mChannel.Send(this.mByteses);
        //		}














        //        public void Send(byte flag, ushort opcode, object message)
        //        {
        //            if (this.Id == 0)
        //            {
        //                throw new Exception("session已经被Dispose了");
        //            }
        //            this.mByteses[0][0] = flag;
        //            this.mByteses[1] = BitConverter.GetBytes(opcode);

        //            MemoryStream stream = this.Stream;

        //            int index = Packet.Index;
        //            stream.Seek(index, SeekOrigin.Begin);
        //            stream.SetLength(index);
        //            var bb = this.mNetwork.MessagePacker.SerializeToByteArray(message);
        //            this.mNetwork.MessagePacker.SerializeToByteArray(message, stream);

        //            stream.Seek(0, SeekOrigin.Begin);
        //            index = 0;
        //            foreach (var bytes in this.mByteses)
        //            {
        //                Array.Copy(bytes, 0, stream.GetBuffer(), index, bytes.Length);
        //                index += bytes.Length;
        //            }

        //#if SERVER
        //			// 如果是allserver，内部消息不走网络，直接转给session,方便调试时看到整体堆栈
        //			if (this.Network.AppType == AppType.AllServer)
        //			{
        //				Session session = this.Network.Entity.GetComponent<NetInnerComponent>().Get(this.RemoteAddress);

        //				Packet packet = ((TChannel)this.channel).parser.packet;

        //				packet.Flag = flag;
        //				packet.Opcode = opcode;
        //				packet.Stream.Seek(0, SeekOrigin.Begin);
        //				packet.Stream.SetLength(0);
        //				this.Network.MessagePacker.SerializeTo(message, stream);
        //				session.Run(packet);
        //				return;
        //			}
        //#endif

        //            this.Send(stream);
        //        }

        //        public void Send(MemoryStream stream)
        //        {
        //            mChannel.Send(stream);
        //        }

        //        public void Dispose()
        //		{
        //			if (this.Id == 0)
        //			{
        //				return;
        //			}

        //			long nId = this.Id;

        //			this.mChannel.Dispose();
        //			this.mNetwork.Remove(nId);
        //		}
    }
}