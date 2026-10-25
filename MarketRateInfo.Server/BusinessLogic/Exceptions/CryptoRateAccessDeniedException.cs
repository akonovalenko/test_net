using System;

namespace MarketRateInfo.Server.BusinessLogic.Exceptions;

/// <summary>
/// Exception thrown when access to cryptocurrency rate information is denied.
/// </summary>
public sealed class CryptoRateAccessDeniedException : Exception
{
	public CryptoRateAccessDeniedException(string message)
		: base(message)
	{
	}
}
