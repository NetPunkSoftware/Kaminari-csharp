using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBaseClient
{
	bool hasPendingSuperPackets();
	ushort firstSuperPacketId();
	byte[] popPendingSuperPacket();

	void disconnect();
	void handlingError();
}
