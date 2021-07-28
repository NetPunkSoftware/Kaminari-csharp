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
		int getServerTimeDiff();
		byte getLoopCounter();
		float getEstimatedRTT();
		float RecvKbpsEstimate();
		float SendKbpsEstimate();
		ushort getLastSentSuperPacketSize(SuperPacket<PQ> superpacket);
		ushort getLastRecvSuperPacketSize(IBaseClient client);
		void setBufferSize(ushort size);
		void InitiateHandshake(SuperPacket<PQ> superpacket);
		void HandleServerTick(SuperPacketReader reader, SuperPacket<PQ> superpacket);
		void HandleAcks(SuperPacketReader reader, SuperPacket<PQ> superpacket);
		bool read(IBaseClient client, SuperPacket<PQ> superpacket, IHandlePacket handler);
		bool IsOutOfOrder(ushort id);
		Buffer update(IBaseClient client, SuperPacket<PQ> superpacket);
	}
}
