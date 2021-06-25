using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
	public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
	{
		public int Compare(TKey x, TKey y)
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
		protected SortedList<ushort, byte[]> _internalList;
		protected static object _lock = new object();

		public bool IsEmpty => _internalList.Count == 0;

		public ConcurrentList()
		{
			_internalList = new SortedList<ushort, byte[]>(new DuplicateKeyComparer<ushort>());
		}

		public void Add(byte[] obj)
		{
			lock (_lock)
			{
				// HACK(gpascualg): A big hack here...
				ushort id = (new Buffer(obj)).readUshort(2);
				_internalList.Add(id, obj);
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

		public bool PopFirst(out byte[] obj)
		{
			obj = null;

			lock (_lock)
			{
				if (_internalList.Count > 0)
				{
					obj = _internalList.Values[0];
					_internalList.RemoveAt(0);
					return true;
				}
			}

			return false;
		}

		public byte[] PopFirstOrNull()
		{
			lock (_lock)
			{
				if (_internalList.Count > 0)
				{
					byte[] obj = _internalList.Values[0];
					_internalList.RemoveAt(0);
					return obj;
				}
			}

			return null;
		}
	}


	public abstract class Client<PQ> : IBaseClient where PQ : IProtocolQueues
	{
		private byte[] lastPacket;
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
			lastPacket = data;
			pendingPackets.Add(data);
			protocol.clientHasNewPacket(this, superPacket);
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
			// HACK(gpascualg): A big hack here...
			if (lastPacket != null)
			{
				return (new Buffer(lastPacket)).readUshort(2);
			}

			return 0;
		}

		public byte[] popPendingSuperPacket()
		{
			return pendingPackets.PopFirstOrNull();
		}

		// ABSTRACT METHODS LEFT TO IMPLEMENTATION
		protected abstract void send(Buffer buffer);
		public abstract void handlingError();
		public abstract void disconnect();
	}
}
