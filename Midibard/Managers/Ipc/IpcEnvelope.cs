namespace MidiBard.Managers.Ipc
{
	public class IpcEnvelope
	{
		public IpcOpCode OpCode { get; set; }
		public object Data { get; set; }
	}
}