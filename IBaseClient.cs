using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public interface IBaseClient
	{
		bool hasPendingSuperPackets();
		ushort firstSuperPacketId();
		ushort firstSuperPacketTickId();
		ushort lastSuperPacketId();
		ushort lastSuperPacketSize();
		SuperPacketReader popPendingSuperPacket();

		void disconnect();
		void handlingError();
	}
}
