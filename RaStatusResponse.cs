using System;

namespace RaCaching
{
	[Serializable]
	public struct RaStatusResponse
	{
		public bool Success;
		public string Message;

		public static RaStatusResponse CreateFailure(string message)
		{
			return new RaStatusResponse(false, message);
		}

		public static RaStatusResponse CreateSuccess(string message = "")
		{
			return new RaStatusResponse(true, message);
		}

		public RaStatusResponse(bool success, string message)
		{
			Success = success;
			Message = message;
		}
	}
}