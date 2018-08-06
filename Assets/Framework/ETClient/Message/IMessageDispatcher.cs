namespace Model
{
	public interface IMessageDispatcher
	{
		void Dispatch(ASession session, ushort opcode, int offset, byte[] messageBytes, IMessage message);

        void Dispatch(Session session, Packet packet);

    }
}
