using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immersal.Samples
{
    public interface IJobHost
    {
        string server { get; }
        string token { get; }
    }
}
