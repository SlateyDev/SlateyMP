using System;
using System.Collections.Generic;
using System.Net;
using SlateyMP.Framework.Network;

namespace SlateyMP.Framework.Util {
    public delegate void OpcodeDelegate(Session session, UDPReceiver receiver);

    public static class ReceiveOpcodes {
		private static Dictionary<UInt16, OpcodeHandler> _opcodeMap;

		public class OpcodeHandler {
			public string name;
			public OpcodeDelegate OnReceive;
		}

		static ReceiveOpcodes() {
			_opcodeMap = new Dictionary<UInt16, OpcodeHandler>();
		}

		public static void StoreOpcode(UInt16 Opcode, OpcodeDelegate handler) {
			_opcodeMap.Add(Opcode, new OpcodeHandler { name = Opcode.ToString(), OnReceive = handler });
		}

		public static void RemoveOpcode(UInt16 Opcode) {
			_opcodeMap.Remove(Opcode);
		}

		public static OpcodeHandler GetOpcodeHandler(UInt16 id) {
			if (_opcodeMap.TryGetValue(id, out OpcodeHandler handler)) {
				return handler;
			}
			return null;
		}
	}
}