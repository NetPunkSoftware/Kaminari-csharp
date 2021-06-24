using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public abstract class Client<PQ> : IBaseClient where PQ : IProtocolQueues
	{
		private byte[] lastPacket;
		private ConcurrentBag<byte[]> pendingPackets;
		private IMarshal marshal;
		private IProtocol<PQ> protocol;
		private SuperPacket<PQ> superPacket;

		public Client(IMarshal marshal, IProtocol<PQ> protocol, PQ queues)
		{
			pendingPackets = new ConcurrentBag<byte[]>();
			this.marshal = marshal;
			this.protocol = protocol;
			this.superPacket = new SuperPacket<PQ>(queues);
		}

		public void InitiateHandshake()
		{
			protocol.InitiateHandshake(this.superPacket);
		}

		public void updateInputs()
		{
			protocol.read(this, superPacket, marshal);
		}

		public void updateOutputs()
		{
			Buffer buffer = protocol.update(this, superPacket);
			if (buffer != null)
			{
				send(buffer);
			}
		}

		public void onReceived(byte[] data)
		{
			lastPacket = data;
			pendingPackets.Add(data);
			protocol.clientHasNewPacket(this, superPacket);
		}

		public PQ getSender()
		{
			return superPacket.getQueues();
		}

		public bool hasPendingSuperPackets()
		{
			return !pendingPackets.IsEmpty;
		}

		public ushort firstSuperPacketId()
		{
			// HACK(gpascualg): A big hack here...
			if (pendingPackets.TryPeek(out var data))
			{
				return (new Buffer(data)).readUshort(2);
			}

			return 0;
		}

		public ushort lastSuperPacketId()
		{
			// HACK(gpascualg): A big hack here...
			if (lastPacket != null)
			{
				return (new Buffer(lastPacket)).readUshort(2);
			}

			return 0;
		}

		public byte[] popPendingSuperPacket()
		{
			if (pendingPackets.TryTake(out var data))
			{
				return data;
			}

			return null;
		}

		// ABSTRACT METHODS LEFT TO IMPLEMENTATION
		protected abstract void send(Buffer buffer);
		public abstract void handlingError();
		public abstract void disconnect();
	}
}
