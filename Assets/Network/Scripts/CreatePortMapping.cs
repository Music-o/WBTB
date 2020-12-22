using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Open.Nat;
using System.Net;
using System.Net.Sockets;
using System.Linq;

public class CreatePortMapping : MonoBehaviour
{
    public static void Create()
    {
        var t = Task.Run(async () =>
        {
            var nat = new NatDiscoverer();
            var cts = new CancellationTokenSource(5000);
            var device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            var ip = await device.GetExternalIPAsync();

            var mappings = await device.GetAllMappingsAsync();
            var mappingList = mappings.All(x => x.Description != "WBTB PORT");
            if(mappingList)
            {
                await device.DeletePortMapAsync(new Mapping(Protocol.Tcp, 7777, 7777));
                await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 7777, 7777, "WBTB PORT"));

                var endpoint = new IPEndPoint(IPAddress.Any, 7777);
                var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                socket.Bind(endpoint);
                socket.Listen(100);
            }

        }

        );

        try
        {
            t.Wait();
        }
        catch (AggregateException e)
        {
            if (e.InnerException is NatDeviceNotFoundException)
            {
                Debug.Log("Not Found");
            }
        }
    }

}
