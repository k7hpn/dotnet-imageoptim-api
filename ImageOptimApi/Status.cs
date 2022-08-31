namespace ImageOptimApi
{
    public enum Status
    {
        Success = 0,
        CannotFindImage,
        OptionsOrImageIncorrect,
        OtherError,
        PaymentRequired,
        TestSuccess,
        UsernameMissingIncorrect
    }
}