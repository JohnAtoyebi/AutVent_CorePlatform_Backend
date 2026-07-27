using System.Net.NetworkInformation;
using AutVent.Application.Abstractions.System;

namespace AutVent.Infrastructure.System;

public sealed class NetworkConnectivityService : IConnectivityService
{
    public bool IsOnline() => NetworkInterface.GetIsNetworkAvailable();
}
