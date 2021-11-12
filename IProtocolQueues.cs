using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
    public interface IProtocolQueues
    {
        void reset();
        void ack(ushort blockId);
        void process(ushort tickId, ushort blockId, ref ushort remaining, ref bool unfittingData, SortedDictionary<uint, List<Packet>> byBlock);
    }
}
