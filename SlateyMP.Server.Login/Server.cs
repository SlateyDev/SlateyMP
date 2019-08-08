using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using SlateyMP.Framework.Network;
using SlateyMP.Framework.Util;

namespace SlateyMP.Server.Login {
    public static class Server {
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
			ReceiveOpcodes.StoreOpcode((UInt16)CMSG_AUTH_LOGON_CHALLENGE, new OpcodeDelegate(OnLogonChallenge));
			ReceiveOpcodes.StoreOpcode((UInt16)CMSG_AUTH_LOGON_PROOF, new OpcodeDelegate(OnLogonProof));

			ReceiveOpcodes.StoreOpcode((UInt16)CMSG_REALMLIST, new OpcodeDelegate(OnRealmList));
		}

		private static void OnLogonChallenge(Session session, UDPReceiver data) {
			var sessionData = (ClientInfo)session.Data;

			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] CMD_LOGON_CHALLENGE", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port);
			UInt16 packet_size = data.ReadUint16();
			int bMajor = data.ReadUint8();
			int bMinor = data.ReadUint8();
			int bRevision = data.ReadUint8();
			int ClientBuild = data.ReadInt16();
			UInt16 usernameLen = data.ReadUint16();
			sessionData.Account = data.ReadFixedString(usernameLen);
			UInt16 publicALen = data.ReadUint16();
			byte[] publicABytes = new byte[publicALen];
			sessionData.PublicA = new BigInteger(data.ReadFixedBlob(ref publicABytes, publicALen));
			AccountState acc_state = AccountState.LOGIN_DBBUSY;

			string PassString = string.Empty;

			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] CMD_AUTH_LOGON_CHALLENGE [{3}], Client Version [{4}.{5}.{6}.{7}].", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account, bMajor, bMinor, bRevision, ClientBuild);
			if ((ClientBuild >= REQUIRED_BUILD_LOW) && (ClientBuild <= REQUIRED_BUILD_HIGH)) {
				try {
					MySqlCommand cmd = new MySqlCommand(string.Format("SELECT * FROM account WHERE username = '{0}'", sessionData.Account), Program.db);
					using (MySqlDataReader reader = cmd.ExecuteReader()) {
						if (reader.Read()) {
							if ((int)reader["banned"] == 1) {
								acc_state = AccountState.LOGIN_BANNED;
							}
							else {
								acc_state = AccountState.LOGIN_OK;
								PassString = (string)reader["passwordHash"];
								sessionData.Verifier = BigInteger.Parse((string)reader["verifier"]);
								sessionData.Salt = BigInteger.Parse((string)reader["salt"]);
								sessionData.Access = (AccessLevel)reader["accesslevel"];
							}
						}
						else {
							acc_state = AccountState.LOGIN_UNKNOWN_ACCOUNT;
						}
					}
				}
				catch (Exception ex) {
					Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Data Error [{3}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, ex.ToString());
					acc_state = AccountState.LOGIN_DBBUSY;
				}

				switch (acc_state) {
					case AccountState.LOGIN_OK:
						Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Account found [{3}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account);

						byte[] account = Encoding.UTF8.GetBytes(sessionData.Account);
						byte[] passwordHash = Convert.FromBase64String(PassString);

						var random = new RNGCryptoServiceProvider();
						var randomb = new byte[20];
						random.GetBytes(randomb);
						randomb[randomb.Length - 1] &= 0x7F;
						var b = new BigInteger(randomb);
						sessionData.PublicB = ((sessionData.k * sessionData.Verifier) + BigInteger.ModPow(sessionData.g, b, sessionData.N)) % sessionData.N;

						SHA256Managed sha = new SHA256Managed();
						var u = new BigInteger(sha.ComputeHash(sessionData.PublicA.ToByteArray().Concat(sessionData.PublicB.ToByteArray()).ToArray()).Concat(new byte[] { 0 }).ToArray());

						var S = BigInteger.ModPow((sessionData.PublicA * BigInteger.ModPow(sessionData.Verifier, u, sessionData.N)), b, sessionData.N);
						sessionData.SessionKey = sha.ComputeHash(S.ToByteArray());

						sessionData.M1 = new BigInteger(sha.ComputeHash(sessionData.PublicA.ToByteArray().Concat(sessionData.PublicB.ToByteArray()).Concat(sessionData.SessionKey).ToArray()).Concat(new byte[] { 0 }).ToArray());

						var dsOK = UDPTransmitter.CreateObject();
						dsOK.WriteUint16((UInt16)SMSG_AUTH_LOGON_CHALLENGE_OK);
						byte[] B_bytes = sessionData.PublicB.ToByteArray();
						byte[] S_bytes = sessionData.Salt.ToByteArray();
						dsOK.WriteUint16((UInt16)(4 + B_bytes.Length + S_bytes.Length));    //packet_length
						dsOK.WriteUint16((UInt16)B_bytes.Length);
						dsOK.WriteFixedBlob(B_bytes);
						dsOK.WriteUint16((UInt16)S_bytes.Length);
						dsOK.WriteFixedBlob(S_bytes);
						dsOK.SendTo(data.ReceiveSocket, session.RemoteEndPoint);
						return;
					case AccountState.LOGIN_UNKNOWN_ACCOUNT:
						Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Account not found [{3}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account);
						break;
					case AccountState.LOGIN_BANNED:
						Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Account banned [{3}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account);
						break;
					case AccountState.LOGIN_NOTIME:
						Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Account prepaid time used [{3}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account);
						break;
					case AccountState.LOGIN_ALREADYONLINE:
						Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Account already logged in the game [{3}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account);
						break;
					default:
						Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Account error [{3}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account);
						break;
				}
				var ds = UDPTransmitter.CreateObject();
				ds.WriteUint16((UInt16)SMSG_AUTH_LOGON_CHALLENGE_FAIL);
				ds.WriteUint8((byte)acc_state);
				ds.SendTo(data.ReceiveSocket, session.RemoteEndPoint);
			}
			else {
				Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] WRONG_VERSION [{3}.{4}.{5}.{6}]",
					DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, bMajor, bMinor, bRevision, ClientBuild);

				var ds = UDPTransmitter.CreateObject();
				ds.WriteUint16((UInt16)SMSG_AUTH_LOGON_CHALLENGE_FAIL);
				ds.WriteUint8((byte)AccountState.LOGIN_BADVERSION);
				ds.SendTo(data.ReceiveSocket, session.RemoteEndPoint);
			}
		}

		private static void OnLogonProof(Session session, UDPReceiver data) {
			var sessionData = (ClientInfo)session.Data;

			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] CMD_AUTH_LOGON_PROOF", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port);

			BigInteger ClientM1;

			var M1len = data.ReadInt16();
			var M1data = new byte[M1len];

			using (MemoryStream ms = new MemoryStream(data.Buffer, data.CurrentReadPos, data.RemainingBytes)) {
				using (BinaryReader br = new BinaryReader(ms)) {
					br.Read(M1data, 0, M1len);
					ClientM1 = new BigInteger(M1data.Concat(new byte[] { 0 }).ToArray());
				}
			}

			if (ClientM1 == sessionData.M1) {
				var ds = UDPTransmitter.CreateObject();
				ds.WriteUint16((UInt16)SMSG_AUTH_LOGON_PROOF_OK);
				ds.SendTo(data.ReceiveSocket, session.RemoteEndPoint);

				MySqlCommand cmd = new MySqlCommand(String.Format("UPDATE account SET sessionkey = '{1}', last_ip='{2}', last_login='{3}' WHERE username = '{0}'", sessionData.Account, Convert.ToBase64String(sessionData.SessionKey), session.RemoteEndPoint.Address, DateTime.Now.ToString("yyyy-MM-dd")), Program.db);
				cmd.ExecuteNonQuery();

				Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Auth success for user {3}. [session key = {4}]", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account, Convert.ToBase64String(sessionData.SessionKey));
			}
			else {
				//Wrong pass
				Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Wrong password for user {3}.", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port, sessionData.Account);
				var ds = UDPTransmitter.CreateObject();
				ds.WriteUint16((UInt16)SMSG_AUTH_LOGON_PROOF_FAIL);
				ds.WriteUint8((byte)AccountState.LOGIN_UNKNOWN_ACCOUNT);
				ds.SendTo(data.ReceiveSocket, session.RemoteEndPoint);
			}
		}

		private static void OnRealmList(Session session, UDPReceiver data) {
			var sessionData = (ClientInfo)session.Data;

			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] CMD_REALM_LIST", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port);

			int packetLen = 0;
			DataTable result = new DataTable();

			//Retrieve the Account ID
			MySqlCommand cmd = new MySqlCommand(String.Format("SELECT id FROM account WHERE username = '{0}'", sessionData.Account), Program.db);
			int accountId = (int)cmd.ExecuteScalar();

			Console.WriteLine("[{0:yyyy-MM-dd HH\\:mm\\:ss}] [{1}:{2}] Listing all realms", DateTime.Now, session.RemoteEndPoint.Address, session.RemoteEndPoint.Port);
			Program.MySqlToDataTable(String.Format("SELECT * FROM realm"), Program.db, result);

			foreach (DataRow row in result.Rows) {
				packetLen = packetLen + ((string)row["address"]).Length + ((string)row["name"]).Length + 1 + ((int)row["port"]).ToString("0").Length + 14;
			}

			var ds = UDPTransmitter.CreateObject();
			ds.WriteUint16((byte)SMSG_REALMLIST_RESPONSE);

			using (Aes aes = new AesManaged()) {
				using (MemoryStream ms = new MemoryStream()) {
					using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(sessionData.SessionKey, sessionData.Salt.ToByteArray().Take(16).ToArray()), CryptoStreamMode.Write)) {
						cs.WriteByte((byte)result.Rows.Count);

						foreach (DataRow host in result.Rows) {
							cs.WriteByte((byte)((int)host["type"]));
							cs.WriteByte((byte)((int)host["state"]));
							cs.WriteByte((byte)((int)host["realmflags"]));
							byte[] realmName = Encoding.UTF8.GetBytes((string)host["name"]);
							cs.Write(realmName, 0, realmName.Length);
							cs.WriteByte((byte)0);
							byte[] realmAddress = Encoding.UTF8.GetBytes((string)host["address"] + ":" + host["port"].ToString());
							cs.Write(realmAddress, 0, realmAddress.Length);
							cs.WriteByte((byte)0);
						}
					}

					var messageBody = ms.ToArray();
					ds.WriteUint16((UInt16)messageBody.Length);
					ds.WriteFixedBlob(messageBody);
				}
			}

			ds.SendTo(data.ReceiveSocket, session.RemoteEndPoint);
		}
    }
}