using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using System.Net;

namespace Model
{
	public class NetworkClient : TSingleton<NetworkClient>
    {
        public AppType AppType;

        private AService Service;

        private readonly Dictionary<long, Session> sessions = new Dictionary<long, Session>();

        NetworkProtocol _Protocal;

        public IMessagePacker MessagePacker { get; set; }

        public IMessageDispatcher MessageDispatcher { get; set; }


        private NetworkClient()
        {
        }

        public void Initialize(NetworkProtocol protocol)
        {
            _Protocal = protocol;
            this.MessagePacker = new SerializerPacker();
            this.MessageDispatcher = new ClientDispatcher();
            try
            {
                switch (protocol)
                {
                    case NetworkProtocol.KCP:
                        this.Service = new KService();
                        break;
                    case NetworkProtocol.TCP:
                        this.Service = new TService();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                this.Service.AcceptCallback += this.OnAccept;

                this.StartAccept();
            }
            catch (Exception e)
            {
                throw new Exception($"{e}");
            }
        }

        public void Initialize(NetworkProtocol protocol, IPEndPoint ipEndPoint)
        {
            _Protocal = protocol;
            try
            {
                switch (protocol)
                {
                    case NetworkProtocol.KCP:
                        this.Service = new KService(ipEndPoint);
                        break;
                    case NetworkProtocol.TCP:
                        this.Service = new TService(ipEndPoint);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                this.Service.AcceptCallback += this.OnAccept;

                this.StartAccept();
            }
            catch (Exception e)
            {
                throw new Exception($"{ipEndPoint}", e);
            }
        }

        public void StartAccept()
        {
            this.Service.Start();
        }

        public int Count
        {
            get { return this.sessions.Count; }
        }

        public void OnAccept(AChannel channel)
        {
            Session session = new Session(this, channel);
            this.sessions.Add(session.Id, session);
        }

        public virtual void Remove(long id)
        {
            Session session;
            if (!this.sessions.TryGetValue(id, out session))
            {
                return;
            }
            this.sessions.Remove(id);
            session.Dispose();
        }

        public Session Get(long id)
        {
            Session session;
            this.sessions.TryGetValue(id, out session);
            return session;
        }

        /// <summary>
        /// 创建一个新Session
        /// </summary>
        public Session Create(IPEndPoint ipEndPoint)
        {
            //Initialize(_Protocal, ipEndPoint);
            AChannel channel = this.Service.ConnectChannel(ipEndPoint);
            Session session = new Session(this, channel);
            this.sessions.Add(session.Id, session);
            return session;
        }

        public void Update()
        {
            if (this.Service == null)
            {
                return;
            }
            this.Service.Update();
        }

        public void Dispose()
        {
            foreach (Session session in this.sessions.Values.ToArray())
            {
                session.Dispose();
            }

            this.Service.Dispose();
        }
  //      private AService                            mService;
		//private Dictionary<long, Session>    mSessions               = new Dictionary<long, Session>();

		//public IMessagePacker                       MessagePacker           { get; set; }
		//public IMessageDispatcher                   MessageDispatcher       { get; set; }

  //      private NetworkClient()
  //      {
  //      }

		//public void Initialize(NetworkProtocol rProtocol)
  //      {
  //          this.MessagePacker = new SerializerPacker();
  //          this.MessageDispatcher = new ClientDispatcher();

  //          switch (rProtocol)
		//	{
		//		case NetworkProtocol.TCP:
		//			this.mService = new TService();
		//			break;
		//		case NetworkProtocol.KCP:
		//			this.mService = new KService();
		//			break;
		//		default:
		//			throw new ArgumentOutOfRangeException();
		//	}
		//}

		//public void Initialize(NetworkProtocol rProtocol, string rHost, int nPort)
		//{
		//	try
		//	{
		//		switch (rProtocol)
		//		{
		//			case NetworkProtocol.TCP:
		//				this.mService = new TService(new IPEndPoint(IPAddress.Parse(rHost), nPort));
		//				break;
		//			case NetworkProtocol.KCP:
		//				this.mService = new KService(new IPEndPoint(IPAddress.Parse(rHost), nPort));
		//				break;
		//			default:
		//				throw new ArgumentOutOfRangeException();
  //              }

  //              this.Service.AcceptCallback += this.OnAccept;

  //              this.StartAccept();
		//	}
		//	catch (Exception e)
		//	{
		//		throw new Exception($"{rHost} {nPort}", e);
		//	}
		//}

		//private async void StartAccept()
		//{
		//	while (true)
		//	{
		//		await this.Accept();
		//	}
		//}

		//public virtual async Task<Session> Accept()
		//{
		//	AChannel rChannel = await this.mService.AcceptChannel();
		//	Session rSession = new Session(this, rChannel);
		//	rChannel.ErrorCallback += (c, e) => { this.Remove(rSession.Id); };
		//	this.mSessions.Add(rSession.Id, rSession);
		//	return rSession;
		//}

		//public virtual void Remove(long nId)
		//{
		//	Session rSession;
		//	if (!this.mSessions.TryGetValue(nId, out rSession))
		//	{
		//		return;
		//	}
		//	this.mSessions.Remove(nId);
		//	rSession.Dispose();
		//}

		//public Session Get(long nId)
		//{
		//	Session rSession;
		//	this.mSessions.TryGetValue(nId, out rSession);
		//	return rSession;
		//}

		///// <summary>
		///// 创建一个新Session
		///// </summary>
		//public virtual Session Create(string rAddress)
		//{
		//	try
		//	{
		//		string[] ss = rAddress.Split(':');
		//		int nPort = int.Parse(ss[1]);
		//		string rHost = ss[0];
		//		AChannel rChannel = this.mService.ConnectChannel(rHost, nPort);
		//		Session rSession = new Session(this, rChannel);
		//		rChannel.ErrorCallback += (c, e) => { this.Remove(rSession.Id); };
		//		this.mSessions.Add(rSession.Id, rSession);
		//		return rSession;
		//	}
		//	catch (Exception e)
		//	{
		//		Log.Error(e.ToString());
		//		return null;
		//	}
		//}

		//public void Update()
		//{
		//	if (this.mService == null)
		//	{
		//		return;
		//	}
		//	this.mService.Update();
		//}

		//public void Dispose()
		//{
		//	foreach (Session rSession in this.mSessions.Values.ToArray())
		//	{
		//		rSession.Dispose();
		//	}
		//	this.mService.Dispose();
		//}
	}
}