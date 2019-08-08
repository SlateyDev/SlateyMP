using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace SlateyMP.Framework.Network {
    public class Session {
        public static ConcurrentDictionary<string, Session> Instances { get; } = new ConcurrentDictionary<string, Session>();

        public object Data;

        public static Session GetInstance(IPEndPoint remoteEP, System.Action<Session> sessionFunc) {
            var instanceKey = string.Format("{0}:{1}", remoteEP.Address, remoteEP.Port);
            return Instances.GetOrAdd(instanceKey, x => {
                var newSession = new Session(remoteEP);
                if(sessionFunc != null) {
                    sessionFunc(newSession);
                }
                return newSession;
            });
        }

        public IPEndPoint RemoteEndPoint {get; private set;}
        public bool Validated {get;private set;}
        
        private Session(IPEndPoint remoteEP) {
            RemoteEndPoint = remoteEP;
            Validated = false;
		}
    }
}