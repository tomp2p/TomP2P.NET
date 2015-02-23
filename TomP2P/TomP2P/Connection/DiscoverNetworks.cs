using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using TomP2P.Extensions;

namespace TomP2P.Connection
{
    /// <summary>
    /// A class to search for addresses to bind the sockets to. The user
    /// first creates a <see cref="Bindings"/> instance, provides all the
    /// necessary information and then calls DiscoverInterfaces(Bindings).
    /// The results are stored in the <see cref="Bindings"/> instance as well.
    /// </summary>
    public static class DiscoverNetworks
    {
        /// <summary>
        /// Searches for local interfaces. Hints how to search for those interfaces are
        /// provided by the user through the <see cref="Bindings"/> instance.
        /// The results of that search are stored in this <see cref="Bindings"/> instance as well.
        /// </summary>
        /// <param name="bindings">The hints for the search and where to store the results.</param>
        /// <returns>The status of the search.</returns>
        public static string DiscoverInterfaces(Bindings bindings)
        {
            var sb = new StringBuilder("Discover status: ");
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var netInterface in interfaces)
            {
                if (netInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
                if (bindings.AnyInterfaces)
                {
                    sb.Append(" ++").Append(netInterface.Name);
                    sb.Append(DiscoverNetwork(netInterface, bindings)).Append(", ");
                }
                else
                {
                    if (bindings.ContainsInterface(netInterface.Name))
                    {
                        sb.Append(" +").Append(netInterface.Name);
                        sb.Append(DiscoverNetwork(netInterface, bindings)).Append(", ");
                    }
                    else
                    {
                        sb.Append(" -").Append(netInterface.Name);
                    }
                }
            }
            // delete last char
            sb.Remove(sb.Length - 1, 1);
            return sb.Append(".").ToString();
        }

        /// <summary>
        /// Discovers network interfaces and addresses.
        /// </summary>
        /// <param name="networkInterface">The network interface to search for addresses to listen to.</param>
        /// <param name="bindings">The hints for the search and where to store the results.</param>
        /// <returns>The status of the discovery.</returns>
        public static string DiscoverNetwork(NetworkInterface networkInterface, Bindings bindings)
        {
            var sb = new StringBuilder("(");

            // this part is very .NET-specific
            var unicastIpc = networkInterface.GetIPProperties().UnicastAddresses;
            foreach (var addressInfo in unicastIpc)
            {
                if (addressInfo == null)
                {
                    continue;
                }
                IPAddress inet = addressInfo.Address;
                // TODO how to get the broadcast address?
                if (bindings.ContainsAddress(inet))
                {
                    continue;
                }
                // ignore, if a user specifies an address and inet is not part of it
                if (!bindings.AnyAddresses)
                {
                    if (!bindings.Addresses.Contains(inet))
                    {
                        continue;
                    }
                }

                if (inet.IsIPv4() && bindings.IsIPv4)
                {
                    sb.Append(inet).Append(", ");
                    bindings.AddFoundAddress(inet);
                }
                else if (inet.IsIPv6() && bindings.IsIPv6)
                {
                    sb.Append(inet).Append(", ");
                    bindings.AddFoundAddress(inet);
                }
            }

            sb.Remove(sb.Length - 1, 1);
            return sb.Append(")").ToString();
        }
    }
}
