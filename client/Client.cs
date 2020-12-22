using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public abstract class Client<PQ> : IBaseClient where PQ : IProtocolQueues
{
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

	public void updateInputs()
	{
		// TODO: Populate pendingPackets somewhere
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
	
	public void tryReceive()
	{
		// TODO(gpascualg): Magic numbers
		byte[] data = new byte[500];
		receive(ref data);
		pendingPackets.Add(data);
	}
	
	public PQ getSender()
	{
		return superPacket.getQueues();
	}

	public void disconnect()
	{

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
	protected abstract void receive(ref byte[] packet);
	public abstract void handlingError();
}
