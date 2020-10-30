using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immersal.REST
{
    public interface IJobHost
    {
        string server { get; }
        string token { get; }
    }
}
