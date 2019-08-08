
using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace SlateyMP.Server.Login {
	public enum AccessLevel {
		Player = 0,
		GameMaster
	}

	public class ClientInfo {
		public string Account = "";
		public string UpdateFile = "";
		public AccessLevel Access = AccessLevel.Player;

		public BigInteger g = new BigInteger(new byte[] { 7 });
		public BigInteger N = new BigInteger(new byte[] { 137, 75, 100, 94, 137, 225, 83, 91, 189, 173, 91, 139, 41, 6, 80, 83, 8, 1, 177, 142, 191, 191, 94, 143, 171, 60, 130, 135, 42, 62, 155, 183, 0 });
		public BigInteger k = new BigInteger(new byte[] { 3 });
		public BigInteger PublicA;
		public BigInteger Salt;
		public BigInteger Verifier;
		public BigInteger PublicB;
		public BigInteger M1;
		public byte[] SessionKey;
	}
}
