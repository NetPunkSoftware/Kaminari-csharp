using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public interface IProtocol<PQ> where PQ : IProtocolQueues
	{
		ushort getLastServerID();
		ushort getExpectedBlockId();
		ushort getLastReadID();
		ServerPhaseSync<PQ> getPhaseSync();
		float getServerTimeDiff();
		byte getLoopCounter();
		float getEstimatedRTT();
		float RecvKbpsEstimate();
		float SendKbpsEstimate();
		uint getLastSentSuperPacketSize(SuperPacket<PQ> superpacket);
		uint getLastRecvSuperPacketSize();
		float getPerTickSize();
		void setBufferSize(ushort size);
		void InitiateHandshake(SuperPacket<PQ> superpacket);
		void HandleServerTick(SuperPacketReader reader, SuperPacket<PQ> superpacket);
		void HandleAcks(SuperPacketReader reader, SuperPacket<PQ> superpacket, IMarshal marshal);
		bool read(IBaseClient client, SuperPacket<PQ> superpacket, IMarshal handler);
		bool IsOutOfOrder(ushort id);
		Buffer update(ushort tickId, IBaseClient client, SuperPacket<PQ> superpacket);
	}
}
