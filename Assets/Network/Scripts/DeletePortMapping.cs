using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Open.Nat;

public class DeletePortMapping : MonoBehaviour
{
    public static void Delete()
    {
        var t = Task.Run(async () =>
        {
            var nat = new NatDiscoverer();
            var cts = new CancellationTokenSource(5000);
            var device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            var ip = await device.GetExternalIPAsync();
     
            await device.DeletePortMapAsync(new Mapping(Protocol.Tcp, 7777, 7777));
            Debug.Log("deletemapping");

        }

        );

        try
        {
            t.Wait();
        }
        catch (AggregateException e)
        {
            if(e.InnerException is NatDeviceNotFoundException)
            {
                Debug.Log("Not Found");
            }
        }
    }
}
