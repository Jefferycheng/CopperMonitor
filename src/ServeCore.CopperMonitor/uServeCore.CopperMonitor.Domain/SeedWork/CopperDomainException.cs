namespace uServeCore.CopperMonitor.Domain.SeedWork;

public enum CopperExceptionCode
{
    Unknown = 0,
    InvalidDateRange,
    PriceDataUnavailable,
    ExchangeRateUnavailable,
    LineDeliveryFailed,
    LineNotConfigured,
    UnhandledException
}

public class CopperDomainException(CopperExceptionCode code, string message) : Exception(message)
{
    public CopperExceptionCode Code { get; } = code;
}
