using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection
{
    /// <summary>
    /// A class to search for addresses to bind the sockets to. The user
    /// first creates a <see cref="Bindings"/> instance, provides all the
    /// necessary information and then calls DiscoverInterfaces(Bindings).
    /// The results are stored in the <see cref="Bindings"/> instance as well.
    /// </summary>
    public sealed class DiscoverNetworks
    {
        public DiscoverNetworks()
        { }

        /// <summary>
        /// Searches for local interfaces. Hints how to search for those interfacesare
        /// provided by the user through the <see cref="Bindings"/> instance.
        /// The results of that search are stored in this <see cref="Bindings"/> instance as well.
        /// </summary>
        /// <param name="bindings">The hints for the search and where to store the results.</param>
        /// <returns>The status of the search.</returns>
        public static string DiscoverInterfaces(Bindings bindings)
        {
            var sb = new StringBuilder("Discover status: ");
            var e = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var netInterface in e)
            {
                if (bindings.AnyInterfaces)
                {
                    sb.Append(" ++").Append(netInterface.Name); // TODO correct name property?
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
            var sb = new StringBuilder("( ");
            throw new NotImplementedException();
        }
    }
}
