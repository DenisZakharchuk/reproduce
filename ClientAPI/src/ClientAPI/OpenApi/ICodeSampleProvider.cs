namespace ClientAPI.OpenApi;

internal interface ICodeSampleProvider
{
    IReadOnlyList<CodeSample> GetSamples(OperationMetadata metadata);
}
