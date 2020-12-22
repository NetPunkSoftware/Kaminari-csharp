using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProtocol<PQ> where PQ : IProtocolQueues
{
	bool read(IBaseClient client, SuperPacket<PQ> superpacket, IHandlePacket handler);
	Buffer update(IBaseClient client, SuperPacket<PQ> superpacket);
}
