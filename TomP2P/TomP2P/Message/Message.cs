using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Peers;

namespace TomP2P.Message
{
    public class Message
    {
        // used for creating random message id
        private static readonly Random Random = new Random();

        public const int ContentTypeLength = 8;

        /// <summary>
        /// 8 x 4 bit.
        /// </summary>
        public enum Content
        {
            Empty, Key, MapKey640Data, MapKey640Keys, SetKey640, SetNeighbors, ByteBuffer,
            Long, Integer, PublicKeySignature, SetTrackerData, BloomFilter, MapKey640Byte,
            PublicKey, SetPeerSocket, User1
        }

        /// <summary>
        /// 4 x 1 bit.
        /// </summary>
        public enum Type
        {
            // REQUEST_1 is the normal request
            // REQUEST_2 for GET returns the extended digest (hashes of all stored data)
            // REQUEST_3 for GET returns a Bloom filter
            // REQUEST_4 for GET returns a range (min/max)
            // REQUEST_2 for PUT/ADD/COMPARE_PUT means protect domain
            // REQUEST_3 for PUT means put if absent
            // REQUEST_3 for COMPARE_PUT means partial (partial means that put those data that match compare, ignore others)
            // REQUEST_4 for PUT means protect domain and put if absent
            // REQUEST_4 for COMPARE_PUT means partial and protect domain
            // REQUEST_2 for REMOVE means send back results
            // REQUEST_2 for RAW_DATA means serialize object
            // *** NEIGHBORS has four different cases
            // REQUEST_1 for NEIGHBORS means check for put (no digest) for tracker and storage
            // REQUEST_2 for NEIGHBORS means check for get (with digest) for storage
            // REQUEST_3 for NEIGHBORS means check for get (with digest) for tracker
            // REQUEST_4 for NEIGHBORS means check for put (with digest) for task
            // REQUEST_FF_1 for PEX means fire and forget, coming from mesh
            // REQUEST_FF_1 for PEX means fire and forget, coming from primary
            // REQUEST_1 for TASK is submit new task
            // REQUEST_2 for TASK is status
            // REQUEST_3 for TASK is send back result
            Request1, Request2, Request3, Request4, RequestFf1, RequestFf2, Ok,
            PartiallyOk, NotFound, Denied, UnknownId, Exception, Cancel, User1, User2
        }

        // Header:
        private int _messageId;
        private int _version;
        private Type _type;

        /* commands so far:
         *  0: PING
         *  1: PUT
         *  2: GET
         *  3: ADD
         *  4: REMOVE
         *  5: NEIGHBORS
         *  6: QUIT
         *  7: DIRECT_DATA
         *  8: TRACKER_ADD
         *  9: TRACKER_GET
         * 10: PEX
         * 11: TASK
         * 12: BROADCAST_DATA*/
        private byte _command;
        private PeerAddress _sender;
        private PeerAddress _recipient;
        private PeerAddress _recipientRelay;
        private int _options = 0;

        // Payload:
        // we can send 8 types
        private Content[] _contentTypes = new Content[ContentTypeLength];
        private readonly Queue<NumberType> _contentReferences = new Queue<NumberType>();

        // following the payload objects:
        // content lists:

    }
}
