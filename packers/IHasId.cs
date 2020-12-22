using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHasId : IData
{
    ulong getId();
    void setId(ulong id);
}
