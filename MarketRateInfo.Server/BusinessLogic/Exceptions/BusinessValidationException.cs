using System;

namespace MarketRateInfo.Server.BusinessLogic.Exceptions;

/// <summary>
/// Represents errors that occur when business validation rules are violated.
/// </summary>
public sealed class BusinessValidationException : Exception
{
	public BusinessValidationException(string message)
		: base(message)
	{
	}
}
