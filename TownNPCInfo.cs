namespace Census
{
	internal class TownNPCInfo
	{
		public int type;
		public string conditions;

		public TownNPCInfo(int type, string conditions) {
			this.type = type;
			this.conditions = conditions;
		}

		public void Deconstruct(out int type, out string conditions) {
			type = this.type;
			conditions = this.conditions;
		}
	}
}

