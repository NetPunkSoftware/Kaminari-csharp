using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buffer
{
	private byte[] _body;
	private int _offset;
	private int _index;

	public Buffer()
	{
		_body = new byte[500];
		_offset = 0;
		_index = 5;
	}

	public Buffer(Buffer other)
	{
		_body = (byte[])other._body.Clone();
		_offset = other._offset;
		_index = other._index;
	}

	public Buffer(Buffer other, int offset, int index)
	{
		_body = other._body;
		_offset = offset;
		_index = index;
	}

	public Buffer(byte[] data)
	{
		_body = data;
		_offset = 0;
		_index = 0;
	}

	public int getPosition()
	{
		return _index;
	}

	public void reset()
	{
		_offset = 0;
		_index = 0;
	}

	public int getSize()
	{
		return _body.Length;
	}

	public void write(byte value)
	{
		write(_index, value);
		_index += sizeof(byte);
	}

	public void write(char value)
	{
		write(_index, (byte)value);
		_index += sizeof(byte);
	}

	public void write(bool value)
	{
		write(_index, (byte)(value ? 1 : 0));
		_index += sizeof(byte);
	}

	public void write(short value)
	{
		write(_index, (ushort)value);
		_index += sizeof(ushort);
	}

	public void write(ushort value)
	{
		write(_index, value);
		_index += sizeof(ushort);
	}

	public void write(int value)
	{
		write(_index, (uint)value);
		_index += sizeof(uint);
	}

	public void write(uint value)
	{
		write(_index, value);
		_index += sizeof(uint);
	}

	public void write(long value)
	{
		write(_index, (ulong)value);
		_index += sizeof(ulong);
	}

	public void write(ulong value)
	{
		write(_index, value);
		_index += sizeof(ulong);
	}

	public void write(int position, byte value)
	{
		_body[position] = value;
	}

	public void write(int position, ushort value)
	{
		_body[position + 1] = (byte)(value >> 8);
		_body[position] = (byte)(value);

		_index += sizeof(ushort);
	}

	public void write(int position, uint value)
	{
		_body[position + 3] = (byte)(value >> 24);
		_body[position + 2] = (byte)(value >> 16);
		_body[position + 1] = (byte)(value >> 8);
		_body[position] = (byte)(value);

		_index += sizeof(uint);
	}

	public void write(int position, ulong value)
	{
		_body[position + 7] = (byte)(value >> 56);
		_body[position + 6] = (byte)(value >> 48);
		_body[position + 5] = (byte)(value >> 40);
		_body[position + 4] = (byte)(value >> 32);
		_body[position + 3] = (byte)(value >> 24);
		_body[position + 2] = (byte)(value >> 16);
		_body[position + 1] = (byte)(value >> 8);
		_body[position] = (byte)(value);

		_index += sizeof(ulong);
	}

	public void write(Packet packet)
	{
		Buffer other = packet.getData();
		for (int i = 0; i < other.getPosition(); ++i)
		{
			_body[_offset + _index++] = other._body[i];
		}
	}

	public void write(string str)
	{
		write((byte)str.Length);
		for (int i = 0; i < str.Length; ++i)
		{
			write((byte)str[i]);
		}
	}

	public char readChar()
	{
		return (char)readByte(_index++);
	}

	public byte readByte()
	{
		return readByte(_index++);
	}

	public byte readByte(int pos)
	{
		return _body[_offset + pos];
	}

	public short readShort()
	{
		short value = (short)readUshort(_index);
		_index += sizeof(ushort);
		return value;
	}

	public ushort readUshort()
	{
		ushort value = readUshort(_index);
		_index += sizeof(ushort);
		return value;
	}

	public ushort readUshort(int pos)
	{
		ushort result = (ushort)(_body[_offset + pos + 1] << 8 | _body[_offset + pos]);
		return result;
	}

	public int readInt()
	{
		int value = (int)readUint(_index);
		_index += sizeof(int);
		return value;
	}

	public uint readUint()
	{
		uint value = readUint(_index);
		_index += sizeof(uint);
		return value;
	}

	public uint readUint(int pos)
	{
		uint result = (uint)(_body[_offset + pos + 3] << 24 |
			_body[_offset + pos + 2] << 16 |
			_body[_offset + pos + 1] << 8 |
			_body[_offset + pos]);
		return result;
	}

	public long readLong()
	{
		long value = (long)readUlong(_index);
		_index += sizeof(long);
		return value;
	}

	public ulong readUlong()
	{
		ulong value = readUlong(_index);
		_index += sizeof(ulong);
		return value;
	}

	public ulong readUlong(int pos)
	{
		ulong result = (ulong)(_body[_offset + pos + 7] << 56 |
			_body[_offset + pos + 6] << 48 |
			_body[_offset + pos + 5] << 40 |
			_body[_offset + pos + 4] << 32 |
			_body[_offset + pos + 3] << 24 |
			_body[_offset + pos + 2] << 16 |
			_body[_offset + pos + 1] << 8 |
			_body[_offset + pos]);
		return result;
	}

	public float readFloat()
	{
		float value = readFloat(_index);
		_index += sizeof(float);
		return value;
	}

	public float readFloat(int pos)
	{
		return Convert.ToSingle(BitConverter.ToSingle(_body, _offset + pos));
	}

	public double readDouble()
	{
		double value = readDouble(_index);
		_index += sizeof(double);
		return value;
	}

	public double readDouble(int pos)
	{
		return Convert.ToDouble(BitConverter.ToDouble(_body, _offset + pos));
	}

	public bool readBool()
	{
		return _body[_offset + _index++] == (byte)1;
	}

	public string readString()
	{
		string str = "";
		int len = readByte();
		for (int i = 0; i < len; ++i)
		{
			str += (char)readByte();
		}
		return str;
	}

	public byte peekByte()
	{
		return readByte(_index);
	}
}
