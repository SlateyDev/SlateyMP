namespace SlateyMP.Server.Realm {
	public enum AccessLevel {
		Player = 0,
		GameMaster
	}

	public class ClientInfo {
		public AccessLevel Access = AccessLevel.Player;

		public byte[] SessionKey;
	}
}
