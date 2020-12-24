using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public interface IBaseClient
	{
		bool hasPendingSuperPackets();
		ushort firstSuperPacketId();
		byte[] popPendingSuperPacket();

		void disconnect();
		void handlingError();
	}
}
