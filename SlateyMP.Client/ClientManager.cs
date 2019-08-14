using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using SlateyMP.Framework;
using SlateyMP.Framework.Network;
using SlateyMP.Framework.Util;

namespace SlateyMP.Client {
    public static class ClientManager {
        public static Socket loginSocket;
        public static IPEndPoint loginEndpoint;

        private static BigInteger g = new BigInteger(new byte[] { 7 });
        private static BigInteger N = new BigInteger(new byte[] { 137, 75, 100, 94, 137, 225, 83, 91, 189, 173, 91, 139, 41, 6, 80, 83, 8, 1, 177, 142, 191, 191, 94, 143, 171, 60, 130, 135, 42, 62, 155, 183, 0 });
        private static BigInteger k = new BigInteger(new byte[] { 3 });
        private static BigInteger PublicA;
        private static BigInteger PublicB;
        private static BigInteger Salt;
        private static BigInteger a;

		private const string USERNAME = "slatey";
		private const string PASSWORD = "test";

        private const byte BUILD_MAJOR = 4;
        private const byte BUILD_MINOR = 3;
        private const byte BUILD_REVISION = 2;
        private const short CLIENT_BUILD = 1;

        public static void Run() {
            Core.Initialize();
            RegisterOpcodes();
            loginSocket = Core.StartUDPReceiver(IPAddress.Parse("127.0.0.1"), 0, OnReceive);
            loginEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);

            var random = new RNGCryptoServiceProvider();
            var randoma = new byte[20];
            random.GetBytes(randoma);
            randoma[randoma.Length - 1] &= 0x7F;
            a = new BigInteger(randoma);
            PublicA = BigInteger.ModPow(g, a, N);

            var PublicABytes = PublicA.ToByteArray();

            UDPTransmitter transmitter = UDPTransmitter.CreateObject();
            transmitter.WriteUint16((UInt16)CMSG_AUTH_LOGON_CHALLENGE);      //opcode
            transmitter.WriteUint16((UInt16)(9 + USERNAME.Length + PublicABytes.Length));    //packet_length
            transmitter.WriteUint8(BUILD_MAJOR);
            transmitter.WriteUint8(BUILD_MINOR);
            transmitter.WriteUint8(BUILD_REVISION);
            transmitter.WriteInt16(CLIENT_BUILD);
            transmitter.WriteUint16((UInt16)USERNAME.Length);
            transmitter.WriteFixedString(USERNAME);
            transmitter.WriteUint16((UInt16)PublicABytes.Length);
            transmitter.WriteFixedBlob(PublicABytes);
            transmitter.SendTo(loginSocket, loginEndpoint);
        }

        public static void OnReceive(UDPReceiver receiver, SocketAsyncEventArgs e) {
            var remoteEP = (IPEndPoint)e.RemoteEndPoint;

            if(e.BytesTransferred < 2) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Invalid message received", DateTime.Now, remoteEP.Address, remoteEP.Port);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            UInt16 packetID = receiver.ReadUint16();
            var handler = ReceiveOpcodes.GetOpcodeHandler(packetID);
            if (handler == null) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Unknown Opcode {3}", DateTime.Now, remoteEP.Address, remoteEP.Port, packetID);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            var session = Session.GetInstance(remoteEP, null);
            handler.OnReceive(session, receiver);
        }

		private const int REQUIRED_BUILD_LOW = 1;
		private const int REQUIRED_BUILD_HIGH = 1;

		public enum AccountState : byte {
			//RealmServ Error Codes
			LOGIN_OK = 0x0,
			LOGIN_FAILED = 0x1,             //Unable to connect
			LOGIN_BANNED = 0x3,             //This account has been closed and is no longer in service.
			LOGIN_UNKNOWN_ACCOUNT = 0x4,    //The information you have entered is not valid.  Please check the spelling of the account name and password.  If you need help in retrieving a lost or stolen password and account, see www.worldofwarcraft.com for more information.
			LOGIN_ALREADYONLINE = 0x6,      //This account is already logged in.  Please check the spelling and try again.
			LOGIN_NOTIME = 0x7,             //You have used up your prepaid time for this account. Please purchase more to continue playing.
			LOGIN_DBBUSY = 0x8,             //Could not log in at this time. Please try again later.
			LOGIN_BADVERSION = 0x9,         //Unable to validate game version. This may be caused by file corruption or the interference of another program.
			LOGIN_SUSPENDED = 0xC,          //This account has been temporarily suspended.
		}

        private const UInt16 CMSG_AUTH_LOGON_CHALLENGE = 100;
		private const UInt16 SMSG_AUTH_LOGON_CHALLENGE_OK = 101;
		private const UInt16 SMSG_AUTH_LOGON_CHALLENGE_FAIL = 102;
		private const UInt16 CMSG_AUTH_LOGON_PROOF = 103;
		private const UInt16 SMSG_AUTH_LOGON_PROOF_OK = 104;
		private const UInt16 SMSG_AUTH_LOGON_PROOF_FAIL = 105;
		private const UInt16 CMSG_REALMLIST = 120;
		private const UInt16 SMSG_REALMLIST_RESPONSE = 121;

		public static void RegisterOpcodes() {
			ReceiveOpcodes.StoreOpcode((UInt16)SMSG_AUTH_LOGON_CHALLENGE_OK, new OpcodeDelegate(OnLogonChallengeOK));
			ReceiveOpcodes.StoreOpcode((UInt16)SMSG_AUTH_LOGON_CHALLENGE_FAIL, new OpcodeDelegate(OnLogonChallengeFail));
			ReceiveOpcodes.StoreOpcode((UInt16)SMSG_AUTH_LOGON_PROOF_OK, new OpcodeDelegate(OnLogonProofOK));
			ReceiveOpcodes.StoreOpcode((UInt16)SMSG_AUTH_LOGON_PROOF_FAIL, new OpcodeDelegate(OnLogonProofFail));

			// ReceiveOpcodes.StoreOpcode((UInt16)SMSG_REALMLIST_RESPONSE, new OpcodeDelegate(OnRealmListResponse));
		}

		private static void OnLogonChallengeFail(Session session, UDPReceiver data) {
            // Do nothing. Connection failed.
        }

		private static void OnLogonChallengeOK(Session session, UDPReceiver data) {
			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] CMD_LOGON_CHALLENGE", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port);
			UInt16 packet_size = data.ReadUint16();
            UInt16 B_bytes_length = data.ReadUint16();
            byte[] B_bytes = new byte[B_bytes_length];
            PublicB = new BigInteger(data.ReadFixedBlob(ref B_bytes, B_bytes_length));
            UInt16 S_bytes_length = data.ReadUint16();
            byte[] S_bytes = new byte[S_bytes_length];
            Salt = new BigInteger(data.ReadFixedBlob(ref S_bytes, S_bytes_length));

			SHA256Managed sha = new SHA256Managed();
			var u = new BigInteger(sha.ComputeHash(PublicA.ToByteArray().Concat(PublicB.ToByteArray()).ToArray()).Concat(new byte[] { 0 }).ToArray());
			byte[] passwordHash = sha.ComputeHash(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", USERNAME, PASSWORD.ToUpper())));
			var x = new BigInteger(sha.ComputeHash(passwordHash.Concat(Salt.ToByteArray()).ToArray()).Concat(new byte[] { 0 }).ToArray());
			var S = BigInteger.ModPow(PublicB - k * BigInteger.ModPow(g, x, N), (a + u * x), N);
			if (S < 0) S = S + N;  //C# incorrectly calculates the mod of negative numbers

			var sessionkey = sha.ComputeHash(S.ToByteArray());

			var M1 = sha.ComputeHash(PublicA.ToByteArray().Concat(PublicB.ToByteArray()).Concat(sessionkey).ToArray());

			using (MemoryStream ms = new MemoryStream()) {
				using(BinaryWriter bw = new BinaryWriter(ms)) {
					bw.Write(M1, 0, M1.Length);
				}

				byte[] messageBody;
				messageBody = ms.ToArray();

				using (MemoryStream ms1 = new MemoryStream()) {
                    UDPTransmitter transmitter = UDPTransmitter.CreateObject();
                    transmitter.WriteUint16(CMSG_AUTH_LOGON_PROOF);
                    transmitter.WriteUint16((UInt16)messageBody.Length);
                    transmitter.WriteFixedBlob(messageBody);
                    transmitter.SendTo(loginSocket, loginEndpoint);
				}
			}
		}

		private static void OnLogonProofFail(Session session, UDPReceiver data) {
            //Do nothing. Connection failed
        }

		private static void OnLogonProofOK(Session session, UDPReceiver data) {
			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] CMD_AUTH_LOGON_PROOF", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port);
		}
    }
}