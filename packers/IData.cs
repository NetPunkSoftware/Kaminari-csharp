using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IData
{
	void pack(IMarshal marshal, Packet packet);
	int size(IMarshal marshal);
}
