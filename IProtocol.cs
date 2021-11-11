using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public interface IProtocol<PQ> where PQ : IProtocolQueues
	{
		ushort ExpectedTickId { get; }
		ushort LastServerId { get; }
		ushort LastTickIdRead { get; }
		float ServerTimeDiff { get; }

		ServerPhaseSync<PQ> getPhaseSync();
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
		void HandleAcks(ushort tickId, SuperPacketReader reader, SuperPacket<PQ> superpacket, IMarshal marshal);
		bool read(IBaseClient client, SuperPacket<PQ> superpacket, IMarshal handler);
		bool IsOutOfOrder(ushort id);
		Buffer update(ushort tickId, IBaseClient client, SuperPacket<PQ> superpacket);
	}
}
