﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.NET_Helper
{
    public static class ConnectionHelper
    {
        /// <summary>
        /// Extracts the sender's IPEndPoint from the message.
        /// In Java, this is done in TomP2POutbound.write().
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IPEndPoint ExtractSenderEp(Message.Message message)
        {
            IPEndPoint sender;
            if (message.SenderSocket == null)
            {
                // in case of a request
                sender = message.Sender.CreateSocketUdp();
            }
            else
            {
                // in case of a reply
                sender = message.RecipientSocket;
            }
            return sender;
        }

        /// <summary>
        /// Extracts the receiver's IPEndPoint from the message.
        /// In Java, this is done in TomP2POutbound.write().
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IPEndPoint ExtractReceiverEp(Message.Message message)
        {
            IPEndPoint recipient;
            if (message.SenderSocket == null)
            {
                // in case of a request
                if (message.RecipientRelay != null)
                {
                    // in case of sending to a relay (the relayed flag is already set)
                    recipient = message.RecipientRelay.CreateSocketUdp();
                }
                else
                {
                    recipient = message.Recipient.CreateSocketUdp();
                }
            }
            else
            {
                // in case of a reply
                recipient = message.SenderSocket;
            }
            return recipient;
        }
    }
}