using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Model
{
    [Flags]
    public enum PacketFlags
    {
        None = 0,
        Reliable = 1 << 0,
        Unsequenced = 1 << 1,
        NoAllocate = 1 << 2
    }

    public enum ChannelType
    {
        Connect,
        Accept,
    }

    public abstract class AChannel : IDisposable
    {
        public long Id { set; get; }
        public ChannelType ChannelType { get; }

        protected AService service;

        public abstract MemoryStream Stream { get; }

        public int Error { get; set; }

        public IPEndPoint RemoteAddress { get; protected set; }

        private Action<AChannel, int> errorCallback;

        public event Action<AChannel, int> ErrorCallback
        {
            add
            {
                this.errorCallback += value;
            }
            remove
            {
                this.errorCallback -= value;
            }
        }

        private Action<MemoryStream> readCallback;

        public event Action<MemoryStream> ReadCallback
        {
            add
            {
                this.readCallback += value;
            }
            remove
            {
                this.readCallback -= value;
            }
        }

        public bool IsDisposed
        {
            get
            {
                return this.Id == 0;
            }
        }

        protected void OnRead(MemoryStream memoryStream)
        {
            this.readCallback.Invoke(memoryStream);
        }

        protected void OnError(int e)
        {
            this.Error = e;
            this.errorCallback?.Invoke(this, e);
        }

        protected AChannel(AService service, ChannelType channelType)
        {
            this.Id = IdGenerater.GenerateId();
            this.ChannelType = channelType;
            this.service = service;
        }

        public abstract void Start();

        /// <summary>
        /// 发送消息
        /// </summary>
        //public abstract void Send(byte[] buffer, int index, int length);

        public abstract void Send(MemoryStream stream);

        public virtual void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.service.Remove(this.Id);
        }
    }
}