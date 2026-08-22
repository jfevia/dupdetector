using DupDetector.Core.Model;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="InstanceDocument" />.
/// </summary>
public static class InstanceDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="instance"></param>
    public static InstanceDocument From(CodeInstance instance)
    {
        var value = new InstanceDocument()
        {
            File = instance.FilePath,
            Project = instance.Project.ToString(),
            Member = instance.MemberName,
            StartLine = instance.Lines.Start,
            EndLine = instance.Lines.End,
            IsTestFile = instance.IsTestFile,
            Hash = instance.Hash,
        };
        return value;
    }
}
