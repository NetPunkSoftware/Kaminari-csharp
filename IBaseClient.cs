using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public interface IBaseClient
	{
		bool hasPendingSuperPackets();
		ushort firstSuperPacketId();
		ushort lastSuperPacketId();
		ushort lastSuperPacketSize();
		byte[] popPendingSuperPacket();

		void disconnect();
		void handlingError();
	}
}
