using System;

namespace Model
{
	public interface IMHandler
	{
		void Handle(ASession session, IMessage message);
		Type GetMessageType();
	}
}