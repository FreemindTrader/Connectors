namespace StockSharp.MoexISS.Native.Requests;

class SecurityDividendsResponse
{
	[JsonProperty("dividends", Required = Required.Always)]
	public IssResponsePayload Dividends { get; set; }
}
