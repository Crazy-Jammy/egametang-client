using System;
using System.Net;
using System.Threading.Tasks;

namespace Model
{
	public enum NetworkProtocol
	{
		TCP,
		KCP
	}

	public abstract class AService: IDisposable
    {
        public abstract AChannel GetChannel(long id);

        private Action<AChannel> acceptCallback;

        public event Action<AChannel> AcceptCallback
        {
            add
            {
                this.acceptCallback += value;
            }
            remove
            {
                this.acceptCallback -= value;
            }
        }

        protected void OnAccept(AChannel channel)
        {
            this.acceptCallback.Invoke(channel);
        }

        public abstract AChannel ConnectChannel(IPEndPoint ipEndPoint);

        public abstract AChannel ConnectChannel(string address);

        public virtual void Dispose() { }

        public abstract void Remove(long channelId);

        public abstract void Update();

        public abstract void Start();
    }
}