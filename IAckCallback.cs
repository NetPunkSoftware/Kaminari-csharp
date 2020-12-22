using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAckCallback
{
    void onAck(Packet packet);
}
