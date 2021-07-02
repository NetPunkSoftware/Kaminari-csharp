using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace Kaminari
{
	public class DuplicateKeyComparer : IComparer<ushort>
	{
		public int Compare(ushort x, ushort y)
		{
			int result = x.CompareTo(y);

			if (result == 0)
				return 1; // Handle equality as being greater. Note: this will break Remove(key) or
			else          // IndexOfKey(key) since the comparer never returns 0 to signal key equality
				return result;
		}
	}

	public class ConcurrentList
	{
		protected SortedList<ushort, SuperPacketReader> _internalList;
		protected static object _lock = new object();

		public bool IsEmpty => _internalList.Count == 0;

		public ConcurrentList()
		{
			_internalList = new SortedList<ushort, SuperPacketReader>(new DuplicateKeyComparer());
		}

		public void Add(SuperPacketReader reader)
		{
			lock (_lock)
			{
				_internalList.Add(reader.id(), reader);
			}
		}

		public ushort Peek()
		{
			lock (_lock)
			{
				if (_internalList.Count > 0)
				{
					return _internalList.Keys[0];
				}
			}

			return 0;
		}

		public ushort PeekLast()
		{
			lock (_lock)
			{
				if (_internalList.Count > 0)
				{
					return _internalList.Keys[_internalList.Count - 1];
				}
			}

			return 0;
		}

		public ushort PeekLastSize()
		{
			lock (_lock)
			{
				if (_internalList.Count > 0)
				{
					SuperPacketReader reader = _internalList.Values[_internalList.Count - 1];
					return reader.length();
				}
			}

			return 0;
		}

		public bool PopFirst(out SuperPacketReader reader)
		{
			reader = null;

			lock (_lock)
			{
				if (_internalList.Count > 0)
				{
					reader = _internalList.Values[0];
					_internalList.RemoveAt(0);
					return true;
				}
			}

			return false;
		}

		public SuperPacketReader PopFirstOrNull()
		{
			lock (_lock)
			{
				if (_internalList.Count > 0)
				{
					SuperPacketReader reader = _internalList.Values[0];
					_internalList.RemoveAt(0);
					return reader;
				}
			}

			return null;
		}
	}


	public abstract class Client<PQ> : IBaseClient where PQ : IProtocolQueues
	{
		private ushort lastPacketID;
		private ushort lastPacketSize;
		private ConcurrentList pendingPackets;
		private IMarshal marshal;
		private IProtocol<PQ> protocol;
		private SuperPacket<PQ> superPacket;

		public Client(IMarshal marshal, IProtocol<PQ> protocol, PQ queues)
		{
			pendingPackets = new ConcurrentList();
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
			SuperPacketReader reader = new SuperPacketReader(data);
			if (protocol.IsOutOfOrder(reader.id()))
			{
				return;
			}

			// Add to pending list
			pendingPackets.Add(reader);
			lastPacketID = reader.id();
			lastPacketSize = reader.length();

			// Handle all acks already
			protocol.HandleAcks(reader, superPacket);
		}

		public IProtocol<PQ> getProtocol()
		{
			return protocol;
		}

		public SuperPacket<PQ> getSuperPacket()
		{
			return superPacket;
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
			return pendingPackets.Peek();
		}

		public ushort lastSuperPacketId()
		{
			return lastPacketID;
		}

		public ushort lastSuperPacketSize()
		{
			return lastPacketSize;
		}

		public SuperPacketReader popPendingSuperPacket()
		{
			return pendingPackets.PopFirstOrNull();
		}

		// ABSTRACT METHODS LEFT TO IMPLEMENTATION
		protected abstract void send(Buffer buffer);
		public abstract void handlingError();
		public abstract void disconnect();
	}
}
